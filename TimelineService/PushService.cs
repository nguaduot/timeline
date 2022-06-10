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
using Windows.ApplicationModel.Background;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.UI;

namespace TimelineService {
    public sealed class PushService : IBackgroundTask {
        private ApplicationDataContainer localSettings;
        private Ini ini;
        private int desktopPeriod = 24;
        private int lockPeriod = 24;
        private string desktopTag; // 免重复推送桌面背景标记
        private string lockTag; // 免重复推送锁屏背景标记
        private bool pushNow = false; // 立即运行一次

        public async void Run(IBackgroundTaskInstance taskInstance) {
            Init(taskInstance);
            LogUtil.I("PushService.Run() trigger: " + taskInstance.TriggerDetails);
            LogUtil.I("PushService.Run() desktopProvider: " + ini.DesktopProvider);
            LogUtil.I("PushService.Run() lockProvider: " + ini.LockProvider);
            LogUtil.I("PushService.Run() desktopPeriod: " + desktopPeriod);
            LogUtil.I("PushService.Run() lockPeriod: " + lockPeriod);
            // 检查网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                LogUtil.W("PushService.Run() network not available");
                return;
            }

            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            try {
                bool stats = false;
                if (CheckDesktopNecessary()) {
                    bool done = false;
                    if (BingIni.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadBing(true);
                    } else if (NasaIni.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadNasa(true);
                    } else if (OneplusIni.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadOneplus(true);
                    } else if (TimelineIni.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadTimeline(true);
                    } else if (OneIni.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadOne(true);
                    } else if (Himawari8Ini.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadHimawari8(true);
                    } else if (YmyouliIni.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadYmyouli(true);
                    } else if (WallhavenIni.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadWallhaven(true);
                    } else if (QingbzIni.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadQingbz(true);
                    } else if (WallhereIni.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadWallhere(true);
                    } else if (InfinityIni.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadInfinity(true);
                    } else if (ObzhiIni.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadObzhi(true);
                    } else if (LspIni.GetId().Equals(ini.DesktopProvider)) {
                        done = await LoadLsp(true);
                    }
                    if (done) {
                        localSettings.Values[desktopTag] = (int)localSettings.Values[desktopTag] + 1;
                        stats = true;
                    }
                } else {
                    LogUtil.I("PushService.Run() skip desktopTag: " + desktopPeriod);
                }
                if (CheckLockNecessary()) {
                    bool done = false;
                    if (BingIni.GetId().Equals(ini.LockProvider)) {
                        done = await LoadBing(false);
                    } else if (NasaIni.GetId().Equals(ini.LockProvider)) {
                        done = await LoadNasa(false);
                    } else if (OneplusIni.GetId().Equals(ini.LockProvider)) {
                        done = await LoadOneplus(false);
                    } else if (TimelineIni.GetId().Equals(ini.LockProvider)) {
                        done = await LoadTimeline(false);
                    } else if (OneIni.GetId().Equals(ini.LockProvider)) {
                        done = await LoadOne(false);
                    } else if (Himawari8Ini.GetId().Equals(ini.LockProvider)) {
                        done = await LoadHimawari8(false);
                    } else if (YmyouliIni.GetId().Equals(ini.LockProvider)) {
                        done = await LoadYmyouli(false);
                    } else if (WallhavenIni.GetId().Equals(ini.LockProvider)) {
                        done = await LoadWallhaven(false);
                    } else if (InfinityIni.GetId().Equals(ini.LockProvider)) {
                        done = await LoadInfinity(false);
                    } else if (QingbzIni.GetId().Equals(ini.LockProvider)) {
                        done = await LoadQingbz(false);
                    } else if (WallhereIni.GetId().Equals(ini.LockProvider)) {
                        done = await LoadWallhere(false);
                    } else if (ObzhiIni.GetId().Equals(ini.LockProvider)) {
                        done = await LoadObzhi(false);
                    } else if (LspIni.GetId().Equals(ini.LockProvider)) {
                        done = await LoadLsp(false);
                    }
                    if (done) {
                        localSettings.Values[lockTag] = (int)localSettings.Values[lockTag] + 1;
                        stats = true;
                    }
                } else {
                    LogUtil.I("PushService.Run() skip lockTag: " + desktopPeriod);
                }
                if (stats) {
                    _ = await Api.Stats(ini, true);
                }
            } catch (Exception e) {
                LogUtil.E("PushService.Run() " + e.Message);
                Debug.WriteLine(e);
            } finally {
                deferral.Complete();
            }
        }

        private void Init(IBackgroundTaskInstance taskInstance) {
            pushNow = taskInstance.TriggerDetails is ApplicationTriggerDetails;
            ini = IniUtil.GetIni();
            desktopPeriod = ini.GetDesktopPeriod(ini.DesktopProvider);
            lockPeriod = ini.GetLockPeriod(ini.LockProvider);
            desktopTag = string.Format("{0}{1}-{2}", DateTime.Now.ToString("yyyyMMdd"),
                DateTime.Now.Hour / desktopPeriod, ini.DesktopProvider);
            lockTag = string.Format("{0}{1}-{2}", DateTime.Now.ToString("yyyyMMdd"),
                DateTime.Now.Hour / lockPeriod, ini.LockProvider);

            if (localSettings == null) {
                localSettings = ApplicationData.Current.LocalSettings;
                if (!localSettings.Values.ContainsKey(desktopTag)) {
                    localSettings.Values[desktopTag] = 0;
                }
                if (!localSettings.Values.ContainsKey(lockTag)) {
                    localSettings.Values[lockTag] = 0;
                }
            }
        }

        private bool CheckDesktopNecessary() {
            if (!UserProfilePersonalizationSettings.IsSupported()) {
                LogUtil.I("PushService.CheckNecessary() UserProfilePersonalizationSettings false");
                return false;
            }
            if (pushNow) { // 立即运行一次
                return true;
            }
            if (string.IsNullOrEmpty(ini.DesktopProvider)) { // 未开启推送
                return false;
            }
            return (int)localSettings.Values[desktopTag] == 0;
        }

        private bool CheckLockNecessary() {
            if (!UserProfilePersonalizationSettings.IsSupported()) {
                LogUtil.I("PushService.CheckNecessary() UserProfilePersonalizationSettings false");
                return false;
            }
            if (pushNow) { // 立即运行一次
                return true;
            }
            if (string.IsNullOrEmpty(ini.LockProvider)) { // 未开启推送
                return false;
            }
            return (int)localSettings.Values[lockTag] == 0;
        }

        private async Task<bool> SetWallpaper(string urlImg, bool setDesktopOrLock, float offset = -1f, float ratio = -1f) {
            if (urlImg == null) {
                LogUtil.I("PushService.SetWallpaper() invalid url");
                return false;
            }

            StorageFile fileWallpaper = await ApplicationData.Current.LocalFolder.CreateFileAsync(setDesktopOrLock ? "desktop" : "lock",
                    CreationCollisionOption.ReplaceExisting);
            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation operation = downloader.CreateDownload(new Uri(urlImg), fileWallpaper);
            DownloadOperation resOperation = await operation.StartAsync().AsTask();
            if (resOperation.Progress.Status != BackgroundTransferStatus.Completed) {
                LogUtil.I("PushService.SetWallpaper() download error");
                return false;
            }
            if (offset >= 0 && ratio >= 0) {
                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasBitmap bitmap = null;
                using (var stream = await fileWallpaper.OpenReadAsync()) {
                    bitmap = await CanvasBitmap.LoadAsync(device, stream);
                }
                if (bitmap == null) {
                    return false;
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
                fileWallpaper = await ApplicationData.Current.LocalFolder.CreateFileAsync(setDesktopOrLock ? "desktop-reset" : "lock-reset",
                    CreationCollisionOption.ReplaceExisting);
                await target.SaveAsync(fileWallpaper.Path, CanvasBitmapFileFormat.Png);
            }
            UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
            return setDesktopOrLock ? await profileSettings.TrySetWallpaperImageAsync(fileWallpaper)
                : await profileSettings.TrySetLockScreenImageAsync(fileWallpaper);
        }

        private async Task<bool> LoadBing(bool setDesktopOrLock) {
            const string URL_API_HOST = "https://global.bing.com";
            const string URL_API = URL_API_HOST + "/HPImageArchive.aspx?pid=hp&format=js&uhd=1&idx=0&n=1";
            LogUtil.I("PushService.LoadBing() api url: " + URL_API);
            HttpClient client = new HttpClient();
            string jsonData = await client.GetStringAsync(URL_API);
            BingApi bing = JsonConvert.DeserializeObject<BingApi>(jsonData);
            string urlUhd = string.Format("{0}{1}_UHD.jpg", URL_API_HOST, bing.Images[0].UrlBase);
            LogUtil.I("PushService.LoadBing() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock);
        }

        private async Task<bool> LoadNasa(bool setDesktopOrLock) {
            string urlApi = string.Format("https://api.nasa.gov/planetary/apod?api_key=DEMO_KEY&thumbs=True&start_date={0}&end_date={1}",
                DateTime.UtcNow.AddHours(-4).AddDays(-6).ToString("yyyy-MM-dd"), DateTime.UtcNow.AddHours(-4).ToString("yyyy-MM-dd"));
            LogUtil.I("PushService.LoadNasa() api url: " + urlApi);
            HttpClient client = new HttpClient();
            string jsonData = await client.GetStringAsync(urlApi);
            IList<NasaApiItem> items = JsonConvert.DeserializeObject<IList<NasaApiItem>>(jsonData);
            string urlUhd = null;
            for (int i = items.Count - 1; i >= 0; --i) { // 取最近日期
                if ("image".Equals(items[i].MediaType)) {
                    urlUhd = items[i].HdUrl;
                    break;
                }
            }
            LogUtil.I("PushService.LoadBing() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock);
        }

        private async Task<bool> LoadOneplus(bool setDesktopOrLock) {
            OneplusRequest request = new OneplusRequest {
                PageSize = 1,
                CurrentPage = 1,
                SortMethod = "1" // 默认按：最新添加
            };
            string requestStr = JsonConvert.SerializeObject(request);
            const string URL_API = "https://photos.oneplus.com/cn/shot/photo/schedule";
            LogUtil.I("PushService.LoadOneplus() api url: " + URL_API + " " + requestStr);
            HttpClient client = new HttpClient();
            HttpContent content = new StringContent(requestStr);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await client.PostAsync(URL_API, content);
            _ = response.EnsureSuccessStatusCode();
            string jsonData = await response.Content.ReadAsStringAsync();
            OneplusApi oneplusApi = JsonConvert.DeserializeObject<OneplusApi>(jsonData);
            string urlUhd = oneplusApi.Items[0].PhotoUrl;
            LogUtil.I("PushService.LoadBing() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock);
        }

        private async Task<bool> LoadTimeline(bool setDesktopOrLock) {
            const string URL_API = "https://api.nguaduot.cn/timeline/today?client=timelinewallpaper";
            LogUtil.I("PushService.LoadTimeline() api url: " + URL_API);
            HttpClient client = new HttpClient(new HttpClientHandler {
                AllowAutoRedirect = false
            });
            HttpResponseMessage msg = await client.GetAsync(URL_API);
            string urlUhd = msg.Headers.Location.AbsoluteUri;
            LogUtil.I("PushService.LoadTimeline() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock);
        }

        private async Task<bool> LoadOne(bool setDesktopOrLock) {
            const string URL_TOKEN = "http://m.wufazhuce.com/one";
            const string URL_API = "http://m.wufazhuce.com/one/ajaxlist/0?_token={0}";
            HttpClient client = new HttpClient();
            HttpResponseMessage msg = await client.GetAsync(URL_TOKEN); // cookie 无需手动取，自动包含
            string htmlData = await msg.Content.ReadAsStringAsync();
            Match match = Regex.Match(htmlData, @"One.token ?= ?[""'](.+?)[""']");
            string token = match.Groups[1].Value;
            string urlApi = string.Format(URL_API, token);
            LogUtil.I("PushService.LoadOne() api url: " + urlApi);
            string jsonData = await client.GetStringAsync(urlApi);
            match = Regex.Match(jsonData, @"""img_url"": ?""(.+?)""");
            string urlUhd = Regex.Unescape(match.Groups[1].Value); // 反转义
            LogUtil.I("PushService.LoadOne() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock);
        }

        private async Task<bool> LoadHimawari8(bool setDesktopOrLock) {
            const string URL_API = "https://himawari8-dl.nict.go.jp/himawari8/img/D531106/latest.json";
            const string URL_IMG = "https://himawari8.nict.go.jp/img/D531106/1d/550/{0}/{1}_0_0.png";
            LogUtil.I("PushService.LoadHimawari8() api url: " + URL_API);
            HttpClient client = new HttpClient();
            string jsonData = await client.GetStringAsync(URL_API);
            Match match = Regex.Match(jsonData, @"""date"": ?""(.+?)""");
            DateTime time = DateTime.ParseExact(match.Groups[1].Value, "yyyy-MM-dd HH:mm:ss",
                new System.Globalization.CultureInfo("en-US"));
            string urlUhd = string.Format(URL_IMG, time.ToString(@"yyyy\/MM\/dd"),
                string.Format("{0}{1}000", time.ToString("HH"), time.Minute / 10));
            LogUtil.I("PushService.LoadHimawari8() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock, ini.Himawari8.Offset, ini.Himawari8.Ratio);
        }

        private async Task<bool> LoadYmyouli(bool setDesktopOrLock) {
            const string URL_API = "https://api.nguaduot.cn/ymyouli/random?client=timelinewallpaper&cate={0}";
            string urlApi = string.Format(URL_API, ini.Ymyouli.Cate);
            LogUtil.I("PushService.LoadYmyouli() api url: " + urlApi);
            HttpClient client = new HttpClient(new HttpClientHandler {
                AllowAutoRedirect = false
            });
            HttpResponseMessage msg = await client.GetAsync(urlApi);
            string urlUhd = msg.Headers.Location.AbsoluteUri;
            LogUtil.I("PushService.LoadYmyouli() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock);
        }

        private async Task<bool> LoadWallhaven(bool setDesktopOrLock) {
            const string URL_API = "https://api.nguaduot.cn/wallhaven/random?client=timelinewallpaper&cate={0}";
            string urlApi = string.Format(URL_API, ini.Wallhaven.Cate);
            LogUtil.I("PushService.LoadWallhaven() api url: " + urlApi);
            HttpClient client = new HttpClient(new HttpClientHandler {
                AllowAutoRedirect = false
            });
            HttpResponseMessage msg = await client.GetAsync(urlApi);
            string urlUhd = msg.Headers.Location.AbsoluteUri;
            LogUtil.I("PushService.LoadWallhaven() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock);
        }

        private async Task<bool> LoadQingbz(bool setDesktopOrLock) {
            const string URL_API = "https://api.nguaduot.cn/qingbz/random?client=timelinewallpaper&cate={0}";
            string urlApi = string.Format(URL_API, ini.Qingbz.Cate);
            LogUtil.I("PushService.LoadQingbz() api url: " + urlApi);
            HttpClient client = new HttpClient(new HttpClientHandler {
                AllowAutoRedirect = false
            });
            HttpResponseMessage msg = await client.GetAsync(urlApi);
            string urlUhd = msg.Headers.Location.AbsoluteUri;
            LogUtil.I("PushService.LoadQingbz() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock);
        }

        private async Task<bool> LoadWallhere(bool setDesktopOrLock) {
            const string URL_API = "https://api.nguaduot.cn/wallhere/random?client=timelinewallpaper&cate={0}";
            string urlApi = string.Format(URL_API, ini.Wallhere.Cate);
            LogUtil.I("PushService.LoadWallhere() api url: " + urlApi);
            HttpClient client = new HttpClient(new HttpClientHandler {
                AllowAutoRedirect = false
            });
            HttpResponseMessage msg = await client.GetAsync(urlApi);
            string urlUhd = msg.Headers.Location.AbsoluteUri;
            LogUtil.I("PushService.LoadWallhere() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock);
        }

        private async Task<bool> LoadInfinity(bool setDesktopOrLock) {
            const string URL_API = "https://infinity-api.infinitynewtab.com/random-wallpaper?_={0}";
            string urlApi = string.Format(URL_API,
                (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
            LogUtil.I("PushService.LoadInfinity() api url: " + urlApi);
            HttpClient client = new HttpClient();
            string jsonData = await client.GetStringAsync(urlApi);
            Match match = Regex.Match(jsonData, @"""rawSrc"": ?""(.+?)""");
            string urlUhd = match.Groups[1].Value;
            LogUtil.I("PushService.LoadInfinity() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock);
        }

        private async Task<bool> LoadObzhi(bool setDesktopOrLock) {
            const string URL_API = "https://api.nguaduot.cn/obzhi/random?client=timelinewallpaper&cate={0}";
            string urlApi = string.Format(URL_API, ini.Qingbz.Cate);
            LogUtil.I("PushService.LoadObzhi() api url: " + urlApi);
            HttpClient client = new HttpClient(new HttpClientHandler {
                AllowAutoRedirect = false
            });
            HttpResponseMessage msg = await client.GetAsync(urlApi);
            string urlUhd = msg.Headers.Location.AbsoluteUri;
            LogUtil.I("PushService.LoadObzhi() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock);
        }

        private async Task<bool> LoadLsp(bool setDesktopOrLock) {
            const string URL_API = "https://api.nguaduot.cn/lsp/random?client=timelinewallpaper&cate={0}";
            string urlApi = string.Format(URL_API, ini.Lsp.Cate);
            LogUtil.I("PushService.LoadLsp() api url: " + urlApi);
            HttpClient client = new HttpClient(new HttpClientHandler {
                AllowAutoRedirect = false
            });
            HttpResponseMessage msg = await client.GetAsync(urlApi);
            string urlUhd = msg.Headers.Location.AbsoluteUri;
            LogUtil.I("PushService.LoadLsp() img url: " + urlUhd);
            return await SetWallpaper(urlUhd, setDesktopOrLock);
        }
    }
}
