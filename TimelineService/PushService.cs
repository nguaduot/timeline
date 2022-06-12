using Microsoft.Graphics.Canvas;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TimelineService.Beans;
using TimelineService.Utils;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Data.Xml.Dom;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;

namespace TimelineService {
    public sealed class PushService : IBackgroundTask {
        BackgroundTaskDeferral _deferral;
        private ApplicationDataContainer localSettings;
        private Ini ini;
        private int periodDesktop = 24;
        private int periodLock = 24;
        private int periodTile = 4;
        private string tagDesktop; // 免重复推送桌面背景标记
        private string tagLock; // 免重复推送锁屏背景标记
        private string tagTile; // 免重复推送磁贴背景标记
        private bool pushNow = false; // 立即运行一次

        private enum Action {
            Desktop,
            Lock,
            Tile
        }

        public async void Run(IBackgroundTaskInstance taskInstance) {
            _deferral = taskInstance.GetDeferral();
            // 初始化
            Init(taskInstance);
            LogUtil.I("Run() trigger: " + taskInstance.TriggerDetails);
            LogUtil.I("Run() desktop: " + ini.DesktopProvider + ", " + periodDesktop);
            LogUtil.I("Run() lock: " + ini.LockProvider + ", " + periodLock);
            LogUtil.I("Run() tile: " + ini.Provider + ", " + periodTile);
            // 检查网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                LogUtil.W("Run() network not available");
                _deferral.Complete();
                return;
            }
            bool pushDesktop = CheckDesktopNecessary();
            bool pushLock = CheckLockNecessary();
            bool pushTile = await CheckTileNecessaryAsync();
            LogUtil.I("Run() CheckNecessary: " + pushDesktop + " " + pushLock + " " + pushTile);
            if (!pushDesktop && !pushLock && !pushTile) { // 本次无需推送
                _deferral.Complete();
                return;
            }
            // 上传统计
            IReadOnlyDictionary<string, int> dosage = await FileUtil.ReadDosage();
            long screen = (long)(localSettings.Values["Screen"] ?? 0);
            long screenW = (screen & 0xffff0000) >> 16;
            long screenH = screen & 0x0000ffff;
            await Api.Stats(ini, dosage.GetValueOrDefault("all", 0), 0, string.Format("{0}x{1}", screenW, screenH));
            // 开始推送
            try {
                if (pushDesktop) {
                    await PushDesktopAsync();
                }
                if (pushLock) {
                    await PushLockAsync();
                }
                if (pushTile) {
                    await PushTileAsync();
                }
            } catch (Exception e) {
                LogUtil.E("Run() " + e.Message);
            } finally {
                _deferral.Complete();
            }
        }

        private void Init(IBackgroundTaskInstance taskInstance) {
            pushNow = taskInstance.TriggerDetails is ApplicationTriggerDetails;
            ini = IniUtil.GetIni();
            periodDesktop = ini.GetDesktopPeriod(ini.DesktopProvider);
            periodLock = ini.GetLockPeriod(ini.LockProvider);
            tagDesktop = string.Format("{0}{1}-{2}", DateTime.Now.ToString("yyyyMMdd"),
                DateTime.Now.Hour / periodDesktop, ini.DesktopProvider);
            tagLock = string.Format("{0}{1}-{2}", DateTime.Now.ToString("yyyyMMdd"),
                DateTime.Now.Hour / periodLock, ini.LockProvider);
            tagTile = string.Format("{0}{1}-{2}", DateTime.Now.ToString("yyyyMMdd"),
                DateTime.Now.Hour / periodTile, ini.Provider);

            if (localSettings == null) {
                localSettings = ApplicationData.Current.LocalSettings;
            }
        }

        private bool CheckDesktopNecessary() {
            if (string.IsNullOrEmpty(ini.DesktopProvider)) { // 未开启推送
                return false;
            }
            if (!UserProfilePersonalizationSettings.IsSupported()) { // 检查是否支持修改背景
                LogUtil.W("CheckDesktopNecessary() not supported");
                return false;
            }
            if (pushNow && ini.DesktopProvider.Equals(ini.Provider)) { // 立即运行一次
                return true;
            }
            return localSettings.Values.ContainsKey(tagDesktop);
        }

        private bool CheckLockNecessary() {
            if (string.IsNullOrEmpty(ini.LockProvider)) { // 未开启推送
                return false;
            }
            if (!UserProfilePersonalizationSettings.IsSupported()) { // 检查是否支持修改背景
                LogUtil.W("CheckLockNecessary() not supported");
                return false;
            }
            if (pushNow && ini.LockProvider.Equals(ini.Provider)) { // 立即运行一次
                return true;
            }
            return localSettings.Values.ContainsKey(tagLock);
        }

        private async Task<bool> CheckTileNecessaryAsync() {
            if (VerUtil.GetOsVerMajor() != 10) { // 检查 Win10
                return false;
            }
            AppListEntry entry = (await Package.Current.GetAppListEntriesAsync())[0];
            if (!await StartScreenManager.GetDefault().ContainsAppListEntryAsync(entry)) { // 检查当前是否已固定
                return false;
            }
            if (pushNow) { // 立即运行一次
                return true;
            }
            return localSettings.Values.ContainsKey(tagTile);
        }

        private async Task<bool> PushDesktopAsync() {
            await FileUtil.WriteDosage(ini.DesktopProvider);
            bool res = false;
            if (BingIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadBingAsync(Action.Desktop);
            } else if (NasaIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadNasaAsync(Action.Desktop);
            } else if (OneplusIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadOneplusAsync(Action.Desktop);
            } else if (TimelineIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadTimelineAsync(Action.Desktop);
            } else if (OneIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadOneAsync(Action.Desktop);
            } else if (Himawari8Ini.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadHimawari8Async(Action.Desktop);
            } else if (YmyouliIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadYmyouliAsync(Action.Desktop);
            } else if (WallhavenIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadWallhavenAsync(Action.Desktop);
            } else if (QingbzIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadQingbzAsync(Action.Desktop);
            } else if (WallhereIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadWallhereAsync(Action.Desktop);
            } else if (InfinityIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadInfinityAsync(Action.Desktop);
            } else if (ObzhiIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadObzhiAsync(Action.Desktop);
            } else if (LspIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadLspAsync(Action.Desktop);
            }
            if (res) {
                localSettings.Values[tagDesktop] = (int)(localSettings.Values[tagDesktop] ?? 0) + 1;
            }
            LogUtil.I("PushDesktopAsync() " + res);
            return res;
        }

        private async Task<bool> PushLockAsync() {
            await FileUtil.WriteDosage(ini.LockProvider);
            bool res = false;
            if (BingIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadBingAsync(Action.Lock);
            } else if (NasaIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadNasaAsync(Action.Lock);
            } else if (OneplusIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadOneplusAsync(Action.Lock);
            } else if (TimelineIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadTimelineAsync(Action.Lock);
            } else if (OneIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadOneAsync(Action.Lock);
            } else if (Himawari8Ini.GetId().Equals(ini.LockProvider)) {
                res = await LoadHimawari8Async(Action.Lock);
            } else if (YmyouliIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadYmyouliAsync(Action.Lock);
            } else if (WallhavenIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadWallhavenAsync(Action.Lock);
            } else if (QingbzIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadQingbzAsync(Action.Lock);
            } else if (WallhereIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadWallhereAsync(Action.Lock);
            } else if (InfinityIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadInfinityAsync(Action.Lock);
            } else if (ObzhiIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadObzhiAsync(Action.Lock);
            } else if (LspIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadLspAsync(Action.Lock);
            }
            if (res) {
                localSettings.Values[tagLock] = (int)(localSettings.Values[tagLock] ?? 0) + 1;
            }
            LogUtil.I("PushLockAsync() " + res);
            return res;
        }

        private async Task<bool> PushTileAsync() {
            await FileUtil.WriteDosage(ini.Provider);
            bool res = false;
            if (BingIni.GetId().Equals(ini.Provider)) {
                res = await LoadBingAsync(Action.Tile);
            } else if (NasaIni.GetId().Equals(ini.Provider)) {
                res = await LoadNasaAsync(Action.Tile);
            } else if (OneplusIni.GetId().Equals(ini.Provider)) {
                res = await LoadOneplusAsync(Action.Tile);
            } else if (TimelineIni.GetId().Equals(ini.Provider)) {
                res = await LoadTimelineAsync(Action.Tile);
            } else if (OneIni.GetId().Equals(ini.Provider)) {
                res = await LoadOneAsync(Action.Tile);
            } else if (Himawari8Ini.GetId().Equals(ini.Provider)) {
                res = await LoadHimawari8Async(Action.Tile);
            } else if (YmyouliIni.GetId().Equals(ini.Provider)) {
                res = await LoadYmyouliAsync(Action.Tile);
            } else if (WallhavenIni.GetId().Equals(ini.Provider)) {
                res = await LoadWallhavenAsync(Action.Tile);
            } else if (QingbzIni.GetId().Equals(ini.Provider)) {
                res = await LoadQingbzAsync(Action.Tile);
            } else if (WallhereIni.GetId().Equals(ini.Provider)) {
                res = await LoadWallhereAsync(Action.Tile);
            } else if (InfinityIni.GetId().Equals(ini.Provider)) {
                res = await LoadInfinityAsync(Action.Tile);
            } else if (ObzhiIni.GetId().Equals(ini.Provider)) {
                res = await LoadObzhiAsync(Action.Tile);
            } else if (LspIni.GetId().Equals(ini.Provider)) {
                res = await LoadLspAsync(Action.Tile);
            }
            if (res) {
                localSettings.Values[tagTile] = (int)(localSettings.Values[tagTile] ?? 0) + 1;
            }
            LogUtil.I("PushTileAsync() " + res);
            return res;
        }

        private async Task<bool> SetDesktopBgAsync(StorageFile fileImg) {
            BasicProperties properties = await fileImg.GetBasicPropertiesAsync();
            if (properties.Size == 0) {
                return false;
            }
            return await UserProfilePersonalizationSettings.Current.TrySetWallpaperImageAsync(fileImg);
        }

        private async Task<bool> SetLockBackground(StorageFile fileImg) {
            BasicProperties properties = await fileImg.GetBasicPropertiesAsync();
            if (properties.Size == 0) {
                return false;
            }
            return await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(fileImg);
        }

        private bool SetTileBg(string urlThumb) {
            if (string.IsNullOrEmpty(urlThumb)) {
                return false;
            }
            string content = string.Format("<tile><visual>" +
                "<binding template='TileWide'>" +
                "<image src='{0}' placement='background' />" +
                "</binding>" +
                "<binding template='TileLarge'>" +
                "<image src='{0}' placement='background' />" +
                "</binding>" +
                "</visual></tile>", urlThumb);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);
            TileNotification notification = new TileNotification(doc);
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
            return true;
        }

        private async Task<bool> SetTileBackgroundForHimawari8(StorageFile fileImg) {
            BasicProperties properties = await fileImg.GetBasicPropertiesAsync();
            if (properties.Size == 0) {
                return false;
            }
            string content = string.Format("<tile><visual>" +
                "<binding template='TileMedium'>" +
                "<image src='{0}' placement='background' />" +
                "</binding>" +
                "<binding template='TileWide'>" +
                "<image src='{0}' placement='background' />" +
                "</binding>" +
                "<binding template='TileLarge'>" +
                "<image src='{0}' placement='background' />" +
                "</binding>" +
                "</visual></tile>", fileImg.Path);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);
            TileNotification notification = new TileNotification(doc);
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
            return true;
        }

        private async Task<StorageFile> DownloadImgAsync(string urlImg, Action action, float offset = -1f, float ratio = -1f) {
            string cacheName;
            switch (action) {
                case Action.Desktop:
                    cacheName = "desktop";
                    break;
                case Action.Lock:
                    cacheName = "lock";
                    break;
                case Action.Tile:
                default:
                    cacheName = "tile";
                    break;
            }
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(cacheName, CreationCollisionOption.ReplaceExisting);
            if (string.IsNullOrEmpty(urlImg)) {
                LogUtil.E("SetWallpaper() invalid url");
                return file;
            }

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation operation = downloader.CreateDownload(new Uri(urlImg), file);
            DownloadOperation resOperation = await operation.StartAsync().AsTask();
            if (resOperation.Progress.Status != BackgroundTransferStatus.Completed) {
                LogUtil.E("SetWallpaper() download error");
                return file;
            }
            if (offset >= 0 && ratio >= 0) {
                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasBitmap bitmap = null;
                using (var stream = await file.OpenReadAsync()) {
                    bitmap = await CanvasBitmap.LoadAsync(device, stream);
                }
                if (bitmap == null) {
                    return file;
                }
                //DisplayInformation info = DisplayInformation.GetForCurrentView();
                //Size monitorSize = new Size((int)info.ScreenWidthInRawPixels, (int)info.ScreenHeightInRawPixels);
                long screen = (long)(localSettings.Values["Screen"] ?? 0);
                long screenW = (screen & 0xffff0000) >> 16;
                long screenH = screen & 0x0000ffff;
                if (screen == 0) {
                    screenW = 1920;
                    screenH = 1080;
                }
                float canvasW, canvasH;
                if (screenW > screenH) {
                    canvasH = bitmap.SizeInPixels.Height / ratio;
                    canvasW = canvasH / screenH * screenW;
                } else {
                    canvasW = bitmap.SizeInPixels.Width / ratio;
                    canvasH = canvasW / screenW * screenH;
                }
                CanvasRenderTarget target = new CanvasRenderTarget(device, canvasW, canvasH, 96);
                using (var session = target.CreateDrawingSession()) {
                    session.Clear(Colors.Black);
                    session.DrawImage(bitmap, (canvasW + bitmap.SizeInPixels.Width) * offset - bitmap.SizeInPixels.Width,
                        canvasH / 2 - bitmap.SizeInPixels.Height / 2);
                }
                file = await ApplicationData.Current.LocalFolder.CreateFileAsync(cacheName + "-reset", CreationCollisionOption.ReplaceExisting);
                await target.SaveAsync(file.Path, CanvasBitmapFileFormat.Png, 1.0f);
            }
            return file;
        }

        private async Task<bool> LoadBingAsync(Action action) {
            const string URL_API_HOST = "https://global.bing.com";
            const string URL_API = URL_API_HOST + "/HPImageArchive.aspx?pid=hp&format=js&uhd=1&idx=0&n=1";
            LogUtil.I("LoadBing() api url: " + URL_API);
            HttpClient client = new HttpClient();
            string jsonData = await client.GetStringAsync(URL_API);
            BingApi bing = JsonConvert.DeserializeObject<BingApi>(jsonData);
            if (action == Action.Tile) {
                string urlThumb = string.Format("{0}{1}_400x240.jpg", URL_API_HOST, bing.Images[0].UrlBase);
                LogUtil.I("LoadBing() img url: " + urlThumb);
                return SetTileBg(urlThumb);
            } else {
                string urlUhd = string.Format("{0}{1}_UHD.jpg", URL_API_HOST, bing.Images[0].UrlBase);
                LogUtil.I("LoadBing() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadNasaAsync(Action action) {
            string urlApi = string.Format("https://api.nasa.gov/planetary/apod?api_key=DEMO_KEY&thumbs=True&start_date={0}&end_date={1}",
                DateTime.UtcNow.AddHours(-4).AddDays(-6).ToString("yyyy-MM-dd"), DateTime.UtcNow.AddHours(-4).ToString("yyyy-MM-dd"));
            LogUtil.I("LoadNasa() api url: " + urlApi);
            HttpClient client = new HttpClient();
            string jsonData = await client.GetStringAsync(urlApi);
            IList<NasaApiItem> items = JsonConvert.DeserializeObject<IList<NasaApiItem>>(jsonData);
            if (action == Action.Tile) {
                string urlThumb = null;
                for (int i = items.Count - 1; i >= 0; --i) { // 取最近日期
                    if ("image".Equals(items[i].MediaType)) {
                        urlThumb = items[i].Url;
                        break;
                    }
                }
                LogUtil.I("LoadNasa() img url: " + urlThumb);
                return SetTileBg(urlThumb);
            } else {
                string urlUhd = null;
                for (int i = items.Count - 1; i >= 0; --i) { // 取最近日期
                    if ("image".Equals(items[i].MediaType)) {
                        urlUhd = items[i].HdUrl;
                        break;
                    }
                }
                LogUtil.I("LoadNasa() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadOneplusAsync(Action action) {
            OneplusRequest request = new OneplusRequest {
                PageSize = 1,
                CurrentPage = 1,
                SortMethod = "1" // 默认按：最新添加
            };
            string requestStr = JsonConvert.SerializeObject(request);
            const string URL_API = "https://photos.oneplus.com/cn/shot/photo/schedule";
            LogUtil.I("LoadOneplus() api url: " + URL_API + " " + requestStr);
            HttpClient client = new HttpClient();
            HttpContent content = new StringContent(requestStr);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await client.PostAsync(URL_API, content);
            _ = response.EnsureSuccessStatusCode();
            string jsonData = await response.Content.ReadAsStringAsync();
            OneplusApi oneplusApi = JsonConvert.DeserializeObject<OneplusApi>(jsonData);
            if (action == Action.Tile) {
                string urlThumb = oneplusApi.Items[0].PhotoUrl.Replace(".jpg", "_400_0.jpg");
                LogUtil.I("LoadBing() img url: " + urlThumb);
                return SetTileBg(urlThumb);
            } else {
                string urlUhd = oneplusApi.Items[0].PhotoUrl;
                LogUtil.I("LoadOneplus() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadTimelineAsync(Action action) {
            if (action == Action.Tile) {
                const string URL_API = "https://api.nguaduot.cn/timeline/today?client=timelinewallpaper&thumb=1";
                LogUtil.I("LoadTimeline() api url: " + URL_API);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(URL_API);
                string urlThumb = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadTimeline() img url: " + urlThumb);
                return SetTileBg(urlThumb);
            } else {
                const string URL_API = "https://api.nguaduot.cn/timeline/today?client=timelinewallpaper";
                LogUtil.I("LoadTimeline() api url: " + URL_API);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(URL_API);
                string urlUhd = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadTimeline() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadOneAsync(Action action) {
            const string URL_TOKEN = "http://m.wufazhuce.com/one";
            const string URL_API = "http://m.wufazhuce.com/one/ajaxlist/0?_token={0}";
            HttpClient client = new HttpClient();
            HttpResponseMessage msg = await client.GetAsync(URL_TOKEN); // cookie 无需手动取，自动包含
            string htmlData = await msg.Content.ReadAsStringAsync();
            Match match = Regex.Match(htmlData, @"One.token ?= ?[""'](.+?)[""']");
            string token = match.Groups[1].Value;
            string urlApi = string.Format(URL_API, token);
            LogUtil.I("LoadOne() api url: " + urlApi);
            string jsonData = await client.GetStringAsync(urlApi);
            match = Regex.Match(jsonData, @"""img_url"": ?""(.+?)""");
            string urlUhd = Regex.Unescape(match.Groups[1].Value); // 反转义
            LogUtil.I("LoadOne() img url: " + urlUhd);
            if (action == Action.Tile) {
                return SetTileBg(urlUhd);
            } else {
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadHimawari8Async(Action action) {
            const string URL_API = "https://himawari8-dl.nict.go.jp/himawari8/img/D531106/latest.json";
            const string URL_IMG = "https://himawari8.nict.go.jp/img/D531106/1d/550/{0}/{1}_0_0.png";
            LogUtil.I("LoadHimawari8() api url: " + URL_API);
            HttpClient client = new HttpClient();
            string jsonData = await client.GetStringAsync(URL_API);
            Match match = Regex.Match(jsonData, @"""date"": ?""(.+?)""");
            DateTime time = DateTime.ParseExact(match.Groups[1].Value, "yyyy-MM-dd HH:mm:ss",
                new System.Globalization.CultureInfo("en-US"));
            string urlUhd = string.Format(URL_IMG, time.ToString(@"yyyy\/MM\/dd"),
                string.Format("{0}{1}000", time.ToString("HH"), time.Minute / 10));
            LogUtil.I("LoadHimawari8() img url: " + urlUhd);
            if (action == Action.Tile) {
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action, 0.5f, 0.5f);
                return await SetTileBackgroundForHimawari8(fileImg);
            } else {
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action, ini.Himawari8.Offset, ini.Himawari8.Ratio);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadYmyouliAsync(Action action) {
            if (action == Action.Tile) {
                const string URL_API = "https://api.nguaduot.cn/ymyouli/random?client=timelinewallpaper&cate={0}&thumb=1";
                string urlApi = string.Format(URL_API, ini.Ymyouli.Cate);
                LogUtil.I("LoadYmyouli() api url: " + urlApi);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(urlApi);
                string urlThumb = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadYmyouli() img url: " + urlThumb);
                return SetTileBg(urlThumb);
            } else {
                const string URL_API = "https://api.nguaduot.cn/ymyouli/random?client=timelinewallpaper&cate={0}";
                string urlApi = string.Format(URL_API, ini.Ymyouli.Cate);
                LogUtil.I("LoadYmyouli() api url: " + urlApi);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(urlApi);
                string urlUhd = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadYmyouli() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadWallhavenAsync(Action action) {
            if (action == Action.Tile) {
                const string URL_API = "https://api.nguaduot.cn/wallhaven/random?client=timelinewallpaper&cate={0}&thumb=1";
                string urlApi = string.Format(URL_API, ini.Wallhaven.Cate);
                LogUtil.I("LoadWallhaven() api url: " + urlApi);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(urlApi);
                string urlThumb = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadWallhaven() img url: " + urlThumb);
                return SetTileBg(urlThumb);
            } else {
                const string URL_API = "https://api.nguaduot.cn/wallhaven/random?client=timelinewallpaper&cate={0}";
                string urlApi = string.Format(URL_API, ini.Wallhaven.Cate);
                LogUtil.I("LoadWallhaven() api url: " + urlApi);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(urlApi);
                string urlUhd = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadWallhaven() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadQingbzAsync(Action action) {
            if (action == Action.Tile) {
                const string URL_API = "https://api.nguaduot.cn/qingbz/random?client=timelinewallpaper&cate={0}&thumb=1";
                string urlApi = string.Format(URL_API, ini.Qingbz.Cate);
                LogUtil.I("LoadQingbz() api url: " + urlApi);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(urlApi);
                string urlThumb = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadQingbz() img url: " + urlThumb);
                return SetTileBg(urlThumb);
            } else {
                const string URL_API = "https://api.nguaduot.cn/qingbz/random?client=timelinewallpaper&cate={0}";
                string urlApi = string.Format(URL_API, ini.Qingbz.Cate);
                LogUtil.I("LoadQingbz() api url: " + urlApi);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(urlApi);
                string urlUhd = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadQingbz() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadWallhereAsync(Action action) {
            if (action == Action.Tile) {
                const string URL_API = "https://api.nguaduot.cn/wallhere/random?client=timelinewallpaper&cate={0}&thumb=1";
                string urlApi = string.Format(URL_API, ini.Wallhere.Cate);
                LogUtil.I("LoadWallhere() api url: " + urlApi);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(urlApi);
                string urlThumb = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadWallhere() img url: " + urlThumb);
                return SetTileBg(urlThumb);
            } else {
                const string URL_API = "https://api.nguaduot.cn/wallhere/random?client=timelinewallpaper&cate={0}";
                string urlApi = string.Format(URL_API, ini.Wallhere.Cate);
                LogUtil.I("LoadWallhere() api url: " + urlApi);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(urlApi);
                string urlUhd = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadWallhere() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadInfinityAsync(Action action) {
            const string URL_API = "https://infinity-api.infinitynewtab.com/random-wallpaper?_={0}";
            string urlApi = string.Format(URL_API,
                (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
            LogUtil.I("LoadInfinity() api url: " + urlApi);
            HttpClient client = new HttpClient();
            string jsonData = await client.GetStringAsync(urlApi);
            if (action == Action.Tile) {
                Match match = Regex.Match(jsonData, @"""smallSrc"": ?""(.+?)""");
                string urlThumb = match.Groups[1].Value;
                LogUtil.I("LoadObzhi() img url: " + urlThumb);
                return SetTileBg(urlThumb);
            } else {
                Match match = Regex.Match(jsonData, @"""rawSrc"": ?""(.+?)""");
                string urlUhd = match.Groups[1].Value;
                LogUtil.I("LoadInfinity() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadObzhiAsync(Action action) {
            if (action == Action.Tile) {
                const string URL_API = "https://api.nguaduot.cn/obzhi/random?client=timelinewallpaper&cate={0}&thumb=1";
                string urlApi = string.Format(URL_API, ini.Obzhi.Cate);
                LogUtil.I("LoadObzhi() api url: " + urlApi);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(urlApi);
                string urlThumb = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadObzhi() img url: " + urlThumb);
                return SetTileBg(urlThumb);
            } else {
                const string URL_API = "https://api.nguaduot.cn/obzhi/random?client=timelinewallpaper&cate={0}";
                string urlApi = string.Format(URL_API, ini.Obzhi.Cate);
                LogUtil.I("LoadObzhi() api url: " + urlApi);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(urlApi);
                string urlUhd = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadObzhi() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadLspAsync(Action action) {
            if (action == Action.Tile) {
                const string URL_API = "https://api.nguaduot.cn/lsp/random?client=timelinewallpaper&cate={0}&thumb=1";
                string urlApi = string.Format(URL_API, ini.Lsp.Cate);
                LogUtil.I("LoadLsp() api url: " + urlApi);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(urlApi);
                string urlThumb = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadLsp() img url: " + urlThumb);
                return SetTileBg(urlThumb);
            } else {
                const string URL_API = "https://api.nguaduot.cn/lsp/random?client=timelinewallpaper&cate={0}";
                string urlApi = string.Format(URL_API, ini.Lsp.Cate);
                LogUtil.I("LoadLsp() api url: " + urlApi);
                HttpClient client = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false
                });
                HttpResponseMessage msg = await client.GetAsync(urlApi);
                string urlUhd = msg.Headers.Location.AbsoluteUri;
                LogUtil.I("LoadLsp() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBackground(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }
    }
}
