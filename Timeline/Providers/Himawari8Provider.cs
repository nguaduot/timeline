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
using System.Collections.Generic;
using System.Threading;
using Windows.ApplicationModel;

namespace Timeline.Providers {
    public class Himawari8Provider : BaseProvider {
        // 地球位置（0.01~1.00，0.50 为居中，0.01~0.50 偏左，0.50~1.00 偏右）
        private float offsetEarth = 0.5f;
        // 地球大小（0.10~1.00）
        private float ratioEarth = 0.5f;

        // 向日葵-8号即時網頁 - NICT
        // https://himawari.asia
        // https://gitee.com/irontec/himawaripy
        //private const string URL_API = "https://himawari8-dl.nict.go.jp/himawari8/img/D531106/latest.json";
        //private const string URL_IMG = "https://himawari8.nict.go.jp/img/D531106/1d/550/{0}/{1}_0_0.png";
        //private const string URL_IMG = "https://himawari8.nict.go.jp/img/D531106/thumbnail/550/{0}/{1}_0_0.png";
        //private const string URL_IMG = "https://himawari8-dl.nict.go.jp/himawari8/img/D531106/1d/550/{0}/{1}_0_0.png";
        private const string URL_API = "https://ncthmwrwbtst.cr.chiba-u.ac.jp/img/FULL_24h/latest.json?_=";
        private const string URL_IMG = "https://ncthmwrwbtst.cr.chiba-u.ac.jp/img/D531106/1d/550/{0}/{1}_0_0.png";

        private Meta ParseBean(DateTime time) {
            string index = string.Format("{0}{1}000", time.ToString("HH"), (time.Minute / 10)); // 转整十分钟
            Meta meta = new Meta {
                Id = time.ToString("yyyyMMdd") + index,
                Uhd = string.Format(URL_IMG, time.ToString(@"yyyy\/MM\/dd"), index),
                Format = ".png"
            };
            meta.Thumb = meta.Uhd;
            meta.Date = time.ToLocalTime(); // UTC转本地时间
            meta.SortFactor = time.Ticks;
            meta.Caption = "🌏 " + meta.Date.ToString("M") + " " + meta.Date.ToString("t");
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, Go go) {
            Himawari8Ini ini = bi as Himawari8Ini;
            offsetEarth = ini.Offset;
            ratioEarth = ini.Ratio;
            await base.LoadData(token, bi, go);

            if (GetCount() > 0) {
                return true;
            }
            string urlApi = URL_API + DateUtil.CurrentTimeMillis();
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                Himawari8Api api = JsonConvert.DeserializeObject<Himawari8Api>(jsonData);
                DateTime date = DateTime.ParseExact(api.Date, "yyyy-MM-dd HH:mm:ss",
                    new System.Globalization.CultureInfo("en-US")); // UTC时间（非实时，延迟15~25分钟）
                List<Meta> metasAdd = new List<Meta>();
                for (int i = 0; i < 99; i++) {
                    metasAdd.Add(ParseBean(date.AddHours(-i)));
                }
                AppendMetas(metasAdd);
                return true;
            } catch (Exception e) {
                // 情况1：任务被取消
                // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                LogUtil.E("LoadData() " + e.Message);
            }

            if (GetCount() == 0) { // 加载失败，使用备用图
                StorageFile defaultFile = await Package.Current.InstalledLocation.GetFileAsync("Assets\\Images\\himawari8-20220521182000.png");
                List<Meta> metasAdd = new List<Meta>();
                Meta meta = ParseBean(DateTime.Parse("2022-05-21 18:20:00"));
                meta.CacheUhd = defaultFile;
                metasAdd.Add(meta);
                AppendMetas(metasAdd);
            }
            return true;
        }

        public override async Task<Meta> CacheAsync(Meta meta, bool calFacePos, CancellationToken token) {
            await base.CacheAsync(meta, calFacePos, token);
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
            meta.Dimen = new Windows.Foundation.Size();
            meta.FacePos = new List<Windows.Foundation.Point>();
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
            Windows.Foundation.Size monitorSize = SysUtil.GetMonitorPixels(false);
            if (monitorSize.IsEmpty) {
                monitorSize = new Windows.Foundation.Size(1920, 1080);
            }
            // 根据地球大小参数计算画布大小
            double canvasW, canvasH;
            if (monitorSize.Width > monitorSize.Height) {
                canvasH = bitmap.SizeInPixels.Height / ratioEarth;
                canvasW = canvasH / monitorSize.Height * monitorSize.Width;
            } else {
                canvasW = bitmap.SizeInPixels.Width / ratioEarth;
                canvasH = canvasW / monitorSize.Width * monitorSize.Height;
            }
            meta.Dimen = new Windows.Foundation.Size(canvasW, canvasH);
            // 根据地球位置参数在画布上绘制地球
            CanvasRenderTarget target = new CanvasRenderTarget(device, (float)meta.Dimen.Width, (float)meta.Dimen.Height, 96);
            using (var session = target.CreateDrawingSession()) {
                session.Clear(Colors.Black);
                session.DrawImage(bitmap, (float)((meta.Dimen.Width + bitmap.SizeInPixels.Width) * offsetEarth - bitmap.SizeInPixels.Width),
                    (float)(meta.Dimen.Height / 2.0f - bitmap.SizeInPixels.Height / 2.0f));
            }
            if (token.IsCancellationRequested) {
                return meta;
            }

            meta.CacheUhd = await ApplicationData.Current.TemporaryFolder
                .CreateFileAsync(Id + "-" + meta.Id + tag + meta.Format, CreationCollisionOption.OpenIfExists);
            await target.SaveAsync(meta.CacheUhd.Path, CanvasBitmapFileFormat.Png, 1.0f);
            return meta;
        }
    }
}
