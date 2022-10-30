using Timeline.Beans;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Timeline.Utils;
using Microsoft.Graphics.Canvas;
using Windows.UI;
using Windows.Storage;
using System.Collections.Generic;
using System.Threading;
using Windows.ApplicationModel;
using System.Net;

namespace Timeline.Providers {
    public class Himawari8Provider : BaseProvider {
        // 地球位置（0.01~1.00，0.50 为居中，0.01~0.50 偏左，0.50~1.00 偏右）
        private float offsetEarth = 0.5f;
        // 地球大小（0.10~1.00）
        private float ratioEarth = 0.5f;
        // 次页加载量
        private const int PAGE_SIZE = 12;
        // 首页加载量（减少量以节省HEAD校验时间）
        private const int PAGE_SIZE_FIRST = 3;
        // 最小间隔
        private const int INTERVAL_MINUTES = 10;
        // 最大延迟
        private const int DELAY_MINUTES = 25;

        // 向日葵-8号即時網頁 - NICT
        // https://himawari.asia
        // https://gitee.com/irontec/himawaripy
        // 最小间隔：10min（可能出现缺失）
        // 最大延迟：25min
        //private const string URL_API = "https://himawari8-dl.nict.go.jp/himawari8/img/D531106/latest.json";
        //private const string URL_API = "https://ncthmwrwbtst.cr.chiba-u.ac.jp/img/FULL_24h/latest.json?_=";
        //private const string URL_API = "https://himawari8.nict.go.jp/img/FULL_24h/latest.json?_=";
        private const string URL_API = "https://himawari8.nict.go.jp/img/D531106/latest.json?_=";
        // https://himawari8.nict.go.jp/img/D531106/thumbnail/550/2022/10/03/022000_0_0.png
        private const string URL_IMG = "https://himawari8.nict.go.jp/img/D531106/1d/550/{0}_0_0.png";
        //private const string URL_IMG = "https://himawari8.nict.go.jp/img/D531106/thumbnail/550/{0}_0_0.png";
        //private const string URL_IMG = "https://himawari8-dl.nict.go.jp/himawari8/img/D531106/1d/550/{0}_0_0.png";
        //private const string URL_IMG = "https://ncthmwrwbtst.cr.chiba-u.ac.jp/img/D531106/1d/550/{0}_0_0.png";

        private Meta ParseBean(DateTime timeUtc) {
            DateTime timeValid = new DateTime(timeUtc.Ticks
                / (10000L * 1000 * 60 * INTERVAL_MINUTES) * (10000L * 1000 * 60 * INTERVAL_MINUTES)); // 转整十分钟
            Meta meta = new Meta {
                Id = timeValid.ToString("yyyyMMddHHmmss"),
                Uhd = string.Format(URL_IMG, timeValid.ToString(@"yyyy\/MM\/dd\/HHmmss")),
                Format = ".png"
            };
            meta.Thumb = meta.Uhd;
            meta.Date = timeValid.ToLocalTime(); // UTC转本地时间
            meta.Caption = "🌏 " + meta.Date.ToString("M") + " " + meta.Date.ToString("t");
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, Ini ai, BaseIni bi, Go go) {
            Himawari8Ini ini = bi as Himawari8Ini;
            offsetEarth = ini.Offset;
            ratioEarth = ini.Ratio;
            List<Meta> metasTodo = new List<Meta>();
            HttpClient client = null;
            if (GetCount() > 0) { // 加载下一页
                DateTime timeUtc = GetMinDate(true).AddHours(-1); // -1 避免重复加载衔接图
                for (int i = 0; i < PAGE_SIZE; i++) {
                    metasTodo.Add(ParseBean(timeUtc.AddHours(-i)));
                }
            } else if (!go.Date.ToString("yyyyMMdd").Equals(DateTime.Now.ToString("yyyyMMdd"))) { // 加载指定时间数据
                DateTime timeUtc = go.Date.ToUniversalTime(); // 使用UTC时间
                timeUtc = timeUtc > DateTime.UtcNow.AddMinutes(-DELAY_MINUTES)
                    ? DateTime.UtcNow.AddMinutes(-DELAY_MINUTES) : timeUtc;
                for (int i = 0; i < PAGE_SIZE; i++) {
                    metasTodo.Add(ParseBean(timeUtc.AddHours(-i)));
                }
            } else { // 加载最新时间数据
                string urlApi = URL_API + DateUtil.CurrentTimeMillis();
                LogUtil.D("LoadData() provider url: " + urlApi);
                try {
                    client = new HttpClient();
                    HttpResponseMessage res = await client.GetAsync(urlApi, token);
                    string jsonData = await res.Content.ReadAsStringAsync();
                    //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                    Himawari8Api api = JsonConvert.DeserializeObject<Himawari8Api>(jsonData);
                    //DateTime date = DateTime.ParseExact(api.Date, "yyyy-MM-dd HH:mm:ss",
                    //    new System.Globalization.CultureInfo("en-US"));
                    if (!DateTime.TryParse(api.Date, out DateTime date)) { // UTC时间
                        date = DateTime.UtcNow.AddMinutes(-DELAY_MINUTES);
                        Debug.WriteLine("1 " + date.Kind);
                    } else {
                        Debug.WriteLine("2 " + date.Kind);
                    }
                    for (int i = 0; i < PAGE_SIZE_FIRST; i++) {
                        metasTodo.Add(ParseBean(date.AddHours(-i)));
                    }
                } catch (Exception e) {
                    // 情况1：任务被取消
                    // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                    LogUtil.E("LoadData() " + e.Message);
                }
            }

            if (metasTodo.Count > 0) { // 检查有效性
                // 多连接优化
                // https://www.cnblogs.com/dudu/p/csharp-httpclient-attention.html
                client = client ?? new HttpClient();
                client.DefaultRequestHeaders.Connection.Add("keep-alive");
                List<Meta> metasAdd = new List<Meta>();
                foreach (Meta m in metasTodo) {
                    if (token.IsCancellationRequested) {
                        break;
                    }
                    try {
                        HttpRequestMessage reqMsg = new HttpRequestMessage(HttpMethod.Head, m.Uhd);
                        HttpResponseMessage resMsg = await client.SendAsync(reqMsg, token);
                        if (resMsg.StatusCode == HttpStatusCode.OK && (resMsg.Content.Headers.ContentLength ?? 0) > 10 * 1024) {
                            metasAdd.Add(m);
                        } else { // 再尝试一次
                            Meta metaAlt = ParseBean(m.Date.ToUniversalTime().AddMinutes(-INTERVAL_MINUTES));
                            reqMsg = new HttpRequestMessage(HttpMethod.Head, metaAlt.Uhd);
                            resMsg = await client.SendAsync(reqMsg, token);
                            if (resMsg.StatusCode == HttpStatusCode.OK && (resMsg.Content.Headers.ContentLength ?? 0) > 10 * 1024) {
                                metasAdd.Add(metaAlt);
                            }
                        }
                    } catch (Exception) { }
                }
                AppendMetas(metasAdd);
                LogUtil.I("LoadData() head pass " + metasAdd.Count + "/" + metasTodo.Count);
            }
            if (GetCount() == 0) { // 加载失败，使用备用图
                DateTime utcDef = new DateTime(2022, 5, 21, 18, 20, 0, DateTimeKind.Utc);
                string nameDef = "Assets\\Images\\himawari8-" + utcDef.ToString("yyyyMMddHHmmss") + ".png";
                StorageFile fileDef = await Package.Current.InstalledLocation.GetFileAsync(nameDef);
                Meta meta = ParseBean(utcDef);
                meta.CacheUhd = fileDef;
                AppendMetas(new List<Meta>() { meta });
            }
            return true;
        }

        public override async Task<Meta> CacheAsync(Meta meta, bool calFacePos, CancellationToken token) {
            await base.CacheAsync(meta, calFacePos, token);
            if (meta?.CacheUhd == null) {
                return null;
            }
            string tag = string.Format("-offset{0}-ratio{1}",
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
