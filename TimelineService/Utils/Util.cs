using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;

namespace TimelineService.Utils {
    public sealed class IniUtil {
        //[DllImport("kernel32")]
        //private static extern int GetPrivateProfileString(string section, string key, string defValue, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(byte[] section, byte[] key, byte[] defValue, byte[] retVal, int size, string filePath);

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

        private static string GetIniFile() {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            string iniFile = Path.Combine(folder.Path, FileUtil.GetIniName());
            if (!File.Exists(iniFile)) { // 可能更新后尚未启动应用，新配置尚未生成，寻找旧配置
                LogUtil.I("IniUtil.GetIniFile() searching old ini");
                FileInfo[] oldFiles = new DirectoryInfo(folder.Path).GetFiles("*.ini", SearchOption.TopDirectoryOnly);
                Array.Sort(oldFiles, (a, b) => (b as FileInfo).CreationTime.CompareTo((a as FileInfo).CreationTime));
                if (oldFiles.Length > 0) { // 继承设置
                    iniFile = oldFiles[0].FullName;
                }
            }
            return File.Exists(iniFile) ? iniFile : null;
        }

        public static Ini GetIni() {
            string iniFile = GetIniFile();
            LogUtil.I("IniUtil.GetIni() " + FileUtil.GetIniName());
            Ini ini = new Ini();
            if (iniFile == null) { // 尚未初始化
                return ini;
            }
            ini.Provider = GetPrivateProfileString("app", "provider", BingIni.GetId(), iniFile);
            ini.DesktopProvider = GetPrivateProfileString("app", "desktopprovider", "", iniFile);
            ini.LockProvider = GetPrivateProfileString("app", "lockprovider", "", iniFile);
            ini.ToastProvider = GetPrivateProfileString("app", "toastprovider", "", iniFile);
            ini.TileProvider = GetPrivateProfileString("app", "tileprovider", "", iniFile);
            ini.Folder = GetPrivateProfileString("app", "folder", "", iniFile);
            ini.Cache = GetPrivateProfileInt("app", "cache", 600, iniFile);

            ini.Local.DesktopPeriod = GetPrivateProfileFloat(LocalIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Local.LockPeriod = GetPrivateProfileFloat(LocalIni.GetId(), "lockperiod", 24, iniFile);
            ini.Local.ToastPeriod = GetPrivateProfileFloat(LocalIni.GetId(), "toastperiod", 24, iniFile);
            ini.Local.TilePeriod = GetPrivateProfileFloat(LocalIni.GetId(), "tileperiod", 2, iniFile);
            ini.Local.Folder = GetPrivateProfileString(LocalIni.GetId(), "folder", "", iniFile);
            ini.Local.Depth = GetPrivateProfileInt(LocalIni.GetId(), "depth", 0, iniFile);

            ini.Bing.DesktopPeriod = GetPrivateProfileFloat(BingIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Bing.LockPeriod = GetPrivateProfileFloat(BingIni.GetId(), "lockperiod", 24, iniFile);
            ini.Bing.ToastPeriod = GetPrivateProfileFloat(BingIni.GetId(), "toastperiod", 24, iniFile);
            ini.Bing.TilePeriod = GetPrivateProfileFloat(BingIni.GetId(), "tileperiod", 2, iniFile);

            ini.Nasa.DesktopPeriod = GetPrivateProfileFloat(NasaIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Nasa.LockPeriod = GetPrivateProfileFloat(NasaIni.GetId(), "lockperiod", 24, iniFile);
            ini.Nasa.ToastPeriod = GetPrivateProfileFloat(NasaIni.GetId(), "toastperiod", 24, iniFile);
            ini.Nasa.TilePeriod = GetPrivateProfileFloat(NasaIni.GetId(), "tileperiod", 2, iniFile);

            ini.Timeline.DesktopPeriod = GetPrivateProfileFloat(TimelineIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Timeline.LockPeriod = GetPrivateProfileFloat(TimelineIni.GetId(), "lockperiod", 24, iniFile);
            ini.Timeline.ToastPeriod = GetPrivateProfileFloat(TimelineIni.GetId(), "toastperiod", 24, iniFile);
            ini.Timeline.TilePeriod = GetPrivateProfileFloat(TimelineIni.GetId(), "tileperiod", 2, iniFile);

            ini.One.DesktopPeriod = GetPrivateProfileFloat(OneIni.GetId(), "desktopperiod", 24, iniFile);
            ini.One.LockPeriod = GetPrivateProfileFloat(OneIni.GetId(), "lockperiod", 24, iniFile);
            ini.One.ToastPeriod = GetPrivateProfileFloat(OneIni.GetId(), "toastperiod", 24, iniFile);
            ini.One.TilePeriod = GetPrivateProfileFloat(OneIni.GetId(), "tileperiod", 2, iniFile);

            ini.Himawari8.DesktopPeriod = GetPrivateProfileFloat(Himawari8Ini.GetId(), "desktopperiod", 1, iniFile);
            ini.Himawari8.LockPeriod = GetPrivateProfileFloat(Himawari8Ini.GetId(), "lockperiod", 2, iniFile);
            ini.Himawari8.ToastPeriod = GetPrivateProfileFloat(Himawari8Ini.GetId(), "toastperiod", 24, iniFile);
            ini.Himawari8.TilePeriod = GetPrivateProfileFloat(Himawari8Ini.GetId(), "tileperiod", 2, iniFile);
            ini.Himawari8.Offset = GetPrivateProfileFloat(Himawari8Ini.GetId(), "offset", 0.5f, iniFile);
            ini.Himawari8.Ratio = GetPrivateProfileFloat(Himawari8Ini.GetId(), "ratio", 0.5f, iniFile);

            ini.Ymyouli.DesktopPeriod = GetPrivateProfileFloat(YmyouliIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Ymyouli.LockPeriod = GetPrivateProfileFloat(YmyouliIni.GetId(), "lockperiod", 24, iniFile);
            ini.Ymyouli.ToastPeriod = GetPrivateProfileFloat(YmyouliIni.GetId(), "toastperiod", 24, iniFile);
            ini.Ymyouli.TilePeriod = GetPrivateProfileFloat(YmyouliIni.GetId(), "tileperiod", 2, iniFile);
            ini.Ymyouli.Order = GetPrivateProfileString(YmyouliIni.GetId(), "order", "random", iniFile);
            ini.Ymyouli.Cate = GetPrivateProfileString(YmyouliIni.GetId(), "cate", "", iniFile);

            ini.Qingbz.DesktopPeriod = GetPrivateProfileFloat(QingbzIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Qingbz.LockPeriod = GetPrivateProfileFloat(QingbzIni.GetId(), "lockperiod", 24, iniFile);
            ini.Qingbz.ToastPeriod = GetPrivateProfileFloat(QingbzIni.GetId(), "toastperiod", 24, iniFile);
            ini.Qingbz.TilePeriod = GetPrivateProfileFloat(QingbzIni.GetId(), "tileperiod", 2, iniFile);
            ini.Qingbz.Order = GetPrivateProfileString(QingbzIni.GetId(), "order", "random", iniFile);
            ini.Qingbz.Cate = GetPrivateProfileString(QingbzIni.GetId(), "cate", "", iniFile);

            ini.Wallhaven.DesktopPeriod = GetPrivateProfileFloat(WallhavenIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Wallhaven.LockPeriod = GetPrivateProfileFloat(WallhavenIni.GetId(), "lockperiod", 24, iniFile);
            ini.Wallhaven.ToastPeriod = GetPrivateProfileFloat(WallhavenIni.GetId(), "toastperiod", 24, iniFile);
            ini.Wallhaven.TilePeriod = GetPrivateProfileFloat(WallhavenIni.GetId(), "tileperiod", 2, iniFile);
            ini.Wallhaven.Order = GetPrivateProfileString(WallhavenIni.GetId(), "order", "random", iniFile);
            ini.Wallhaven.Cate = GetPrivateProfileString(WallhavenIni.GetId(), "cate", "", iniFile);

            ini.Wallhere.DesktopPeriod = GetPrivateProfileFloat(WallhereIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Wallhere.LockPeriod = GetPrivateProfileFloat(WallhereIni.GetId(), "lockperiod", 24, iniFile);
            ini.Wallhere.ToastPeriod = GetPrivateProfileFloat(WallhereIni.GetId(), "toastperiod", 24, iniFile);
            ini.Wallhere.TilePeriod = GetPrivateProfileFloat(WallhereIni.GetId(), "tileperiod", 2, iniFile);
            ini.Wallhere.Order = GetPrivateProfileString(WallhereIni.GetId(), "order", "random", iniFile);
            ini.Wallhere.Cate = GetPrivateProfileString(WallhereIni.GetId(), "cate", "", iniFile);

            ini.Zzzmh.DesktopPeriod = GetPrivateProfileFloat(ZzzmhIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Zzzmh.LockPeriod = GetPrivateProfileFloat(ZzzmhIni.GetId(), "lockperiod", 24, iniFile);
            ini.Zzzmh.ToastPeriod = GetPrivateProfileFloat(ZzzmhIni.GetId(), "toastperiod", 24, iniFile);
            ini.Zzzmh.TilePeriod = GetPrivateProfileFloat(ZzzmhIni.GetId(), "tileperiod", 2, iniFile);
            ini.Zzzmh.Order = GetPrivateProfileString(ZzzmhIni.GetId(), "order", "random", iniFile);
            ini.Zzzmh.Cate = GetPrivateProfileString(ZzzmhIni.GetId(), "cate", "", iniFile);

            ini.Toopic.DesktopPeriod = GetPrivateProfileFloat(ToopicIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Toopic.LockPeriod = GetPrivateProfileFloat(ToopicIni.GetId(), "lockperiod", 24, iniFile);
            ini.Toopic.ToastPeriod = GetPrivateProfileFloat(ToopicIni.GetId(), "toastperiod", 24, iniFile);
            ini.Toopic.TilePeriod = GetPrivateProfileFloat(ToopicIni.GetId(), "tileperiod", 2, iniFile);
            ini.Toopic.Order = GetPrivateProfileString(ToopicIni.GetId(), "order", "random", iniFile);
            ini.Toopic.Cate = GetPrivateProfileString(ToopicIni.GetId(), "cate", "", iniFile);

            ini.Netbian.DesktopPeriod = GetPrivateProfileFloat(NetbianIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Netbian.LockPeriod = GetPrivateProfileFloat(NetbianIni.GetId(), "lockperiod", 24, iniFile);
            ini.Netbian.ToastPeriod = GetPrivateProfileFloat(NetbianIni.GetId(), "toastperiod", 24, iniFile);
            ini.Netbian.TilePeriod = GetPrivateProfileFloat(NetbianIni.GetId(), "tileperiod", 2, iniFile);
            ini.Netbian.Order = GetPrivateProfileString(NetbianIni.GetId(), "order", "random", iniFile);
            ini.Netbian.Cate = GetPrivateProfileString(NetbianIni.GetId(), "cate", "", iniFile);

            ini.Backiee.DesktopPeriod = GetPrivateProfileFloat(BackieeIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Backiee.LockPeriod = GetPrivateProfileFloat(BackieeIni.GetId(), "lockperiod", 24, iniFile);
            ini.Backiee.ToastPeriod = GetPrivateProfileFloat(BackieeIni.GetId(), "toastperiod", 24, iniFile);
            ini.Backiee.TilePeriod = GetPrivateProfileFloat(BackieeIni.GetId(), "tileperiod", 2, iniFile);
            ini.Backiee.Order = GetPrivateProfileString(BackieeIni.GetId(), "order", "random", iniFile);
            ini.Backiee.Cate = GetPrivateProfileString(BackieeIni.GetId(), "cate", "", iniFile);

            ini.Infinity.DesktopPeriod = GetPrivateProfileFloat(InfinityIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Infinity.LockPeriod = GetPrivateProfileFloat(InfinityIni.GetId(), "lockperiod", 24, iniFile);
            ini.Infinity.ToastPeriod = GetPrivateProfileFloat(InfinityIni.GetId(), "toastperiod", 24, iniFile);
            ini.Infinity.TilePeriod = GetPrivateProfileFloat(InfinityIni.GetId(), "tileperiod", 2, iniFile);
            ini.Infinity.Order = GetPrivateProfileString(InfinityIni.GetId(), "order", "random", iniFile);

            ini.Ihansen.DesktopPeriod = GetPrivateProfileFloat(IhansenIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Ihansen.LockPeriod = GetPrivateProfileFloat(IhansenIni.GetId(), "lockperiod", 24, iniFile);
            ini.Ihansen.ToastPeriod = GetPrivateProfileFloat(IhansenIni.GetId(), "toastperiod", 24, iniFile);
            ini.Ihansen.TilePeriod = GetPrivateProfileFloat(IhansenIni.GetId(), "tileperiod", 2, iniFile);
            ini.Ihansen.Order = GetPrivateProfileString(IhansenIni.GetId(), "order", "date", iniFile);

            ini.Glutton.DesktopPeriod = GetPrivateProfileFloat(GluttonIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Glutton.LockPeriod = GetPrivateProfileFloat(GluttonIni.GetId(), "lockperiod", 24, iniFile);
            ini.Glutton.ToastPeriod = GetPrivateProfileFloat(GluttonIni.GetId(), "toastperiod", 24, iniFile);
            ini.Glutton.TilePeriod = GetPrivateProfileFloat(GluttonIni.GetId(), "tileperiod", 2, iniFile);
            ini.Glutton.Album = GetPrivateProfileString(GluttonIni.GetId(), "album", "journal", iniFile);
            ini.Glutton.Order = GetPrivateProfileString(GluttonIni.GetId(), "order", "date", iniFile);

            ini.Lsp.DesktopPeriod = GetPrivateProfileFloat(LspIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Lsp.LockPeriod = GetPrivateProfileFloat(LspIni.GetId(), "lockperiod", 24, iniFile);
            ini.Lsp.ToastPeriod = GetPrivateProfileFloat(LspIni.GetId(), "toastperiod", 24, iniFile);
            ini.Lsp.TilePeriod = GetPrivateProfileFloat(LspIni.GetId(), "tileperiod", 2, iniFile);
            ini.Lsp.Order = GetPrivateProfileString(LspIni.GetId(), "order", "random", iniFile);
            ini.Lsp.Cate = GetPrivateProfileString(LspIni.GetId(), "cate", "", iniFile);

            ini.Oneplus.DesktopPeriod = GetPrivateProfileFloat(OneplusIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Oneplus.LockPeriod = GetPrivateProfileFloat(OneplusIni.GetId(), "lockperiod", 24, iniFile);
            ini.Oneplus.ToastPeriod = GetPrivateProfileFloat(OneplusIni.GetId(), "toastperiod", 24, iniFile);
            ini.Oneplus.TilePeriod = GetPrivateProfileFloat(OneplusIni.GetId(), "tileperiod", 2, iniFile);

            ini.Wallpaperup.DesktopPeriod = GetPrivateProfileFloat(WallpaperupIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Wallpaperup.LockPeriod = GetPrivateProfileFloat(WallpaperupIni.GetId(), "lockperiod", 24, iniFile);
            ini.Wallpaperup.ToastPeriod = GetPrivateProfileFloat(WallpaperupIni.GetId(), "toastperiod", 24, iniFile);
            ini.Wallpaperup.TilePeriod = GetPrivateProfileFloat(WallpaperupIni.GetId(), "tileperiod", 2, iniFile);
            ini.Wallpaperup.Order = GetPrivateProfileString(WallpaperupIni.GetId(), "order", "random", iniFile);
            ini.Wallpaperup.Cate = GetPrivateProfileString(WallpaperupIni.GetId(), "cate", "", iniFile);

            ini.Obzhi.DesktopPeriod = GetPrivateProfileFloat(ObzhiIni.GetId(), "desktopperiod", 24, iniFile);
            ini.Obzhi.LockPeriod = GetPrivateProfileFloat(ObzhiIni.GetId(), "lockperiod", 24, iniFile);
            ini.Obzhi.ToastPeriod = GetPrivateProfileFloat(ObzhiIni.GetId(), "toastperiod", 24, iniFile);
            ini.Obzhi.TilePeriod = GetPrivateProfileFloat(ObzhiIni.GetId(), "tileperiod", 2, iniFile);
            ini.Obzhi.Order = GetPrivateProfileString(ObzhiIni.GetId(), "order", "random", iniFile);
            ini.Obzhi.Cate = GetPrivateProfileString(ObzhiIni.GetId(), "cate", "", iniFile);
            return ini;
        }
    }

    public sealed class SysUtil {
        internal struct PowerStatus {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }

        [DllImport("kernel32.dll")]
        internal static extern bool GetSystemPowerStatus(out PowerStatus BatteryInfo);

        /// <summary>
        /// 通过电池状态判定PC类型
        /// </summary>
        /// <returns>
        /// desktop：台式电脑
        /// laptop：笔记本电脑或平板
        /// </returns>
        public static string GetPcType() {
            if (GetSystemPowerStatus(out PowerStatus status)) {
                if (status.BatteryFlag == 255) { // Unknown status
                    return null;
                } else if (status.BatteryFlag == 128) { // No system battery
                    return "desktop";
                }
                // 1: High
                // 2: Low
                // 4: Critical
                // 8: Charging
                return "laptop";
            }
            return null;
        }

        public static string GetPkgVer(bool forShort) {
            if (forShort) {
                return string.Format("{0}.{1}",
                    Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor);
            }
            return string.Format("{0}.{1}.{2}.{3}",
                Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor,
                Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
        }

        public static string GetOsVer() {
            ulong version = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
            ulong major = (version & 0xFFFF000000000000L) >> 48;
            ulong minor = (version & 0x0000FFFF00000000L) >> 32;
            ulong build = (version & 0x00000000FFFF0000L) >> 16;
            ulong revision = (version & 0x000000000000FFFFL);
            return $"{major}.{minor}.{build}.{revision}";
        }

        public static int GetOsVerMajor() {
            // Win11：10.0.22000.194
            ulong version = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
            ulong major = (version & 0xFFFF000000000000L) >> 48;
            ulong minor = (version & 0x0000FFFF00000000L) >> 32;
            ulong build = (version & 0x00000000FFFF0000L) >> 16;
            ulong revision = (version & 0x000000000000FFFFL);
            if (major > 10) {
                return 11;
            } else if (major == 10) {
                if (minor > 0) {
                    return 11;
                } else if (minor == 0) {
                    if (build > 22000) {
                        return 11;
                    } else if (build == 22000) {
                        if (revision >= 194) {
                            return 11;
                        }
                    }
                }
            }
            return 10;
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
    }

    public sealed class FileUtil {
        public static IAsyncOperation<IReadOnlyDictionary<string, int>> ReadDosage() {
            return ReadDosage_Impl().AsAsyncOperation();
        }

        public static IAsyncOperation<bool> WriteDosage(string provider) {
            return WriteDosage_Impl(provider).AsAsyncOperation();
        }

        public static IAsyncOperation<string> ReadProviderCache(string provider, string p1, string p2) {
            return ReadProviderCache_Impl(provider, p1, p2).AsAsyncOperation();
        }

        public static IAsyncOperation<bool> WriteProviderCache(string provider, string p1, string p2, string data) {
            return WriteProviderCache_Impl(provider, p1, p2, data).AsAsyncOperation();
        }

        public static IAsyncOperation<bool> ClearCache(int count_threshold) {
            return ClearCache_Impl(count_threshold).AsAsyncOperation();
        }

        private static async Task<IReadOnlyDictionary<string, int>> ReadDosage_Impl() {
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

        private static async Task<bool> WriteDosage_Impl(string provider) {
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
                return true;
            } catch (Exception e) {
                Debug.WriteLine(e);
                LogUtil.E("WriteDosage() " + e.Message);
            }
            return false;
        }

        private static async Task<string> ReadProviderCache_Impl(string provider, string p1, string p2) {
            try {
                StorageFolder folder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync("data",
                    CreationCollisionOption.OpenIfExists);
                string name = string.Format("{0}-{1}-{2}-{3}.json", provider ?? "", p1 ?? "", p2 ?? "", DateTime.Now.ToString("yyyyMMdd"));
                StorageFile file = await folder.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);
                return await FileIO.ReadTextAsync(file);
            } catch (Exception e) {
                Debug.WriteLine(e);
                LogUtil.E("WriteDosage() " + e.Message);
            }
            return null;
        }

        private static async Task<bool> WriteProviderCache_Impl(string provider, string p1, string p2, string data) {
            try {
                StorageFolder folder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync("data",
                    CreationCollisionOption.OpenIfExists);
                string name = string.Format("{0}-{1}-{2}-{3}.json", provider ?? "", p1 ?? "", p2 ?? "", DateTime.Now.ToString("yyyyMMdd"));
                StorageFile file = await folder.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(file, data);
                return true;
            } catch (Exception e) {
                Debug.WriteLine(e);
                LogUtil.E("WriteDosage() " + e.Message);
            }
            return false;
        }

        private static async Task<bool> ClearCache_Impl(int count_threshold) {
            count_threshold = Math.Max(count_threshold, 0);
            try {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("wallpaper",
                    CreationCollisionOption.OpenIfExists); // 壁纸缓存文件夹
                FileInfo[] files = new DirectoryInfo(folder.Path).GetFiles(); // 缓存图片
                Array.Sort(files, (a, b) => (b as FileInfo).CreationTime.CompareTo((a as FileInfo).CreationTime)); // 日期降序排列
                int count_clear = 0;
                for (int i = count_threshold; i < files.Length; ++i) { // 删除超量图片
                    files[i].Delete();
                    count_clear++;
                }
                LogUtil.I("ClearCache() " + count_clear);
                return true;
            } catch (Exception e) {
                LogUtil.E("ClearCache() " + e.Message);
            }
            return false;
        }

        public static string GetIniName() {
            return string.Format("timeline-{0}.ini", SysUtil.GetPkgVer(true));
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
            string logFilePath = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "logs/timelineservice.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            isInitialized = true;
            Log.Debug("Initialized Serilog");
        }
    }

    public sealed class DateUtil {
        private static long ToUnixMillis(DateTime date) {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return (long)diff.TotalMilliseconds;
        }

        public static long CurrentTimeMillis() {
            return ToUnixMillis(DateTime.Now);
        }
    }
}
