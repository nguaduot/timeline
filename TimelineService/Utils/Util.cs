﻿using Serilog;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;

namespace TimelineService.Utils {
    public sealed class IniUtil {
        // TODO: 参数有变动时需调整配置名
        private const string FILE_INI = "timeline-5.6.ini";

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defValue,
            StringBuilder returnedString, int size, string filePath);

        private static string GetIniFile() {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            string iniFile = Path.Combine(folder.Path, FILE_INI);
            return File.Exists(iniFile) ? iniFile : null;
        }

        public static Ini GetIni() {
            string iniFile = GetIniFile();
            LogUtil.I("IniUtil.GetIni() " + FILE_INI);
            Ini ini = new Ini();
            if (iniFile == null) { // 尚未初始化
                return ini;
            }
            StringBuilder sb = new StringBuilder(1024);
            _ = GetPrivateProfileString("app", "provider", BingIni.GetId(), sb, 1024, iniFile);
            ini.Provider = sb.ToString();
            _ = GetPrivateProfileString("app", "desktopprovider", "", sb, 1024, iniFile);
            ini.DesktopProvider = sb.ToString();
            _ = GetPrivateProfileString("app", "lockprovider", "", sb, 1024, iniFile);
            ini.LockProvider = sb.ToString();
            _ = GetPrivateProfileString("app", "theme", "", sb, 1024, iniFile);
            ini.Theme = sb.ToString();
            _ = GetPrivateProfileString("app", "cache", "1000", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out int cache);
            ini.Cache = cache;
            _ = GetPrivateProfileString("app", "r18", "0", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out int r18);
            ini.R18 = r18;
            _ = GetPrivateProfileString(BingIni.GetId(), "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out int period);
            ini.Bing.DesktopPeriod = period;
            _ = GetPrivateProfileString(BingIni.GetId(), "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Bing.LockPeriod = period;
            _ = GetPrivateProfileString(BingIni.GetId(), "lang", "", sb, 1024, iniFile);
            ini.Bing.Lang = sb.ToString();
            _ = GetPrivateProfileString(NasaIni.GetId(), "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Nasa.DesktopPeriod = period;
            _ = GetPrivateProfileString(NasaIni.GetId(), "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Nasa.LockPeriod = period;
            _ = GetPrivateProfileString(NasaIni.GetId(), "mirror", "", sb, 1024, iniFile);
            ini.Nasa.Mirror = sb.ToString();
            _ = GetPrivateProfileString(OneplusIni.GetId(), "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Oneplus.DesktopPeriod = period;
            _ = GetPrivateProfileString(OneplusIni.GetId(), "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Oneplus.LockPeriod = period;
            _ = GetPrivateProfileString(OneplusIni.GetId(), "order", "date", sb, 1024, iniFile);
            ini.Oneplus.Order = sb.ToString();
            _ = GetPrivateProfileString(TimelineIni.GetId(), "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Timeline.DesktopPeriod = period;
            _ = GetPrivateProfileString(TimelineIni.GetId(), "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Timeline.LockPeriod = period;
            _ = GetPrivateProfileString(TimelineIni.GetId(), "order", "date", sb, 1024, iniFile);
            ini.Timeline.Order = sb.ToString();
            _ = GetPrivateProfileString(TimelineIni.GetId(), "cate", "", sb, 1024, iniFile);
            ini.Timeline.Cate = sb.ToString();
            _ = GetPrivateProfileString(TimelineIni.GetId(), "unauthorized", "0", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out int unauthorized);
            ini.Timeline.Unauthorized = unauthorized;
            _ = GetPrivateProfileString(OneIni.GetId(), "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.One.DesktopPeriod = period;
            _ = GetPrivateProfileString(OneIni.GetId(), "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.One.LockPeriod = period;
            _ = GetPrivateProfileString(OneIni.GetId(), "order", "date", sb, 1024, iniFile);
            ini.One.Order = sb.ToString();
            _ = GetPrivateProfileString(Himawari8Ini.GetId(), "desktopperiod", "1", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Himawari8.DesktopPeriod = period;
            _ = GetPrivateProfileString(Himawari8Ini.GetId(), "lockperiod", "2", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Himawari8.LockPeriod = period;
            _ = GetPrivateProfileString(Himawari8Ini.GetId(), "offset", "0.50", sb, 1024, iniFile);
            _ = float.TryParse(sb.ToString(), out float offset);
            ini.Himawari8.Offset = offset;
            _ = GetPrivateProfileString(Himawari8Ini.GetId(), "ratio", "0.50", sb, 1024, iniFile);
            _ = float.TryParse(sb.ToString(), out float ratio);
            ini.Himawari8.Ratio = ratio;
            _ = GetPrivateProfileString(YmyouliIni.GetId(), "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Ymyouli.DesktopPeriod = period;
            _ = GetPrivateProfileString(YmyouliIni.GetId(), "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Ymyouli.LockPeriod = period;
            _ = GetPrivateProfileString(YmyouliIni.GetId(), "order", "random", sb, 1024, iniFile);
            ini.Ymyouli.Order = sb.ToString();
            _ = GetPrivateProfileString(YmyouliIni.GetId(), "cate", "", sb, 1024, iniFile);
            ini.Ymyouli.Cate = sb.ToString();
            _ = GetPrivateProfileString(WallhavenIni.GetId(), "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Wallhaven.DesktopPeriod = period;
            _ = GetPrivateProfileString(WallhavenIni.GetId(), "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Wallhaven.LockPeriod = period;
            _ = GetPrivateProfileString(WallhavenIni.GetId(), "order", "random", sb, 1024, iniFile);
            ini.Wallhaven.Order = sb.ToString();
            _ = GetPrivateProfileString(WallhavenIni.GetId(), "cate", "", sb, 1024, iniFile);
            ini.Wallhaven.Cate = sb.ToString();
            _ = GetPrivateProfileString(QingbzIni.GetId(), "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Qingbz.DesktopPeriod = period;
            _ = GetPrivateProfileString(QingbzIni.GetId(), "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Qingbz.LockPeriod = period;
            _ = GetPrivateProfileString(QingbzIni.GetId(), "order", "random", sb, 1024, iniFile);
            ini.Qingbz.Order = sb.ToString();
            _ = GetPrivateProfileString(QingbzIni.GetId(), "cate", "", sb, 1024, iniFile);
            ini.Qingbz.Cate = sb.ToString();
            _ = GetPrivateProfileString(WallhereIni.GetId(), "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Wallhere.DesktopPeriod = period;
            _ = GetPrivateProfileString(WallhereIni.GetId(), "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Wallhere.LockPeriod = period;
            _ = GetPrivateProfileString(WallhereIni.GetId(), "order", "random", sb, 1024, iniFile);
            ini.Wallhere.Order = sb.ToString();
            _ = GetPrivateProfileString(WallhereIni.GetId(), "cate", "", sb, 1024, iniFile);
            ini.Wallhere.Cate = sb.ToString();
            _ = GetPrivateProfileString(InfinityIni.GetId(), "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Infinity.DesktopPeriod = period;
            _ = GetPrivateProfileString(InfinityIni.GetId(), "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Infinity.LockPeriod = period;
            _ = GetPrivateProfileString(InfinityIni.GetId(), "order", "random", sb, 1024, iniFile);
            ini.Infinity.Order = sb.ToString();
            _ = GetPrivateProfileString(ObzhiIni.GetId(), "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Obzhi.DesktopPeriod = period;
            _ = GetPrivateProfileString(ObzhiIni.GetId(), "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Obzhi.LockPeriod = period;
            _ = GetPrivateProfileString(ObzhiIni.GetId(), "order", "random", sb, 1024, iniFile);
            ini.Obzhi.Order = sb.ToString();
            _ = GetPrivateProfileString(ObzhiIni.GetId(), "cate", "", sb, 1024, iniFile);
            ini.Obzhi.Cate = sb.ToString();
            _ = GetPrivateProfileString(LspIni.GetId(), "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Lsp.DesktopPeriod = period;
            _ = GetPrivateProfileString(LspIni.GetId(), "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out period);
            ini.Lsp.LockPeriod = period;
            _ = GetPrivateProfileString(LspIni.GetId(), "order", "random", sb, 1024, iniFile);
            ini.Lsp.Order = sb.ToString();
            _ = GetPrivateProfileString(LspIni.GetId(), "cate", "", sb, 1024, iniFile);
            ini.Lsp.Cate = sb.ToString();
            return ini;
        }
    }

    public sealed class VerUtil {
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

        public static string GetDevice() {
            var deviceInfo = new EasClientDeviceInformation();
            if (deviceInfo.SystemSku.Length > 0) {
                return deviceInfo.SystemSku;
            }
            return string.Format("{0}_{1}", deviceInfo.SystemManufacturer,
                deviceInfo.SystemProductName);
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
            string logFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs/timelineservice.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            isInitialized = true;
            Log.Debug("Initialized Serilog");
        }
    }
}
