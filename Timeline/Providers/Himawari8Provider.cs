using Timeline.Beans;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Timeline.Utils;
using Microsoft.Graphics.Canvas;
using Windows.UI;
using Windows.Storage;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using Windows.ApplicationModel;

namespace Timeline.Providers {
    public class Himawari8Provider : BaseProvider {
        // 下一页索引（从最新UTC时间开始，非实时，延迟15~25分钟）（用于按需加载）
        private DateTime? nextPage = null;

        // 地球位置（0.01~1.00，0.50 为居中，0.01~0.50 偏左，0.50~1.00 偏右）
        private float offsetEarth = 0.5f;
        // 地球大小（0.10~1.00）
        private float ratioEarth = 0.5f;

        // 向日葵-8号即時網頁 - NICT
        // https://himawari8.nict.go.jp/zh/himawari8-image.htm
        // https://gitee.com/irontec/himawaripy
        private const string URL_API = "https://himawari8-dl.nict.go.jp/himawari8/img/D531106/latest.json";
        private const string URL_IMG = "https://himawari8.nict.go.jp/img/D531106/1d/550/{0}/{1}_0_0.png";
        //private const string URL_IMG = "https://himawari8.nict.go.jp/img/D531106/thumbnail/550/{0}/{1}_0_0.png";
        //private const string URL_IMG = "https://himawari8-dl.nict.go.jp/himawari8/img/D531106/1d/550/{0}/{1}_0_0.png";

        private Meta ParseBean(DateTime time) {
            string index = string.Format("{0}{1}000", time.ToString("HH"), (time.Minute / 10)); // 转整十分钟
            Meta meta = new Meta {
                Id = time.ToString("yyyyMMdd") + index,
                Uhd = string.Format(URL_IMG, time.ToString(@"yyyy\/MM\/dd"), index),
                Format = ".png"
            };
            meta.Thumb = meta.Uhd;
            meta.Date = time.ToLocalTime();
            meta.SortFactor = time.Ticks;
            meta.Caption = meta.Date.Value.ToString("M") + " " + meta.Date.Value.ToString("t");
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni ini, DateTime? date = null) {
            offsetEarth = ((Himawari8Ini)ini).Offset;
            ratioEarth = ((Himawari8Ini)ini).Ratio;
            // 无需加载更多
            if (indexFocus < metas.Count - 1 && date == null) {
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(token, ini, date);

            if (date != null) { // 指定时间
                metas.Clear();
                nextPage = date;
            } else if (nextPage == null) { // 获取最新UTC时间
                LogUtil.D("LoadData() provider url: " + URL_API);
                try {
                    HttpClient client = new HttpClient();
                    HttpResponseMessage res = await client.GetAsync(URL_API, token);
                    string jsonData = await res.Content.ReadAsStringAsync();
                    //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                    Himawari8Api api = JsonConvert.DeserializeObject<Himawari8Api>(jsonData);
                    nextPage = DateTime.ParseExact(api.Date, "yyyy-MM-dd HH:mm:ss", new System.Globalization.CultureInfo("en-US"));
                } catch (Exception e) {
                    // 情况1：任务被取消
                    // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                    LogUtil.E("LoadData() " + e.Message);
                }
            }
            if (nextPage != null) {
                List<Meta> metasAdd = new List<Meta>();
                for (int i = 0; i < 99; i++) {
                    metasAdd.Add(ParseBean(nextPage.Value.AddHours(-i)));
                }
                SortMetas(metasAdd);
                nextPage = nextPage.Value.AddHours(-99);
            } else if (metas.Count == 0) {
                StorageFile defaultFile = await Package.Current.InstalledLocation.GetFileAsync("Assets\\Images\\himawari8-20220521182000.png");
                List<Meta> metasAdd = new List<Meta>();
                Meta meta = ParseBean(DateTime.Parse("2022-05-21 18:20:00"));
                meta.CacheUhd = defaultFile;
                metasAdd.Add(meta);
                SortMetas(metasAdd);
            }

            return metas.Count > 0;
        }

        public override async Task<Meta> CacheAsync(Meta meta, CancellationToken token) {
            await base.CacheAsync(meta, token);
            if (meta?.CacheUhd == null) {
                return null;
            }
            string tag = String.Format("-offset{0}-ratio{1}",
                (offsetEarth * 100).ToString("000"), (ratioEarth * 100).ToString("000"));
            if (meta.CacheUhd.Path.Contains(tag)) {
                return meta;
            }
            StorageFile cacheUhd = meta.CacheUhd;
            meta.CacheUhd = null;
            meta.Dimen = new Size();
            meta.FacePos = new List<Point>();
            if (token.IsCancellationRequested) {
                return meta;
            }

            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasBitmap bitmap = null;
            using (var stream = await cacheUhd.OpenReadAsync()) {
                bitmap = await CanvasBitmap.LoadAsync(device, stream);
            }
            if (bitmap == null || token.IsCancellationRequested) {
                return meta;
            }

            // 获取显示器分辨率
            Size monitorSize = SysUtil.GetMonitorPhysicalPixels();
            if (monitorSize.IsEmpty) {
                monitorSize = new Size(1920, 1080);
            }
            // 根据地球大小参数计算画布大小
            float canvasW, canvasH;
            if (monitorSize.Width > monitorSize.Height) {
                canvasH = bitmap.SizeInPixels.Height / ratioEarth;
                canvasW = canvasH / monitorSize.Height * monitorSize.Width;
            } else {
                canvasW = bitmap.SizeInPixels.Width / ratioEarth;
                canvasH = canvasW / monitorSize.Width * monitorSize.Height;
            }
            meta.Dimen = new Size((int)canvasW, (int)canvasH);
            // 根据地球位置参数在画布上绘制地球
            CanvasRenderTarget target = new CanvasRenderTarget(device, meta.Dimen.Width, meta.Dimen.Height, 96);
            using (var session = target.CreateDrawingSession()) {
                session.Clear(Colors.Black);
                session.DrawImage(bitmap, (meta.Dimen.Width + bitmap.SizeInPixels.Width) * offsetEarth - bitmap.SizeInPixels.Width,
                    meta.Dimen.Height / 2.0f - bitmap.SizeInPixels.Height / 2.0f);
            }
            if (token.IsCancellationRequested) {
                return meta;
            }

            meta.CacheUhd = await ApplicationData.Current.TemporaryFolder
                .CreateFileAsync(Id + "-" + meta.Id + tag + meta.Format, CreationCollisionOption.OpenIfExists);
            await target.SaveAsync(meta.CacheUhd.Path, CanvasBitmapFileFormat.Png);
            return meta;
        }
    }
}
