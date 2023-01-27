using Microsoft.Graphics.Canvas;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;

namespace TimelineService {
    public sealed class PushService : IBackgroundTask {
        BackgroundTaskDeferral _deferral;
        private ApplicationDataContainer localSettings;
        private Ini ini;
        private float periodDesktop = 24;
        private float periodLock = 24;
        private float periodToast = 24;
        private float periodTile = 2;
        private string tagDesktop; // 免重复推送桌面背景标记
        private string tagLock; // 免重复推送锁屏背景标记
        private string tagToast; // 免重复推送磁贴背景标记
        private string tagTile; // 免重复推送磁贴背景标记
        private bool pushNow = false; // 立即运行一次

        private enum Action {
            Desktop, Lock, Toast, Tile
        }

        public async void Run(IBackgroundTaskInstance taskInstance) {
            _deferral = taskInstance.GetDeferral();
            // 初始化
            Init(taskInstance);
            LogUtil.I("Run() trigger: " + taskInstance.TriggerDetails);
            LogUtil.I("Run() desktop: " + ini.DesktopProvider + ", " + periodDesktop);
            LogUtil.I("Run() lock: " + ini.LockProvider + ", " + periodLock);
            LogUtil.I("Run() toast: " + ini.ToastProvider + ", " + periodToast);
            LogUtil.I("Run() tile: " + (!string.IsNullOrEmpty(ini.TileProvider)
                ? ini.TileProvider : ini.Provider) + ", " + periodTile);
            // 检查网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                LogUtil.W("Run() network not available");
                _deferral.Complete();
                return;
            }
            bool pushDesktop = CheckDesktopNecessary();
            bool pushLock = CheckLockNecessary();
            bool pushToast = CheckToastNecessary();
            bool pushTile = await CheckTileNecessaryAsync();
            LogUtil.I("Run() CheckNecessary: " + pushDesktop + " " + pushLock + " " + pushToast + " " + pushTile);
            if (!pushDesktop && !pushLock && !pushToast && !pushTile) { // 本次无需推送
                _deferral.Complete();
                return;
            }
            // 上传统计
            IReadOnlyDictionary<string, int> dosage = await FileUtil.ReadDosage();
            double scaleFactor = (double)(localSettings.Values["Scale"] ?? 0);
            double diagonalInch = (double)(localSettings.Values["Diagonal"] ?? 0);
            long resolution = (long)(localSettings.Values["Resolution"] ?? 0);
            long resW = (resolution & 0xffff0000) >> 16;
            long resH = resolution & 0x0000ffff;
            await Api.Stats(ini, dosage.GetValueOrDefault("all", 0), 0,
                string.Format("{0}x{1},{2},{3}", resW, resH, scaleFactor.ToString("0.00"), diagonalInch.ToString("0.0")));
            // 在缓存可能被使用之前清理缓存
            await FileUtil.ClearCache(ini.Cache);
            // 开始推送
            try {
                if (pushDesktop) {
                    await PushDesktopAsync();
                }
                if (pushLock) {
                    await PushLockAsync();
                }
                // TODO
                //if (pushToast) {
                //    await PushToastAsync();
                //}
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
            periodToast = ini.GetToastPeriod(ini.ToastProvider);
            periodTile = ini.GetTilePeriod(ini.TileProvider);
            int dayMinutes = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
            tagDesktop = string.Format("{0}{1}-{2}", DateTime.Now.ToString("yyyyMMdd"),
                Math.Ceiling(dayMinutes / (periodDesktop * 60)), ini.DesktopProvider);
            tagLock = string.Format("{0}{1}-{2}", DateTime.Now.ToString("yyyyMMdd"),
                Math.Ceiling(dayMinutes / (periodLock * 60)), ini.LockProvider);
            tagToast = string.Format("{0}{1}-{2}", DateTime.Now.ToString("yyyyMMdd"),
                Math.Ceiling(dayMinutes / (periodToast * 60)), ini.ToastProvider);
            tagTile = string.Format("{0}{1}-{2}", DateTime.Now.ToString("yyyyMMdd"),
                Math.Ceiling(dayMinutes / (periodTile * 60)),
                !string.IsNullOrEmpty(ini.TileProvider) ? ini.TileProvider : ini.Provider);

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
            if (pushNow) { // 立即运行一次
                return ini.DesktopProvider.Equals(ini.Provider);
            }
            return !localSettings.Values.ContainsKey(tagDesktop);
        }

        private bool CheckLockNecessary() {
            if (string.IsNullOrEmpty(ini.LockProvider)) { // 未开启推送
                return false;
            }
            if (!UserProfilePersonalizationSettings.IsSupported()) { // 检查是否支持修改背景
                LogUtil.W("CheckLockNecessary() not supported");
                return false;
            }
            if (pushNow) { // 立即运行一次
                return ini.LockProvider.Equals(ini.Provider);
            }
            return !localSettings.Values.ContainsKey(tagLock);
        }

        private bool CheckToastNecessary() {
            if (string.IsNullOrEmpty(ini.ToastProvider)) { // 未开启推送
                return false;
            }
            if (pushNow) { // 立即运行一次
                return ini.ToastProvider.Equals(ini.Provider);
            }
            return !localSettings.Values.ContainsKey(tagToast);
        }

        private async Task<bool> CheckTileNecessaryAsync() {
            if (SysUtil.GetOsVerMajor() != 10) { // 检查 Win10
                return false;
            }
            AppListEntry entry = (await Package.Current.GetAppListEntriesAsync())[0];
            if (!await StartScreenManager.GetDefault().ContainsAppListEntryAsync(entry)) { // 检查当前是否已固定
                return false;
            }
            if (pushNow) { // 立即运行一次
                return true;
            }
            return !localSettings.Values.ContainsKey(tagTile);
        }

        private async Task<bool> PushDesktopAsync() {
            await FileUtil.WriteDosage(ini.DesktopProvider);
            bool res = false;
            if (LocalIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadLocalAsync(Action.Desktop);
            } else if (BingIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadBingAsync(Action.Desktop);
            } else if (NasaIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadNasaAsync(Action.Desktop);
            } else if (OneplusIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadOneplusAsync(Action.Desktop);
            } else if (TimelineIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadTimelineAsync(Action.Desktop);
            } else if (OneIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadOneAsync(Action.Desktop);
            } else if (IhansenIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadIhansenAsync(Action.Desktop);
            } else if (Himawari8Ini.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadHimawari8Async(Action.Desktop);
            } else if (YmyouliIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadYmyouliAsync(Action.Desktop);
            } else if (QingbzIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadQingbzAsync(Action.Desktop);
            } else if (WallhavenIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadWallhavenAsync(Action.Desktop);
            } else if (WallhereIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadWallhereAsync(Action.Desktop);
            } else if (ZzzmhIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadZzzmhAsync(Action.Desktop);
            } else if (ToopicIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadToopicAsync(Action.Desktop);
            } else if (NetbianIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadNetbianAsync(Action.Desktop);
            } else if (BackieeIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadBackieeAsync(Action.Desktop);
            } else if (SkitterIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadSkitterAsync(Action.Desktop);
            } else if (InfinityIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadInfinityAsync(Action.Desktop);
            } else if (WallpaperupIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadWallpaperupAsync(Action.Desktop);
            } else if (ObzhiIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadObzhiAsync(Action.Desktop);
            } else if (GluttonIni.GetId().Equals(ini.DesktopProvider)) {
                res = await LoadGluttonAsync(Action.Desktop);
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
            if (LocalIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadLocalAsync(Action.Lock);
            } else if (BingIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadBingAsync(Action.Lock);
            } else if (NasaIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadNasaAsync(Action.Lock);
            } else if (OneplusIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadOneplusAsync(Action.Lock);
            } else if (TimelineIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadTimelineAsync(Action.Lock);
            } else if (OneIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadOneAsync(Action.Lock);
            } else if (IhansenIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadIhansenAsync(Action.Lock);
            } else if (Himawari8Ini.GetId().Equals(ini.LockProvider)) {
                res = await LoadHimawari8Async(Action.Lock);
            } else if (YmyouliIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadYmyouliAsync(Action.Lock);
            } else if (QingbzIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadQingbzAsync(Action.Lock);
            } else if (WallhavenIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadWallhavenAsync(Action.Lock);
            } else if (WallhereIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadWallhereAsync(Action.Lock);
            } else if (ZzzmhIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadZzzmhAsync(Action.Lock);
            } else if (ToopicIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadToopicAsync(Action.Lock);
            } else if (NetbianIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadNetbianAsync(Action.Lock);
            } else if (BackieeIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadBackieeAsync(Action.Lock);
            } else if (SkitterIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadSkitterAsync(Action.Lock);
            } else if (InfinityIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadInfinityAsync(Action.Lock);
            } else if (WallpaperupIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadWallpaperupAsync(Action.Lock);
            } else if (ObzhiIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadObzhiAsync(Action.Lock);
            } else if (GluttonIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadGluttonAsync(Action.Lock);
            } else if (LspIni.GetId().Equals(ini.LockProvider)) {
                res = await LoadLspAsync(Action.Lock);
            }
            if (res) {
                localSettings.Values[tagLock] = (int)(localSettings.Values[tagLock] ?? 0) + 1;
            }
            LogUtil.I("PushLockAsync() " + res);
            return res;
        }

        private async Task<bool> PushToastAsync() {
            await FileUtil.WriteDosage(ini.ToastProvider);
            bool res = false;
            if (LocalIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadLocalAsync(Action.Toast);
            } else if (BingIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadBingAsync(Action.Toast);
            } else if (NasaIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadNasaAsync(Action.Toast);
            } else if (OneplusIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadOneplusAsync(Action.Toast);
            } else if (TimelineIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadTimelineAsync(Action.Toast);
            } else if (OneIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadOneAsync(Action.Toast);
            } else if (IhansenIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadIhansenAsync(Action.Toast);
            } else if (Himawari8Ini.GetId().Equals(ini.ToastProvider)) {
                res = await LoadHimawari8Async(Action.Toast);
            } else if (YmyouliIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadYmyouliAsync(Action.Toast);
            } else if (QingbzIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadQingbzAsync(Action.Toast);
            } else if (WallhavenIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadWallhavenAsync(Action.Toast);
            } else if (WallhereIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadWallhereAsync(Action.Toast);
            } else if (ZzzmhIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadZzzmhAsync(Action.Toast);
            } else if (ToopicIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadToopicAsync(Action.Toast);
            } else if (NetbianIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadNetbianAsync(Action.Toast);
            } else if (BackieeIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadBackieeAsync(Action.Toast);
            } else if (SkitterIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadSkitterAsync(Action.Toast);
            } else if (InfinityIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadInfinityAsync(Action.Toast);
            } else if (WallpaperupIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadWallpaperupAsync(Action.Toast);
            } else if (ObzhiIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadObzhiAsync(Action.Toast);
            } else if (GluttonIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadGluttonAsync(Action.Toast);
            } else if (LspIni.GetId().Equals(ini.ToastProvider)) {
                res = await LoadLspAsync(Action.Toast);
            }
            if (res) {
                localSettings.Values[tagLock] = (int)(localSettings.Values[tagLock] ?? 0) + 1;
            }
            LogUtil.I("PushToastAsync() " + res);
            return res;
        }

        private async Task<bool> PushTileAsync() {
            string tileProvider = !string.IsNullOrEmpty(ini.TileProvider) ? ini.TileProvider : ini.Provider;
            await FileUtil.WriteDosage(tileProvider);
            bool res;
            if (LocalIni.GetId().Equals(tileProvider)) {
                res = await LoadLocalAsync(Action.Tile);
            } else if (BingIni.GetId().Equals(tileProvider)) {
                res = await LoadBingAsync(Action.Tile);
            } else if (NasaIni.GetId().Equals(tileProvider)) {
                res = await LoadNasaAsync(Action.Tile);
            } else if (OneplusIni.GetId().Equals(tileProvider)) {
                res = await LoadOneplusAsync(Action.Tile);
            } else if (TimelineIni.GetId().Equals(tileProvider)) {
                res = await LoadTimelineAsync(Action.Tile);
            } else if (OneIni.GetId().Equals(tileProvider)) {
                res = await LoadOneAsync(Action.Tile);
            } else if (IhansenIni.GetId().Equals(tileProvider)) {
                res = await LoadIhansenAsync(Action.Tile);
            } else if (Himawari8Ini.GetId().Equals(tileProvider)) {
                res = await LoadHimawari8Async(Action.Tile);
            } else if (YmyouliIni.GetId().Equals(tileProvider)) {
                res = await LoadYmyouliAsync(Action.Tile);
            } else if (QingbzIni.GetId().Equals(tileProvider)) {
                res = await LoadQingbzAsync(Action.Tile);
            } else if (WallhavenIni.GetId().Equals(tileProvider)) {
                res = await LoadWallhavenAsync(Action.Tile);
            } else if (WallhereIni.GetId().Equals(tileProvider)) {
                res = await LoadWallhereAsync(Action.Tile);
            } else if (ZzzmhIni.GetId().Equals(tileProvider)) {
                res = await LoadZzzmhAsync(Action.Tile);
            } else if (ToopicIni.GetId().Equals(tileProvider)) {
                res = await LoadToopicAsync(Action.Tile);
            } else if (NetbianIni.GetId().Equals(tileProvider)) {
                res = await LoadNetbianAsync(Action.Tile);
            } else if (BackieeIni.GetId().Equals(tileProvider)) {
                res = await LoadBackieeAsync(Action.Tile);
            } else if (SkitterIni.GetId().Equals(tileProvider)) {
                res = await LoadSkitterAsync(Action.Tile);
            } else if (InfinityIni.GetId().Equals(tileProvider)) {
                res = await LoadInfinityAsync(Action.Tile);
            } else if (WallpaperupIni.GetId().Equals(tileProvider)) {
                res = await LoadWallpaperupAsync(Action.Tile);
            } else if (ObzhiIni.GetId().Equals(tileProvider)) {
                res = await LoadObzhiAsync(Action.Tile);
            } else if (GluttonIni.GetId().Equals(tileProvider)) {
                res = await LoadGluttonAsync(Action.Tile);
            } else if (LspIni.GetId().Equals(tileProvider)) {
                res = await LoadLspAsync(Action.Tile);
            } else {
                res = await LoadBingAsync(Action.Tile);
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

        private async Task<bool> SetLockBgAsync(StorageFile fileImg) {
            BasicProperties properties = await fileImg.GetBasicPropertiesAsync();
            if (properties.Size == 0) {
                return false;
            }
            return await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(fileImg);
        }

        private bool ShowToast(string urlThumb) {
            if (string.IsNullOrEmpty(urlThumb)) {
                return false;
            }
            // ResourceLoader.GetForCurrentView().GetString("ToastTitle")
            string content = string.Format("<toast><visual>" +
                "<binding template='ToastGeneric'>" +
                "<image src='{0}' placement='hero' />" +
                "<text hint-maxLines='1'>{1}</text>" +
                "</binding></visual>" +
                "<actions>" +
                "<action content='{2}' arguments='' activationType='background' />" +
                "<action content='{3}' arguments='' activationType='background' />" +
                "</actions></toast>", urlThumb, "今日一图", "设为桌面", "设为锁屏");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);
            ToastNotification toast = new ToastNotification(doc) {
                ExpirationTime = DateTime.Now.AddDays(1)
            };
            ToastNotificationManager.CreateToastNotifier().Show(toast);
            return true;
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

        private async Task<bool> SetTileBgForHimawari8(StorageFile fileImg) {
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
                case Action.Toast:
                    cacheName = "toast";
                    break;
                case Action.Tile:
                default:
                    cacheName = "tile";
                    break;
            }
            StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("wallpaper",
                CreationCollisionOption.OpenIfExists);
            StorageFile file = await folder.CreateFileAsync(string.Format("{0}-{1}.jpg", cacheName, DateTime.Now.ToString("yyyyMMddHH00")),
                CreationCollisionOption.ReplaceExisting);
            if (string.IsNullOrEmpty(urlImg)) {
                LogUtil.E("DownloadImgAsync() invalid url");
                return file;
            }
            Uri uriImg = new Uri(urlImg);
            if (uriImg.IsFile) {
                StorageFile srcFile = await StorageFile.GetFileFromPathAsync(urlImg);
                await srcFile.CopyAndReplaceAsync(file);
            } else {
                BackgroundDownloader downloader = new BackgroundDownloader();
                DownloadOperation operation = downloader.CreateDownload(uriImg, file);
                DownloadOperation resOperation = await operation.StartAsync().AsTask();
                if (resOperation.Progress.Status != BackgroundTransferStatus.Completed) {
                    LogUtil.E("DownloadImgAsync() download error");
                    return file;
                }
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
                long screen = (long)(localSettings.Values["Resolution"] ?? 0);
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
                await file.DeleteAsync(); // 删除原图
                file = await folder.CreateFileAsync(string.Format("{0}-reset-{1}.png", cacheName, DateTime.Now.ToString("yyyyMMddHH00")),
                    CreationCollisionOption.ReplaceExisting);
                await target.SaveAsync(file.Path, CanvasBitmapFileFormat.Png, 1.0f);
            }
            IReadOnlyList<StorageFile> oldCacheFiles = (await folder.GetFilesAsync())
                .Where(f => f.Name.StartsWith(cacheName) && !f.Name.Equals(file.Name))
                .OrderByDescending(f => f.Name).ToList();
            if (oldCacheFiles.Count > 0) { // 删除上次重图
                BasicProperties propertiesLast = await oldCacheFiles[0].GetBasicPropertiesAsync();
                BasicProperties propertiesThis = await file.GetBasicPropertiesAsync();
                if (propertiesLast.Size == propertiesThis.Size) {
                    LogUtil.I("delete duplicated: " + oldCacheFiles[0].Name + " (this: " + file.Name);
                    await oldCacheFiles[0].DeleteAsync();
                }
            }
            return file;
        }

        private async Task<bool> LoadLocalAsync(Action action) {
            StorageFolder folder = null;
            if (!string.IsNullOrEmpty(ini.Local.Folder)) {
                try {
                    folder = await StorageFolder.GetFolderFromPathAsync(ini.Local.Folder.Replace("/", "\\"));
                } catch (FileNotFoundException ex) { // 指定的文件夹不存在
                    LogUtil.E("LoadLocalAsync() " + ex.Message);
                } catch (UnauthorizedAccessException ex) { // 您无权访问指定文件夹
                    LogUtil.E("LoadLocalAsync() " + ex.Message);
                } catch (ArgumentException ex) { // 路径不能是相对路径或 URI
                    LogUtil.E("LoadLocalAsync() " + ex.Message);
                } catch (Exception ex) {
                    LogUtil.E("LoadLocalAsync() " + ex.Message);
                }
            }
            if (folder == null && !string.IsNullOrEmpty(ini.Folder)) {
                try {
                    folder = await StorageFolder.GetFolderFromPathAsync(ini.Folder.Replace("/", "\\"));
                } catch (FileNotFoundException ex) { // 指定的文件夹不存在
                    LogUtil.E("LoadLocalAsync() " + ex.Message);
                } catch (UnauthorizedAccessException ex) { // 您无权访问指定文件夹
                    LogUtil.E("LoadLocalAsync() " + ex.Message);
                } catch (ArgumentException ex) { // 路径不能是相对路径或 URI
                    LogUtil.E("LoadLocalAsync() " + ex.Message);
                } catch (Exception ex) {
                    LogUtil.E("LoadLocalAsync() " + ex.Message);
                }
            }
            if (folder == null) {
                folder = await KnownFolders.PicturesLibrary.CreateFolderAsync(AppInfo.Current.DisplayInfo.DisplayName,
                    CreationCollisionOption.OpenIfExists);
            }
            IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
            if (ini.Local.Depth > 0) { // 第一层
                foreach (StorageFolder folder1 in await folder.GetFoldersAsync()) {
                    files = files.Concat(await folder1.GetFilesAsync()).ToArray();
                    if (ini.Local.Depth > 1) { // 第二层
                        foreach (StorageFolder folder2 in await folder1.GetFoldersAsync()) {
                            files = files.Concat(await folder2.GetFilesAsync()).ToArray();
                        }
                    }
                }
            }
            List<StorageFile> srcFiles = new List<StorageFile>();
            foreach (StorageFile file in files) {
                if (file.ContentType.StartsWith("image")) {
                    srcFiles.Add(file);
                }
            }
            StorageFile fileSrc = srcFiles[new Random().Next(srcFiles.Count)];
            LogUtil.I("LoadLocalAsync() img file: " + fileSrc.Path);
            if (action == Action.Toast || action == Action.Tile) {
                // 生成缩略图
                // TODO：无法保持原图比例
                StorageFolder folderWp = await ApplicationData.Current.LocalFolder.CreateFolderAsync("wallpaper",
                    CreationCollisionOption.OpenIfExists);
                StorageFile fileThumb = await folderWp.CreateFileAsync(string.Format("tile-{0}.jpg", DateTime.Now.ToString("yyyyMMddHH00")),
                    CreationCollisionOption.ReplaceExisting);
                StorageItemThumbnail thumb = await fileSrc.GetThumbnailAsync(ThumbnailMode.PicturesView);
                Windows.Storage.Streams.Buffer buffer = new Windows.Storage.Streams.Buffer(Convert.ToUInt32(thumb.Size));
                using (var stream = await fileThumb.OpenAsync(FileAccessMode.ReadWrite)) {
                    await stream.WriteAsync(await thumb.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.None));
                }
                LogUtil.I("LoadLocalAsync() thumb file: " + fileThumb.Path);
                if (action == Action.Toast) {
                    return ShowToast(fileThumb.Path);
                } else {
                    return SetTileBg(fileThumb.Path);
                }
            } else {
                StorageFile fileImg = await DownloadImgAsync(fileSrc.Path, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadBingAsync(Action action) {
            const string URL_API_HOST = "https://global.bing.com";
            const string URL_CND_HOST = "http://s.cn.bing.net";
            const string URL_API = URL_API_HOST + "/HPImageArchive.aspx?pid=hp&format=js&uhd=1&idx=0&n=1";
            LogUtil.I("LoadBingAsync() api url: " + URL_API);
            bool cn = "CN".Equals(GlobalizationPreferences.HomeGeographicRegion);
            HttpClient client = new HttpClient();
            string jsonData = await client.GetStringAsync(URL_API);
            BingApi api = JsonConvert.DeserializeObject<BingApi>(jsonData);
            if (action == Action.Toast || action == Action.Tile) {
                string urlThumb = string.Format("{0}{1}_400x240.jpg", cn ? URL_CND_HOST : URL_API_HOST, api.Images[0].UrlBase);
                LogUtil.I("LoadBingAsync() thumb url: " + urlThumb);
                if (action == Action.Toast) {
                    return ShowToast(urlThumb);
                } else {
                    return SetTileBg(urlThumb);
                }
            } else {
                string urlUhd = string.Format("{0}{1}_UHD.jpg", cn ? URL_CND_HOST : URL_API_HOST, api.Images[0].UrlBase);
                LogUtil.I("LoadBingAsync() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadNasaAsync(Action action) {
            NasaApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(NasaIni.GetId(), "date", "");
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    NasaApi api = JsonConvert.DeserializeObject<NasaApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadNasaAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadNasaAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/nasa/v2?client=timelinewallpaper&order=date";
                LogUtil.I("LoadNasaAsync() api url: " + URL_API);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(URL_API);
                NasaApi api = JsonConvert.DeserializeObject<NasaApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(NasaIni.GetId(), "date", "", jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadNasaAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadNasaAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadTimelineAsync(Action action) {
            TimelineApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(TimelineIni.GetId(), "date", "");
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    data = JsonConvert.DeserializeObject<TimelineApi>(jsonData).Data[0];
                    LogUtil.I("LoadTimelineAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadTimelineAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/timeline/v2?client=timelinewallpaper&order=date&cate=";
                LogUtil.I("LoadTimelineAsync() api url: " + URL_API);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(URL_API);
                data = JsonConvert.DeserializeObject<TimelineApi>(jsonData).Data[0];
                await FileUtil.WriteProviderCache(TimelineIni.GetId(), "date", "", jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadTimelineAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadTimelineAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
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
            LogUtil.I("LoadOneAsync() api url: " + urlApi);
            string jsonData = await client.GetStringAsync(urlApi);
            match = Regex.Match(jsonData, @"""img_url"": ?""(.+?)""");
            string urlUhd = Regex.Unescape(match.Groups[1].Value); // 反转义
            LogUtil.I("LoadOneAsync() img url: " + urlUhd);
            if (action == Action.Toast || action == Action.Tile) {
                if (action == Action.Toast) {
                    return ShowToast(urlUhd);
                } else {
                    return SetTileBg(urlUhd);
                }
            } else {
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadIhansenAsync(Action action) {
            const string URL_API = "https://api.ihansen.org/img/detail?page=0&perPage=10&index=&orderBy=today&tag=&favorites=";
            LogUtil.I("LoadIhansenAsync() api url: " + URL_API);
            HttpClient client = new HttpClient();
            string jsonData = await client.GetStringAsync(URL_API);
            List<IhansenApi> api = JsonConvert.DeserializeObject<List<IhansenApi>>(jsonData);
            IhansenApi target = api.OrderBy(p => new Random().NextDouble())
                .FirstOrDefault(p => p.Width >= p.Height);
            if (action == Action.Toast || action == Action.Tile) {
                string urlThumb = target.SmallUrl;
                LogUtil.I("LoadIhansenAsync() thumb url: " + urlThumb);
                if (action == Action.Toast) {
                    return ShowToast(urlThumb);
                } else {
                    return SetTileBg(urlThumb);
                }
            } else {
                string urlUhd = target.Raw;
                LogUtil.I("LoadIhansenAsync() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadHimawari8Async(Action action) {
            const string URL_API = "https://himawari8.nict.go.jp/img/D531106/latest.json?_=";
            const string URL_IMG = "https://himawari8.nict.go.jp/img/D531106/1d/550/{0}_0_0.png";
            string urlApi = URL_API + DateUtil.CurrentTimeMillis();
            LogUtil.I("LoadHimawari8Async() api url: " + urlApi);
            HttpClient client = new HttpClient();
            string jsonData = await client.GetStringAsync(urlApi);
            //Match match = Regex.Match(jsonData, @"""date"": ?""(.+?)""");
            //DateTime time = DateTime.ParseExact(match.Groups[1].Value, "yyyy-MM-dd HH:mm:ss",
            //    new System.Globalization.CultureInfo("en-US"));
            Himawari8Api api = JsonConvert.DeserializeObject<Himawari8Api>(jsonData);
            if (!DateTime.TryParse(api.Date, out DateTime time)) { // UTC时间
                time = DateTime.UtcNow.AddMinutes(-25);
            }
            string urlUhd = string.Format(URL_IMG, time.ToString(@"yyyy\/MM\/dd\/HHmmss"));
            LogUtil.I("LoadHimawari8Async() img url: " + urlUhd);
            if (action == Action.Toast || action == Action.Tile) {
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Toast) {
                    return ShowToast(fileImg.Path);
                } else {
                    return await SetTileBgForHimawari8(fileImg);
                }
            } else {
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action, ini.Himawari8.Offset, ini.Himawari8.Ratio);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadYmyouliAsync(Action action) {
            YmyouliApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(YmyouliIni.GetId(), ini.Ymyouli.Order, ini.Ymyouli.Cate);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    YmyouliApi api = JsonConvert.DeserializeObject<YmyouliApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadYmyouliAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadYmyouliAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/ymyouli/v2?client=timelinewallpaper&order={0}&cate={1}";
                string urlApi = string.Format(URL_API, ini.Ymyouli.Order, ini.Ymyouli.Cate);
                LogUtil.I("LoadYmyouliAsync() api url: " + urlApi);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(urlApi);
                YmyouliApi api = JsonConvert.DeserializeObject<YmyouliApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(YmyouliIni.GetId(), ini.Ymyouli.Order, ini.Ymyouli.Cate, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadYmyouliAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadYmyouliAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                Debug.WriteLine("LoadYmyouliAsync() img file: " + fileImg?.Path);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadQingbzAsync(Action action) {
            QingbzApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(QingbzIni.GetId(), ini.Qingbz.Order, ini.Qingbz.Cate);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    QingbzApi api = JsonConvert.DeserializeObject<QingbzApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadQingbzAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadQingbzAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/qingbz/v2?client=timelinewallpaper&order={0}&cate={1}";
                string urlApi = string.Format(URL_API, ini.Qingbz.Order, ini.Qingbz.Cate);
                LogUtil.I("LoadQingbzAsync() api url: " + urlApi);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(urlApi);
                QingbzApi api = JsonConvert.DeserializeObject<QingbzApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(QingbzIni.GetId(), ini.Qingbz.Order, ini.Qingbz.Cate, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadQingbzAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadQingbzAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadWallhavenAsync(Action action) {
            WallhavenApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(WallhavenIni.GetId(), ini.Wallhaven.Order, ini.Wallhaven.Cate);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    WallhavenApi api = JsonConvert.DeserializeObject<WallhavenApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadWallhaven() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadWallhaven() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/wallhaven/v2?client=timelinewallpaper&&order={0}cate={1}";
                string urlApi = string.Format(URL_API, ini.Wallhaven.Order, ini.Wallhaven.Cate);
                LogUtil.I("LoadWallhaven() api url: " + urlApi);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(urlApi);
                WallhavenApi api = JsonConvert.DeserializeObject<WallhavenApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(WallhavenIni.GetId(), ini.Wallhaven.Order, ini.Wallhaven.Cate, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadWallhaven() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadWallhaven() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadWallhereAsync(Action action) {
            WallhereApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(WallhereIni.GetId(), ini.Wallhere.Order, ini.Wallhere.Cate);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    WallhereApi api = JsonConvert.DeserializeObject<WallhereApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadWallhereAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadWallhereAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/wallhere/v2?client=timelinewallpaper&order={0}&cate={1}";
                string urlApi = string.Format(URL_API, ini.Wallhere.Order, ini.Wallhere.Cate);
                LogUtil.I("LoadWallhereAsync() api url: " + urlApi);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(urlApi);
                WallhereApi api = JsonConvert.DeserializeObject<WallhereApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(WallhereIni.GetId(), ini.Wallhere.Order, ini.Wallhere.Cate, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadWallhereAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadWallhereAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadZzzmhAsync(Action action) {
            ZzzmhApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(ZzzmhIni.GetId(), ini.Zzzmh.Order, ini.Zzzmh.Cate);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    ZzzmhApi api = JsonConvert.DeserializeObject<ZzzmhApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadZzzmhAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadZzzmhAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/zzzmh/v2?client=timelinewallpaper&order={0}&cate={1}";
                string urlApi = string.Format(URL_API, ini.Zzzmh.Order, ini.Zzzmh.Cate);
                LogUtil.I("LoadZzzmhAsync() api url: " + urlApi);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(urlApi);
                ZzzmhApi api = JsonConvert.DeserializeObject<ZzzmhApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(ZzzmhIni.GetId(), ini.Zzzmh.Order, ini.Zzzmh.Cate, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadZzzmhAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadZzzmhAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadToopicAsync(Action action) {
            ToopicApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(ToopicIni.GetId(), ini.Toopic.Order, ini.Toopic.Cate);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    ToopicApi api = JsonConvert.DeserializeObject<ToopicApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadToopicAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadToopicAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/toopic/v2?client=timelinewallpaper&order={0}&cate={1}";
                string urlApi = string.Format(URL_API, ini.Toopic.Order, ini.Toopic.Cate);
                LogUtil.I("LoadToopicAsync() api url: " + urlApi);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(urlApi);
                ToopicApi api = JsonConvert.DeserializeObject<ToopicApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(ToopicIni.GetId(), ini.Toopic.Order, ini.Toopic.Cate, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadToopicAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadToopicAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadNetbianAsync(Action action) {
            NetbianApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(NetbianIni.GetId(), ini.Netbian.Order, ini.Netbian.Cate);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    NetbianApi api = JsonConvert.DeserializeObject<NetbianApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadNetbianAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadNetbianAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/netbian/v2?client=timelinewallpaper&order={0}&cate={1}";
                string urlApi = string.Format(URL_API, ini.Netbian.Order, ini.Netbian.Cate);
                LogUtil.I("LoadNetbianAsync() api url: " + urlApi);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(urlApi);
                NetbianApi api = JsonConvert.DeserializeObject<NetbianApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(NetbianIni.GetId(), ini.Netbian.Order, ini.Netbian.Cate, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadNetbianAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadNetbianAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadBackieeAsync(Action action) {
            BackieeApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(BackieeIni.GetId(), ini.Backiee.Order, ini.Backiee.Cate);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    BackieeApi api = JsonConvert.DeserializeObject<BackieeApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadBackieeAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadBackieeAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/backiee/v2?client=timelinewallpaper&order={0}&cate={1}";
                string urlApi = string.Format(URL_API, ini.Backiee.Order, ini.Backiee.Cate);
                LogUtil.I("LoadBackieeAsync() api url: " + urlApi);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(urlApi);
                BackieeApi api = JsonConvert.DeserializeObject<BackieeApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(BackieeIni.GetId(), ini.Backiee.Order, ini.Backiee.Cate, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadBackieeAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadBackieeAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadSkitterAsync(Action action) {
            SkitterApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(SkitterIni.GetId(), ini.Skitter.Order, ini.Skitter.Cate);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    SkitterApi api = JsonConvert.DeserializeObject<SkitterApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadSkitterAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadSkitterAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/skitter/v2?client=timelinewallpaper&order={0}&cate={1}";
                string urlApi = string.Format(URL_API, ini.Skitter.Order, ini.Skitter.Cate);
                LogUtil.I("LoadSkitterAsync() api url: " + urlApi);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(urlApi);
                SkitterApi api = JsonConvert.DeserializeObject<SkitterApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(SkitterIni.GetId(), ini.Skitter.Order, ini.Skitter.Cate, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadSkitterAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadSkitterAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadInfinityAsync(Action action) {
            const string URL_API = "https://api.infinitynewtab.com/v2/get_wallpaper_list?client=pc&order=like&page=0";
            const string URL_API_RANDOM = "https://infinity-api.infinitynewtab.com/random-wallpaper?_={0}";
            string urlApi = "score".Equals(ini.Infinity.Order) ? String.Format(URL_API, 0) : string.Format(URL_API_RANDOM,
                (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
            LogUtil.I("LoadInfinityAsync() api url: " + urlApi);
            HttpClient client = new HttpClient();
            string jsonData = await client.GetStringAsync(urlApi);
            if (action == Action.Toast || action == Action.Tile) {
                string urlThumb;
                Match match = Regex.Match(jsonData, @"""smallSrc"": ?""(.+?)""");
                if (match.Success) {
                    urlThumb = match.Groups[1].Value;
                } else { // for URL_API
                    match = Regex.Match(jsonData, @"""rawSrc"": ?""(.+?)""");
                    urlThumb = match.Groups[1].Value + "?imageMogr2/auto-orient/thumbnail/600x/blur/1x0/quality/75|imageslim";
                }
                LogUtil.I("LoadInfinityAsync() thumb url: " + urlThumb);
                if (action == Action.Toast) {
                    return ShowToast(urlThumb);
                } else {
                    return SetTileBg(urlThumb);
                }
            } else {
                Match match = Regex.Match(jsonData, @"""rawSrc"": ?""(.+?)""");
                string urlUhd = match.Groups[1].Value;
                LogUtil.I("LoadInfinityAsync() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadGluttonAsync(Action action) {
            const int PHASE_SIZE = 10; // 每期图片数
            GluttonApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(GluttonIni.GetId(), ini.Glutton.Album, ini.Glutton.Order);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    GluttonApi api = JsonConvert.DeserializeObject<GluttonApi>(jsonData);
                    if ("journal".Equals(ini.Glutton.Album)) {
                        int count = Math.Min(api.Data.Count, "date".Equals(ini.Glutton.Order) ? PHASE_SIZE : api.Data.Count);
                        data = api.Data[new Random().Next(count)];
                    } else { // rank or null
                        data = api.Data[new Random().Next(api.Data.Count)];
                    }
                    LogUtil.I("LoadGluttonAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadGluttonAsync() " + e.Message);
                }
            }
            if (data == null) {
                if ("journal".Equals(ini.Glutton.Album)) {
                    const string URL_API_JOURNAL = "https://api.nguaduot.cn/glutton/journal?client=timelinewallpaper&order={0}";
                    string urlApi = string.Format(URL_API_JOURNAL, ini.Glutton.Order);
                    LogUtil.I("LoadGluttonAsync() api url: " + urlApi);
                    HttpClient client = new HttpClient();
                    jsonData = await client.GetStringAsync(urlApi);
                    GluttonApi api = JsonConvert.DeserializeObject<GluttonApi>(jsonData);
                    // 若为“收录”排序，则仅推送当期
                    int count = Math.Min(api.Data.Count, "date".Equals(ini.Glutton.Order) ? PHASE_SIZE : api.Data.Count);
                    data = api.Data[new Random().Next(count)];
                } else { // rank or null
                    const string URL_API_RANK = "https://api.nguaduot.cn/glutton/rank?client=timelinewallpaper";
                    string urlApi = string.Format(URL_API_RANK);
                    LogUtil.I("LoadGluttonAsync() api url: " + urlApi);
                    HttpClient client = new HttpClient();
                    jsonData = await client.GetStringAsync(urlApi);
                    GluttonApi api = JsonConvert.DeserializeObject<GluttonApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                }
                await FileUtil.WriteProviderCache(GluttonIni.GetId(), ini.Glutton.Album, ini.Glutton.Order, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                // 生成缩略图
                // TODO：无法保持原图比例
                StorageFolder folderWp = await ApplicationData.Current.LocalFolder.CreateFolderAsync("wallpaper",
                    CreationCollisionOption.OpenIfExists);
                StorageFile fileThumb = await folderWp.CreateFileAsync(string.Format("tile-{0}.jpg", DateTime.Now.ToString("yyyyMMddHH00")),
                    CreationCollisionOption.ReplaceExisting);
                StorageFile fileSrc = await DownloadImgAsync(data.ImgUrl, action);
                StorageItemThumbnail thumb = await fileSrc.GetThumbnailAsync(ThumbnailMode.PicturesView);
                Windows.Storage.Streams.Buffer buffer = new Windows.Storage.Streams.Buffer(Convert.ToUInt32(thumb.Size));
                using (var stream = await fileThumb.OpenAsync(FileAccessMode.ReadWrite)) {
                    await stream.WriteAsync(await thumb.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.None));
                }
                LogUtil.I("LoadGluttonAsync() thumb url: " + fileThumb.Path);
                if (action == Action.Toast) {
                    return ShowToast(fileThumb.Path);
                } else {
                    return SetTileBg(fileThumb.Path);
                }
            } else {
                LogUtil.I("LoadGluttonAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadLspAsync(Action action) {
            LspApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(LspIni.GetId(), ini.Lsp.Order, ini.Lsp.Cate);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    LspApi api = JsonConvert.DeserializeObject<LspApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadLspAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadLspAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/lsp/v2?client=timelinewallpaper&order={0}&cate={1}";
                string urlApi = string.Format(URL_API, ini.Lsp.Order, ini.Lsp.Cate);
                LogUtil.I("LoadLspAsync() api url: " + urlApi);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(urlApi);
                LspApi api = JsonConvert.DeserializeObject<LspApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(LspIni.GetId(), ini.Lsp.Order, ini.Lsp.Cate, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadLspAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadLspAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
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
            LogUtil.I("LoadOneplusAsync() api url: " + URL_API + " " + requestStr);
            HttpClient client = new HttpClient();
            HttpContent content = new StringContent(requestStr);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await client.PostAsync(URL_API, content);
            _ = response.EnsureSuccessStatusCode();
            string jsonData = await response.Content.ReadAsStringAsync();
            OneplusApi oneplusApi = JsonConvert.DeserializeObject<OneplusApi>(jsonData);
            if (action == Action.Toast || action == Action.Tile) {
                string urlThumb = oneplusApi.Items[0].PhotoUrl.Replace(".jpg", "_400_0.jpg");
                LogUtil.I("LoadOneplusAsync() thumb url: " + urlThumb);
                if (action == Action.Toast) {
                    return ShowToast(urlThumb);
                } else {
                    return SetTileBg(urlThumb);
                }
            } else {
                string urlUhd = oneplusApi.Items[0].PhotoUrl;
                LogUtil.I("LoadOneplusAsync() img url: " + urlUhd);
                StorageFile fileImg = await DownloadImgAsync(urlUhd, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadWallpaperupAsync(Action action) {
            WallpaperupApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(WallpaperupIni.GetId(), ini.Wallpaperup.Order, ini.Wallpaperup.Cate);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    WallpaperupApi api = JsonConvert.DeserializeObject<WallpaperupApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadWallpaperupAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadWallpaperupAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/wallpaperup/v2?client=timelinewallpaper&order={0}&cate={1}";
                string urlApi = string.Format(URL_API, ini.Wallpaperup.Order, ini.Wallpaperup.Cate);
                LogUtil.I("LoadWallpaperupAsync() api url: " + urlApi);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(urlApi);
                WallpaperupApi api = JsonConvert.DeserializeObject<WallpaperupApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(WallpaperupIni.GetId(), ini.Wallpaperup.Order, ini.Wallpaperup.Cate, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadWallpaperupAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadWallpaperupAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }

        private async Task<bool> LoadObzhiAsync(Action action) {
            ObzhiApiData data = null;
            string jsonData = await FileUtil.ReadProviderCache(ObzhiIni.GetId(), ini.Obzhi.Order, ini.Obzhi.Cate);
            if (!string.IsNullOrEmpty(jsonData)) {
                try {
                    ObzhiApi api = JsonConvert.DeserializeObject<ObzhiApi>(jsonData);
                    data = api.Data[new Random().Next(api.Data.Count)];
                    LogUtil.I("LoadObzhiAsync() cache from disk");
                } catch (Exception e) {
                    LogUtil.E("LoadObzhiAsync() " + e.Message);
                }
            }
            if (data == null) {
                const string URL_API = "https://api.nguaduot.cn/obzhi/v2?client=timelinewallpaper&order={0}&cate={1}";
                string urlApi = string.Format(URL_API, ini.Obzhi.Order, ini.Obzhi.Cate);
                LogUtil.I("LoadObzhiAsync() api url: " + urlApi);
                HttpClient client = new HttpClient();
                jsonData = await client.GetStringAsync(urlApi);
                ObzhiApi api = JsonConvert.DeserializeObject<ObzhiApi>(jsonData);
                data = api.Data[new Random().Next(api.Data.Count)];
                await FileUtil.WriteProviderCache(ObzhiIni.GetId(), ini.Obzhi.Order, ini.Obzhi.Cate, jsonData);
            }
            if (action == Action.Toast || action == Action.Tile) {
                LogUtil.I("LoadObzhiAsync() thumb url: " + data.ThumbUrl);
                if (action == Action.Toast) {
                    return ShowToast(data.ThumbUrl);
                } else {
                    return SetTileBg(data.ThumbUrl);
                }
            } else {
                LogUtil.I("LoadObzhiAsync() img url: " + data.ImgUrl);
                StorageFile fileImg = await DownloadImgAsync(data.ImgUrl, action);
                if (action == Action.Lock) {
                    return await SetLockBgAsync(fileImg);
                } else {
                    return await SetDesktopBgAsync(fileImg);
                }
            }
        }
    }
}
