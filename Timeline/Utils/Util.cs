using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Display;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Timeline.Utils {
    public class IniUtil {
        // Assets 文本文件只支持 .txt 格式；以下两个配置文件结构不同，内容需保持一致
        private const string FILE_INI_DEF = "Assets\\Config\\config.txt";
        private const string FILE_JSON_DEF = "Assets\\Config\\json.txt";

        //[DllImport("kernel32")]
        //private static extern int GetPrivateProfileString(string section, string key, string defValue, StringBuilder retVal, int size, string filePath);

        //[DllImport("kernel32")]
        //private static extern bool WritePrivateProfileString(string section, string key, string value, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(byte[] section, byte[] key, byte[] defValue, byte[] retVal, int size, string filePath);

        [DllImport("kernel32")]
        private static extern bool WritePrivateProfileString(byte[] section, byte[] key, byte[] value, string filePath);

        private static string GetPrivateProfileString(string section, string key, string defValue, string filePath) {
            // GetPrivateProfileString 默认使用GB2313编码，封装使支持UTF-8
            byte[] buffer = new byte[1024];
            int count = GetPrivateProfileString(
                Encoding.GetEncoding("utf-8").GetBytes(section),
                Encoding.GetEncoding("utf-8").GetBytes(key),
                Encoding.GetEncoding("utf-8").GetBytes(defValue),
                buffer, 1024, filePath);
            return Encoding.GetEncoding("utf-8").GetString(buffer, 0, count);
        }

        private static int GetPrivateProfileInt(string section, string key, int defValue, string filePath) {
            string res = GetPrivateProfileString(section, key, defValue.ToString(), filePath);
            if (int.TryParse(res, out int val)) {
                return val;
            }
            return defValue;
        }

        private static float GetPrivateProfileFloat(string section, string key, float defValue, string filePath) {
            string res = GetPrivateProfileString(section, key, defValue.ToString(), filePath);
            if (float.TryParse(res, out float val)) {
                return val;
            }
            return defValue;
        }

        private static bool WritePrivateProfileString(string section, string key, string value, string filePath) {
            // WritePrivateProfileString 默认使用GB2313编码，封装使支持UTF-8
            return WritePrivateProfileString(
                Encoding.GetEncoding("utf-8").GetBytes(section),
                Encoding.GetEncoding("utf-8").GetBytes(key),
                Encoding.GetEncoding("utf-8").GetBytes(value),
                filePath);
        }

        private static async Task<StorageFile> GenerateIniFileAsync() {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            string iniName = FileUtil.GetIniName();
            StorageFile iniFile = await folder.TryGetItemAsync(iniName) as StorageFile;
            if (iniFile == null) { // 生成初始配置文件
                FileInfo[] oldFiles = new DirectoryInfo(folder.Path).GetFiles("*.ini", SearchOption.TopDirectoryOnly);
                Array.Sort(oldFiles, (a, b) => (b as FileInfo).CreationTime.CompareTo((a as FileInfo).CreationTime));
                StorageFile configFile = await Package.Current.InstalledLocation.GetFileAsync(FILE_INI_DEF);
                iniFile = await configFile.CopyAsync(folder, iniName, NameCollisionOption.ReplaceExisting);
                LogUtil.D("GenerateIniFileAsync() copied ini: " + iniFile.Path);
                if (oldFiles.Length > 0) { // 继承设置
                    LogUtil.D("GenerateIniFileAsync() inherit: " + oldFiles[0].Name);
                    await InheritIni(oldFiles[0].FullName, iniFile.Path);
                }
            }
            return iniFile;
        }

        private static string GetIniFile() {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            string iniFile = Path.Combine(folder.Path, FileUtil.GetIniName());
            return File.Exists(iniFile) ? iniFile : null;
        }

        private static async Task InheritIni(string fileOld, string fileNew) {
            StorageFile configFile = await Package.Current.InstalledLocation.GetFileAsync(FILE_JSON_DEF);
            JObject data = JObject.Parse(await FileIO.ReadTextAsync(configFile));
            JObject dataApp = data.Value<JObject>("app");
            JObject dataProvider = data.Value<JObject>("provider");
            StringBuilder sb = new StringBuilder(1024);
            // 全局配置
            foreach (var item in dataApp) {
                string value = item.Value != null
                    ? (item.Value.Type is JTokenType.Boolean ? ((bool)item.Value ? "1" : "0") : (string)item.Value)
                    : "";
                value = GetPrivateProfileString("app", item.Key, value, fileOld);
                _ = WritePrivateProfileString("app", item.Key, value, fileNew);
            }
            // 图源配置
            foreach (var item1 in dataProvider) {
                foreach (var item2 in dataProvider.Value<JObject>(item1.Key)) {
                    string value = item2.Value != null
                        ? (item2.Value.Type is JTokenType.Boolean ? ((bool)item2.Value ? "1" : "0") : (string)item2.Value)
                        : "";
                    value = GetPrivateProfileString(item1.Key, item2.Key, value, fileOld);
                    _ = WritePrivateProfileString(item1.Key, item2.Key, value, fileNew);
                }
            }
        }

        public static async Task SaveProviderAsync(string provider) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("app", "provider", provider, iniFile.Path);
        }

        public static async Task SaveDesktopProviderAsync(string provider) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("app", "desktopprovider", provider, iniFile.Path);
        }

        public static async Task SaveLockProviderAsync(string provider) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("app", "lockprovider", provider, iniFile.Path);
        }

        public static async Task SaveThemeAsync(string theme) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("app", "theme", theme, iniFile.Path);
        }

        public static async Task SaveLocalFolderAsync(string folder) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(LocalIni.ID, "folder", folder, iniFile.Path);
        }

        public static async Task SaveBingLangAsync(string langCode) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(BingIni.ID, "lang", langCode, iniFile.Path);
        }

        public static async Task SaveNasaOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(NasaIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveNasaMirrorAsync(string mirror) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(NasaIni.ID, "mirror", mirror, iniFile.Path);
        }

        public static async Task SaveOneplusOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(OneplusIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveTimelineOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(TimelineIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveTimelineCateAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(TimelineIni.ID, "cate", order, iniFile.Path);
        }

        public static async Task SaveHimawari8OffsetAsync(float offset) {
            offset = offset < 0.01f ? 0.01f : (offset > 1 ? 1 : offset);
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(Himawari8Ini.ID, "offset", offset.ToString("0.00"), iniFile.Path);
        }

        public static async Task SaveHimawari8RatioAsync(float ratio) {
            ratio = ratio < 0.1f ? 0.1f : (ratio > 1 ? 1 : ratio);
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(Himawari8Ini.ID, "ratio", ratio.ToString("0.00"), iniFile.Path);
        }

        public static async Task SaveYmyouliOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(YmyouliIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveYmyouliCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(YmyouliIni.ID, "cate", cate, iniFile.Path);
        }

        public static async Task SaveInfinityOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(InfinityIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveGluttonAlbumAsync(string album) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(GluttonIni.ID, "album", album, iniFile.Path);
        }

        public static async Task SaveGluttonOrderAsync(string album) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(GluttonIni.ID, "order", album, iniFile.Path);
        }

        public static async Task SaveOneOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(OneIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveQingbzOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(QingbzIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveQingbzCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(QingbzIni.ID, "cate", cate, iniFile.Path);
        }

        public static async Task SaveWallhavenOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(WallhavenIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveWallhavenCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(WallhavenIni.ID, "cate", cate, iniFile.Path);
        }

        public static async Task SaveWallhereOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(WallhereIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveWallhereCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(WallhereIni.ID, "cate", cate, iniFile.Path);
        }

        public static async Task SaveWallpaperupOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(WallpaperupIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveWallpaperupCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(WallpaperupIni.ID, "cate", cate, iniFile.Path);
        }

        public static async Task SaveToopicOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(ToopicIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveToopicCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(ToopicIni.ID, "cate", cate, iniFile.Path);
        }

        public static async Task SaveLspOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(LspIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveLspCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(LspIni.ID, "cate", cate, iniFile.Path);
        }

        public static async Task<StorageFile> GetIniPath() {
            return await GenerateIniFileAsync();
        }

        public static Ini GetIni() {
            string iniFile = GetIniFile();
            Ini ini = new Ini();
            if (iniFile == null) { // 尚未初始化
                return ini;
            }
            ini.Provider = GetPrivateProfileString("app", "provider", BingIni.ID, iniFile);
            ini.DesktopProvider = GetPrivateProfileString("app", "desktopprovider", "", iniFile);
            ini.LockProvider = GetPrivateProfileString("app", "lockprovider", "", iniFile);
            ini.ToastProvider = GetPrivateProfileString("app", "toastprovider", "", iniFile);
            ini.TileProvider = GetPrivateProfileString("app", "tileprovider", "", iniFile);
            ini.Theme = GetPrivateProfileString("app", "theme", "", iniFile);
            ini.Cache = GetPrivateProfileInt("app", "cache", 600, iniFile);
            ini.R18 = GetPrivateProfileInt("app", "r18", 0, iniFile);

            ini.SetIni(LocalIni.ID, new LocalIni {
                DesktopPeriod = GetPrivateProfileFloat(LocalIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(LocalIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(LocalIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(LocalIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(LocalIni.ID, "order", "random", iniFile),
                Folder = GetPrivateProfileString(LocalIni.ID, "folder", "", iniFile),
                Depth = GetPrivateProfileInt(LocalIni.ID, "depth", 0, iniFile),
                Appetite = GetPrivateProfileInt(LocalIni.ID, "appetite", 20, iniFile)
            });
            ini.SetIni(BingIni.ID, new BingIni {
                DesktopPeriod = GetPrivateProfileFloat(BingIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(BingIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(BingIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(BingIni.ID, "tileperiod", 2, iniFile),
                Lang = GetPrivateProfileString(BingIni.ID, "lang", "", iniFile)
            });
            ini.SetIni(NasaIni.ID, new NasaIni {
                DesktopPeriod = GetPrivateProfileFloat(NasaIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(NasaIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(NasaIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(NasaIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(NasaIni.ID, "order", "date", iniFile),
                Mirror = GetPrivateProfileString(NasaIni.ID, "mirror", "bjp", iniFile)
            });
            ini.SetIni(TimelineIni.ID, new TimelineIni {
                DesktopPeriod = GetPrivateProfileFloat(TimelineIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(TimelineIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(TimelineIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(TimelineIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(TimelineIni.ID, "order", "date", iniFile),
                Cate = GetPrivateProfileString(TimelineIni.ID, "cate", "", iniFile),
                Unauthorized = GetPrivateProfileInt(TimelineIni.ID, "unauthorized", 0, iniFile)
            });
            ini.SetIni(OneIni.ID, new OneIni {
                DesktopPeriod = GetPrivateProfileFloat(OneIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(OneIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(OneIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(OneIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(OneIni.ID, "order", "date", iniFile)
            });
            ini.SetIni(Himawari8Ini.ID, new Himawari8Ini {
                DesktopPeriod = GetPrivateProfileFloat(Himawari8Ini.ID, "desktopperiod", 1, iniFile),
                LockPeriod = GetPrivateProfileFloat(Himawari8Ini.ID, "lockperiod", 2, iniFile),
                ToastPeriod = GetPrivateProfileFloat(Himawari8Ini.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(Himawari8Ini.ID, "tileperiod", 2, iniFile),
                Offset = GetPrivateProfileFloat(Himawari8Ini.ID, "offset", 0.5f, iniFile),
                Ratio = GetPrivateProfileFloat(Himawari8Ini.ID, "ratio", 0.5f, iniFile)
            });
            ini.SetIni(YmyouliIni.ID, new YmyouliIni {
                DesktopPeriod = GetPrivateProfileFloat(YmyouliIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(YmyouliIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(YmyouliIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(YmyouliIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(YmyouliIni.ID, "order", "random", iniFile),
                Cate = GetPrivateProfileString(TimelineIni.ID, "cate", "", iniFile)
            });
            ini.SetIni(QingbzIni.ID, new QingbzIni {
                DesktopPeriod = GetPrivateProfileFloat(QingbzIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(QingbzIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(QingbzIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(QingbzIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(QingbzIni.ID, "order", "random", iniFile),
                Cate = GetPrivateProfileString(QingbzIni.ID, "cate", "", iniFile)
            });
            ini.SetIni(WallhavenIni.ID, new WallhavenIni {
                DesktopPeriod = GetPrivateProfileFloat(WallhavenIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(WallhavenIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(WallhavenIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(WallhavenIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(WallhavenIni.ID, "order", "random", iniFile),
                Cate = GetPrivateProfileString(WallhavenIni.ID, "cate", "", iniFile)
            });
            ini.SetIni(WallhereIni.ID, new WallhereIni {
                DesktopPeriod = GetPrivateProfileFloat(WallhereIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(WallhereIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(WallhereIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(WallhereIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(WallhereIni.ID, "order", "random", iniFile),
                Cate = GetPrivateProfileString(WallhereIni.ID, "cate", "", iniFile)
            });
            ini.SetIni(WallpaperupIni.ID, new WallpaperupIni {
                DesktopPeriod = GetPrivateProfileFloat(WallpaperupIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(WallpaperupIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(WallpaperupIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(WallpaperupIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(WallpaperupIni.ID, "order", "random", iniFile),
                Cate = GetPrivateProfileString(WallpaperupIni.ID, "cate", "", iniFile)
            });
            ini.SetIni(ToopicIni.ID, new ToopicIni {
                DesktopPeriod = GetPrivateProfileFloat(ToopicIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(ToopicIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(ToopicIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(ToopicIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(ToopicIni.ID, "order", "random", iniFile),
                Cate = GetPrivateProfileString(ToopicIni.ID, "cate", "", iniFile)
            });
            ini.SetIni(InfinityIni.ID, new InfinityIni {
                DesktopPeriod = GetPrivateProfileFloat(InfinityIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(InfinityIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(InfinityIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(InfinityIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(InfinityIni.ID, "order", "random", iniFile)
            });
            ini.SetIni(GluttonIni.ID, new GluttonIni {
                DesktopPeriod = GetPrivateProfileFloat(GluttonIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(GluttonIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(GluttonIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(GluttonIni.ID, "tileperiod", 2, iniFile),
                Album = GetPrivateProfileString(GluttonIni.ID, "album", "journal", iniFile),
                Order = GetPrivateProfileString(GluttonIni.ID, "order", "date", iniFile)
            });
            ini.SetIni(LspIni.ID, new LspIni {
                DesktopPeriod = GetPrivateProfileFloat(LspIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(LspIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(LspIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(LspIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(LspIni.ID, "order", "random", iniFile),
                Cate = GetPrivateProfileString(LspIni.ID, "cate", "", iniFile)
            });
            ini.SetIni(OneplusIni.ID, new OneplusIni {
                DesktopPeriod = GetPrivateProfileFloat(OneplusIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(OneplusIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(OneplusIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(OneplusIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(OneplusIni.ID, "order", "date", iniFile)
            });
            ini.SetIni(ObzhiIni.ID, new ObzhiIni {
                DesktopPeriod = GetPrivateProfileFloat(ObzhiIni.ID, "desktopperiod", 24, iniFile),
                LockPeriod = GetPrivateProfileFloat(ObzhiIni.ID, "lockperiod", 24, iniFile),
                ToastPeriod = GetPrivateProfileFloat(ObzhiIni.ID, "toastperiod", 24, iniFile),
                TilePeriod = GetPrivateProfileFloat(ObzhiIni.ID, "tileperiod", 2, iniFile),
                Order = GetPrivateProfileString(ObzhiIni.ID, "order", "random", iniFile),
                Cate = GetPrivateProfileString(ObzhiIni.ID, "cate", "", iniFile)
            });
            return ini;
        }

        public static async Task<Ini> GetIniAsync() {
            LogUtil.D("GetIniAsync() " + FileUtil.GetIniName());
            _ = await GenerateIniFileAsync();
            return GetIni();
        }
    }

    public class DateUtil {
        public static DateTime FromUnixMillis(long unixMillis) {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddMilliseconds(unixMillis);
        }

        public static long ToUnixMillis(DateTime date) {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return (long)diff.TotalMilliseconds;
        }

        public static long CurrentTimeMillis() {
            return ToUnixMillis(DateTime.Now);
        }

        public static int GetDaysOfMonth(DateTime date) {
            int month = date.Month;
            if (month == 1 || month == 3 || month == 5 || month == 7
                || month == 8 || month == 10 || month == 12) {
                return 31;
            } else if (month == 2) {
                return (date.Year % 4 == 0 && date.Year % 100 != 0)
                    || date.Year % 400 == 0 ? 29 : 28;
            }
            return 30;
        }
    }

    public class FileUtil {
        public static string MakeValidFileName(string text, string replacement = "_") {
            StringBuilder str = new StringBuilder();
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            foreach (char c in text) {
                if (invalidFileNameChars.Contains(c)) {
                    _ = str.Append(replacement ?? "");
                } else {
                    _ = str.Append(c);
                }
            }

            return str.ToString();
        }

        public static string ConvertFileSize(long size) {
            if (size < 1024) {
                return size + "B";
            }
            if (size < 1024 * 1024) {
                return (size / 1024.0).ToString("0KB");
            }
            if (size < 1024 * 1024 * 1024) {
                return (size / 1024.0 / 1024.0).ToString("0.0MB");
            }
            return (size / 1024.0 / 1024.0 / 1024.0).ToString("0.00GB");
        }

        //public static async Task<IList<string>> GetGlitterAsync() {
        //    try {
        //        StorageFile file = await Package.Current.InstalledLocation.GetFileAsync("Assets\\Config\\glitter.txt");
        //        if (file != null) {
        //            return await FileIO.ReadLinesAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);
        //        }
        //    } catch (Exception e) {
        //        LogUtil.E("GetGlitterAsync() " + e.Message);
        //    }
        //    return new List<string>();
        //}

        public static async Task<Dictionary<string, int>> GetHistoryAsync(string provider) {
            try {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("count",
                    CreationCollisionOption.OpenIfExists);
                StorageFile file = await folder.CreateFileAsync(provider + ".json", CreationCollisionOption.OpenIfExists);
                string content = await FileIO.ReadTextAsync(file);
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(content) ?? new Dictionary<string, int>();
            } catch (Exception e) {
                Debug.WriteLine(e);
                LogUtil.E("GetHistoryAsync() " + e.Message);
            }
            return new Dictionary<string, int>();
        }

        public static async Task SaveHistoryAsync(string provider, Dictionary<string, int> dic) {
            try {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("count",
                    CreationCollisionOption.OpenIfExists);
                StorageFile file = await folder.CreateFileAsync(provider + ".json", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(dic));
            } catch (Exception e) {
                Debug.WriteLine(e);
                LogUtil.E("SaveHistoryAsync() " + e.Message);
            }
        }

        public static async Task<Dictionary<string, int>> ReadDosage() {
            // 读取所有图源24h图片用量
            Dictionary<string, int> res = new Dictionary<string, int>();
            try {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("count",
                    CreationCollisionOption.OpenIfExists);
                StorageFile file = await folder.CreateFileAsync("dosage.json", CreationCollisionOption.OpenIfExists);
                string content = await FileIO.ReadTextAsync(file);
                Dictionary<string, string> dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(content) ?? new Dictionary<string, string>();
                foreach (string k in dic.Keys) {
                    res[k] = string.IsNullOrEmpty(dic[k]) ? 0 : dic[k].Split(",").Length;
                }
            } catch (Exception e) {
                Debug.WriteLine(e);
                LogUtil.E("ReadDosage() " + e.Message);
            }
            return res;
        }

        public static async Task WriteDosage(string provider = null) {
            // 刷新所有图源近24h图片用量
            try {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("count",
                    CreationCollisionOption.OpenIfExists);
                StorageFile file = await folder.CreateFileAsync("dosage.json", CreationCollisionOption.OpenIfExists);
                string content = await FileIO.ReadTextAsync(file);
                Dictionary<string, string> dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(content) ?? new Dictionary<string, string>();
                string[] sec_old = dic.ContainsKey("all") ? dic["all"].Split(",") : new string[0];
                List<long> sec_new = new List<long>();
                long sec_now = DateTime.Now.Ticks / 10000 / 1000;
                foreach (string item in sec_old) {
                    long sec = long.Parse(item);
                    if (sec <= sec_now && sec_now - sec <= 24 * 60 * 60) {
                        sec_new.Add(sec);
                    }
                }
                sec_new.Add(sec_now);
                dic["all"] = string.Join(",", sec_new.ToArray());
                if (!string.IsNullOrEmpty(provider)) {
                    sec_old = dic.ContainsKey(provider) ? dic[provider].Split(",") : new string[0];
                    sec_new = new List<long>();
                    foreach (string item in sec_old) {
                        long sec = long.Parse(item);
                        if (sec <= sec_now && sec_now - sec <= 24 * 60 * 60) {
                            sec_new.Add(sec);
                        }
                    }
                    sec_new.Add(sec_now);
                    dic[provider] = string.Join(",", sec_new.ToArray());
                }
                await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(dic));
            } catch (Exception e) {
                Debug.WriteLine(e);
                LogUtil.E("WriteDosage() " + e.Message);
            }
        }

        public static async Task<StorageFolder> GetLogFolderAsync() {
            // LocalCache folder can be relied upon until it's deleted,
            // whereas TempState folder cannot be relied upon at a later time
            // as it's subject to deletion by external factors such as disk clean - up,
            // or by the operating system on running low on storage space.
            return await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync("logs", CreationCollisionOption.OpenIfExists);
        }

        public static async Task<StorageFolder> GetWallpaperFolderAsync() {
            // Your app can't set wallpapers from any folder.
            // Copy file in ApplicationData.Current.LocalFolder and set wallpaper from there.
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync("wallpaper", CreationCollisionOption.OpenIfExists);
        }

        public static async Task LaunchFileAsync(StorageFile file) {
            try {
                await Launcher.LaunchFileAsync(file);
            } catch (Exception e) {
                LogUtil.E("LaunchFileAsync() " + e.Message);
            }
        }

        public static async Task LaunchFolderAsync(StorageFolder folder, StorageFile fileSelected = null) {
            try {
                if (fileSelected != null) {
                    FolderLauncherOptions options = new FolderLauncherOptions();
                    options.ItemsToSelect.Add(fileSelected); // 打开文件夹同时选中目标文件
                    await Launcher.LaunchFolderAsync(folder, options);
                } else {
                    await Launcher.LaunchFolderAsync(folder);
                }
            } catch (Exception e) {
                LogUtil.E("LaunchFolderAsync() " + e.Message);
            }
        }

        public static async Task LaunchUriAsync(Uri uri) {
            try {
                await Launcher.LaunchUriAsync(uri);
            } catch (Exception e) {
                LogUtil.E("LaunchUriAsync() " + e.Message);
            }
        }

        public static async Task ClearCache(Ini ini) {
            int count_threshold = ini?.Cache ?? 600; // 缓存量阈值
            try {
                if (count_threshold <= 0) {
                    await ApplicationData.Current.ClearAsync(ApplicationDataLocality.Temporary);
                    LogUtil.I("ClearCache() all");
                } else {
                    // 清理浏览缓存文件夹
                    StorageFolder folder = ApplicationData.Current.TemporaryFolder; // 浏览缓存文件夹
                    FileInfo[] files = new DirectoryInfo(folder.Path).GetFiles(); // 缓存图片
                    Array.Sort(files, (a, b) => (b as FileInfo).CreationTime.CompareTo((a as FileInfo).CreationTime)); // 日期降序排列
                    int count_clear = 0;
                    for (int i = count_threshold; i < files.Length; ++i) { // 删除超量图片
                        files[i].Delete();
                        count_clear++;
                    }
                    // 清理壁纸缓存文件夹
                    folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("wallpaper",
                        CreationCollisionOption.OpenIfExists); // 壁纸缓存文件夹
                    files = new DirectoryInfo(folder.Path).GetFiles(); // 缓存图片
                    Array.Sort(files, (a, b) => (b as FileInfo).CreationTime.CompareTo((a as FileInfo).CreationTime)); // 日期降序排列
                    int count_clear2 = 0;
                    for (int i = count_threshold; i < files.Length; ++i) { // 删除超量图片
                        files[i].Delete();
                        count_clear2++;
                    }
                    LogUtil.I("ClearCache() " + count_clear + "+" + count_clear2);
                }
            } catch (Exception e) {
                LogUtil.E("ClearCache() " + e.Message);
            }
        }

        //public static async Task<StorageFolder> GetPicLibFolder(string folderName = null) {
        //    StorageFolder folder = null;
        //    if (!string.IsNullOrEmpty(folderName)) {
        //        try {
        //            folder = await KnownFolders.PicturesLibrary.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
        //        } catch (Exception ex) {
        //            LogUtil.E("GetPicLibFolder() " + ex.Message);
        //        }
        //    }
        //    if (folder == null) {
        //        try {
        //            folder = await KnownFolders.PicturesLibrary.CreateFolderAsync(AppInfo.Current.DisplayInfo.DisplayName, CreationCollisionOption.OpenIfExists);
        //        } catch (Exception ex) {
        //            LogUtil.E("GetPicLibFolder() " + ex.Message);
        //        }
        //    }
        //    return folder;
        //}

        public static async Task<StorageFolder> GetGalleryFolder(string pathDir = null) {
            StorageFolder folder = null;
            if (!string.IsNullOrEmpty(pathDir)) {
                try {
                    folder = await StorageFolder.GetFolderFromPathAsync(pathDir.Replace("/", "\\"));
                } catch (FileNotFoundException ex) { // 指定的文件夹不存在
                    LogUtil.E("GetGalleryFolder() " + ex.Message);
                } catch (UnauthorizedAccessException ex) { // 您无权访问指定文件夹
                    LogUtil.E("GetGalleryFolder() " + ex.Message);
                } catch (ArgumentException ex) { // 路径不能是相对路径或 URI
                    LogUtil.E("GetGalleryFolder() " + ex.Message);
                } catch (Exception ex) {
                    LogUtil.E("GetGalleryFolder() " + ex.Message);
                }
            }
            if (folder == null) {
                try {
                    folder = await KnownFolders.PicturesLibrary.CreateFolderAsync(AppInfo.Current.DisplayInfo.DisplayName, CreationCollisionOption.OpenIfExists);
                } catch (Exception ex) {
                    LogUtil.E("GetGalleryFolder() " + ex.Message);
                }
            }
            return folder;
        }

        /// <summary>
        /// 从URL解析文件格式，默认为“.jpg”
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string ParseFormat(string url) {
            if (!string.IsNullOrEmpty(url)) {
                Uri uri = new Uri(url);
                string[] nameArr = uri.Segments[uri.Segments.Length - 1].Split(".");
                if (nameArr.Length > 1) {
                    return "." + nameArr[nameArr.Length - 1];
                }
            }
            return ".jpg";
        }

        public static string GetIniName() {
            return string.Format("timeline-{0}.ini", SysUtil.GetPkgVer(true));
        }

        public static async Task<StorageFile> GetDocShortcutFile() {
            StorageFile configFile = await Package.Current.InstalledLocation.GetFileAsync("Assets\\Config\\shortcut.txt");
            return await configFile.CopyAsync(ApplicationData.Current.LocalFolder, "shortcut.txt",
                NameCollisionOption.ReplaceExisting);
        }

        public static async Task<StorageFile> GetDocGoFile() {
            StorageFile configFile = await Package.Current.InstalledLocation.GetFileAsync("Assets\\Config\\go.txt");
            return await configFile.CopyAsync(ApplicationData.Current.LocalFolder, "go.txt",
                NameCollisionOption.ReplaceExisting);
        }
    }

    public class SysUtil {
        /// <summary>
        /// 获取安装包版本
        /// 短：{major}.{minor}
        /// 长：{major}.{minor}.{build}.{revision}
        /// </summary>
        /// <param name="forShort"></param>
        /// <returns></returns>
        public static string GetPkgVer(bool forShort) {
            if (forShort) {
                return string.Format("{0}.{1}",
                    Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor);
            }
            return string.Format("{0}.{1}.{2}.{3}",
                Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor,
                Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
        }

        public static bool CheckNewVer(string versionNew) {
            string[] versions = versionNew?.Split(".");
            if (versions == null || versions.Length < 2) {
                return false;
            }
            int major = Package.Current.Id.Version.Major;
            int minor = Package.Current.Id.Version.Minor;
            _ = int.TryParse(versions[0], out int majorNew);
            _ = int.TryParse(versions[1], out int minorNew);
            return majorNew > major || (majorNew == major && minorNew > minor);
        }

        public static string GetOsVer() {
            ulong version = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
            ulong major = (version & 0xFFFF000000000000L) >> 48;
            ulong minor = (version & 0x0000FFFF00000000L) >> 32;
            ulong build = (version & 0x00000000FFFF0000L) >> 16;
            ulong revision = (version & 0x000000000000FFFFL);
            return $"{major}.{minor}.{build}.{revision}";
        }

        public static string GetDevice() {
            var deviceInfo = new EasClientDeviceInformation();
            if (deviceInfo.SystemSku.Length > 0) {
                return deviceInfo.SystemSku;
            }
            return string.Format("{0}_{1}", deviceInfo.SystemManufacturer,
                deviceInfo.SystemProductName);
        }

        public static string GetDeviceName() {
            var deviceInfo = new EasClientDeviceInformation();
            return deviceInfo.FriendlyName;
        }

        public static string GetDeviceId() {
            SystemIdentificationInfo systemId = SystemIdentification.GetSystemIdForPublisher();
            // Make sure this device can generate the IDs
            if (systemId.Source != SystemIdentificationSource.None) {
                // The Id property has a buffer with the unique ID
                DataReader dataReader = DataReader.FromBuffer(systemId.Id);
                return dataReader.ReadGuid().ToString();
            }
            return "";
        }

        public static Windows.Foundation.Size GetMonitorPixels(bool logic) {
            try {
                DisplayInformation info = DisplayInformation.GetForCurrentView();
                if (logic) {
                    return new Windows.Foundation.Size(info.ScreenWidthInRawPixels / info.RawPixelsPerViewPixel,
                        info.ScreenHeightInRawPixels / info.RawPixelsPerViewPixel);
                }
                return new Windows.Foundation.Size(info.ScreenWidthInRawPixels, info.ScreenHeightInRawPixels);
            } catch (Exception e) {
                LogUtil.E("GetMonitorPhysicalPixels() " + e.Message);
            }
            return new Windows.Foundation.Size(); // do not use IsEmpty to check empty
        }

        public static double GetMonitorScale() {
            try {
                DisplayInformation info = DisplayInformation.GetForCurrentView();
                return info.RawPixelsPerViewPixel;
            } catch (Exception e) {
                LogUtil.E("GetMonitorPhysicalPixels() " + e.Message);
            }
            return 0;
        }

        public static double GetMonitorDiagonal() {
            try {
                DisplayInformation info = DisplayInformation.GetForCurrentView();
                return info.DiagonalSizeInInches ?? 0;
            } catch (Exception e) {
                LogUtil.E("GetMonitorPhysicalPixels() " + e.Message);
            }
            return 0;
        }
    }

    public static class ThemeUtil {
        public static ElementTheme ParseTheme(string theme) {
            switch (theme) {
                case "light":
                    return ElementTheme.Light;
                case "dark":
                    return ElementTheme.Dark;
                default:
                    //return ElementTheme.Default; // 该值非系统主题值
                    var uiSettings = new Windows.UI.ViewManagement.UISettings();
                    var color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
                    if (color == Windows.UI.Color.FromArgb(0xff, 0xff, 0xff, 0xff)) {
                        return ElementTheme.Light;
                    }
                    return ElementTheme.Dark;
            }
        }
    }

    public static class TextUtil {
        public static bool Copy(string content) {
            if (string.IsNullOrEmpty(content)) {
                return false;
            }
            DataPackage pkg = new DataPackage {
                RequestedOperation = DataPackageOperation.Copy
            };
            pkg.SetText(content);
            Clipboard.SetContent(pkg);
            return true;
        }

        public static bool Copy(StorageFile imgFile) {
            if (imgFile == null || !imgFile.IsAvailable) {
                return false;
            }
            DataPackage pkg = new DataPackage {
                RequestedOperation = DataPackageOperation.Copy
            };
            pkg.SetBitmap(RandomAccessStreamReference.CreateFromFile(imgFile));
            Clipboard.SetContent(pkg);
            return true;
        }
    }

    public static class ImgUtil {
        public static int[] Resize(double boxW, double boxH, double imgW, double imgH, Stretch stretch) {
            int[] res = new int[2];
            if (stretch == Stretch.UniformToFill) { // 填充模式
                if (imgW / imgH > boxW / boxH) { // 图片比窗口宽，缩放至与窗口等高
                    res[1] = (int)Math.Round(boxH);
                    //res[0] = (int)Math.Round(boxH * imgW / imgH);
                    res[0] = 0;
                } else { // 图片比窗口窄，缩放至与窗口等宽
                    res[0] = (int)Math.Round(boxW);
                    //res[1] = (int)Math.Round(boxW * imgH / imgW);
                    res[1] = 0;
                }
            } else if (stretch == Stretch.Uniform) { // 全图模式
                if (imgW / imgH > boxW / boxH) { // 图片比窗口宽，缩放至与窗口等宽
                    res[0] = (int)Math.Round(boxW);
                    //res[1] = (int)Math.Round(boxW * imgH / imgW);
                    res[1] = 0;
                } else { // 图片比窗口窄，缩放至与窗口等高
                    res[1] = (int)Math.Round(boxH);
                    //res[0] = (int)Math.Round(boxH * imgW / imgH);
                    res[0] = 0;
                }
            } else if (stretch == Stretch.Fill) { // 拉伸模式
                res[0] = (int)Math.Round(boxW);
                res[1] = (int)Math.Round(boxH);
            } else { // 原图模式
                res[0] = (int)Math.Round(imgW);
                res[1] = (int)Math.Round(imgH);
            }
            return res;
        }
    }

    public static class LogUtil {
        private static bool isInitialized = false;
        
        public static void I(string message, params object[] values) {
            if (!isInitialized) {
                Initialize();
            }
            Log.Information(message);
        }

        public static void D(string message, params object[] values) {
            if (!isInitialized) {
                Initialize();
            }
            Log.Debug(message, values);
        }

        public static void W(string message, params object[] values) {
            if (!isInitialized) {
                Initialize();
            }
            Log.Warning(message);
        }

        public static void E(string message, params object[] values) {
            if (!isInitialized) {
                Initialize();
            }
            Log.Error(message);
        }

        private static void Initialize() {
            string logFilePath = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "logs/timeline.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            isInitialized = true;
            Log.Debug("Initialized Serilog");
        }
    }
}
