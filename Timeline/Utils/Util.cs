using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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

namespace Timeline.Utils {
    public class IniUtil {
        // TODO: 参数有变动时需调整配置名
        private const string FILE_INI = "timeline-5.9.ini";

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defValue,
            StringBuilder returnedString, int size, string filePath);

        [DllImport("kernel32")]
        private static extern int WritePrivateProfileString(string section, string key, string value, string filePath);

        private static async Task<StorageFile> GenerateIniFileAsync() {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile iniFile = await folder.TryGetItemAsync(FILE_INI) as StorageFile;
            if (iniFile == null) { // 生成初始配置文件
                FileInfo[] oldFiles = new DirectoryInfo(folder.Path).GetFiles("*.ini", SearchOption.TopDirectoryOnly);
                Array.Sort(oldFiles, (a, b) => (b as FileInfo).CreationTime.CompareTo((a as FileInfo).CreationTime));
                StorageFile configFile = await Package.Current.InstalledLocation.GetFileAsync("Assets\\Config\\config.txt");
                iniFile = await configFile.CopyAsync(folder, FILE_INI, NameCollisionOption.ReplaceExisting);
                LogUtil.D("GenerateIniFileAsync() copied ini: " + iniFile.Path);
                if (oldFiles.Length > 0) { // 继承设置
                    LogUtil.D("GenerateIniFileAsync() inherit: " + oldFiles[0].Name);
                    StringBuilder sb = new StringBuilder(1024);
                    _ = GetPrivateProfileString("app", "provider", BingIni.ID, sb, 1024, oldFiles[0].FullName);
                    _ = WritePrivateProfileString("app", "provider", sb.ToString(), iniFile.Path);
                    _ = GetPrivateProfileString("app", "desktopprovider", "", sb, 1024, oldFiles[0].FullName);
                    _ = WritePrivateProfileString("app", "desktopprovider", sb.ToString(), iniFile.Path);
                    _ = GetPrivateProfileString("app", "lockprovider", "", sb, 1024, oldFiles[0].FullName);
                    _ = WritePrivateProfileString("app", "lockprovider", sb.ToString(), iniFile.Path);
                    _ = GetPrivateProfileString("app", "theme", "", sb, 1024, oldFiles[0].FullName);
                    _ = WritePrivateProfileString("app", "theme", sb.ToString(), iniFile.Path);
                    _ = GetPrivateProfileString("app", "cache", "1000", sb, 1024, oldFiles[0].FullName);
                    _ = WritePrivateProfileString("app", "cache", sb.ToString(), iniFile.Path);
                    _ = GetPrivateProfileString("app", "r18", "0", sb, 1024, oldFiles[0].FullName);
                    _ = WritePrivateProfileString("app", "r18", sb.ToString(), iniFile.Path);
                }
            }
            return iniFile;
        }

        private static string GetIniFile() {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            string iniFile = Path.Combine(folder.Path, FILE_INI);
            return File.Exists(iniFile) ? iniFile : null;
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

        public static async Task SaveBingLangAsync(string langCode) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(BingIni.ID, "lang", langCode, iniFile.Path);
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

        public static async Task SaveObzhiOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(ObzhiIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveObzhiCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(ObzhiIni.ID, "cate", cate, iniFile.Path);
        }

        public static async Task SaveLspOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(LspIni.ID, "order", order, iniFile.Path);
        }

        public static async Task SaveLspCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(LspIni.ID, "cate", cate, iniFile.Path);
        }

        public static async Task SaveLocalOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString(LocalIni.ID, "order", order, iniFile.Path);
        }

        public static async Task<StorageFile> GetIniPath() {
            return await GenerateIniFileAsync();
        }

        public static Ini GetIni() {
            string iniFile = GetIniFile();
            LogUtil.D("GetIni() " + FILE_INI);
            Ini ini = new Ini();
            if (iniFile == null) { // 尚未初始化
                return ini;
            }
            StringBuilder sb = new StringBuilder(1024);
            _ = GetPrivateProfileString(LocalIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out int desktopPeriod);
            _ = GetPrivateProfileString(LocalIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out int lockPeriod);
            _ = GetPrivateProfileString(LocalIni.ID, "folder", "", sb, 1024, iniFile);
            ini.SetIni(LocalIni.ID, new LocalIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Folder = sb.ToString()
            });
            _ = GetPrivateProfileString("app", "provider", BingIni.ID, sb, 1024, iniFile);
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
            _ = GetPrivateProfileString(BingIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(BingIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            _ = GetPrivateProfileString(BingIni.ID, "lang", "", sb, 1024, iniFile);
            ini.SetIni(BingIni.ID, new BingIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Lang = sb.ToString()
            });
            _ = GetPrivateProfileString(NasaIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(NasaIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            _ = GetPrivateProfileString(NasaIni.ID, "mirror", "", sb, 1024, iniFile);
            ini.SetIni(NasaIni.ID, new NasaIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Mirror = sb.ToString()
            });
            _ = GetPrivateProfileString(OneplusIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(OneplusIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            _ = GetPrivateProfileString(OneplusIni.ID, "order", "date", sb, 1024, iniFile);
            ini.SetIni(OneplusIni.ID, new OneplusIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Order = sb.ToString()
            });
            _ = GetPrivateProfileString(TimelineIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(TimelineIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            TimelineIni timelineIni = new TimelineIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString(TimelineIni.ID, "order", "date", sb, 1024, iniFile);
            timelineIni.Order = sb.ToString();
            _ = GetPrivateProfileString(TimelineIni.ID, "cate", "", sb, 1024, iniFile);
            timelineIni.Cate = sb.ToString();
            _ = GetPrivateProfileString(TimelineIni.ID, "unauthorized", "0", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out int unauthorized);
            timelineIni.Unauthorized = unauthorized;
            ini.SetIni(TimelineIni.ID, timelineIni);
            _ = GetPrivateProfileString(OneIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(OneIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            _ = GetPrivateProfileString(OneIni.ID, "order", "date", sb, 1024, iniFile);
            ini.SetIni(OneIni.ID, new OneIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Order = sb.ToString()
            });
            _ = GetPrivateProfileString(Himawari8Ini.ID, "desktopperiod", "1", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(Himawari8Ini.ID, "lockperiod", "2", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            _ = GetPrivateProfileString(Himawari8Ini.ID, "offset", "0.50", sb, 1024, iniFile);
            _ = float.TryParse(sb.ToString(), out float offset);
            _ = GetPrivateProfileString(Himawari8Ini.ID, "ratio", "0.50", sb, 1024, iniFile);
            _ = float.TryParse(sb.ToString(), out float ratio);
            ini.SetIni(Himawari8Ini.ID, new Himawari8Ini {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Offset = offset,
                Ratio = ratio
            });
            _ = GetPrivateProfileString(YmyouliIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(YmyouliIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            YmyouliIni ymyouliIni = new YmyouliIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString(YmyouliIni.ID, "order", "random", sb, 1024, iniFile);
            ymyouliIni.Order = sb.ToString();
            _ = GetPrivateProfileString(YmyouliIni.ID, "cate", "", sb, 1024, iniFile);
            ymyouliIni.Cate = sb.ToString();
            ini.SetIni(YmyouliIni.ID, ymyouliIni);
            _ = GetPrivateProfileString(WallhavenIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(WallhavenIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            WallhavenIni wallhavenIni = new WallhavenIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString(WallhavenIni.ID, "order", "random", sb, 1024, iniFile);
            wallhavenIni.Order = sb.ToString();
            _ = GetPrivateProfileString(WallhavenIni.ID, "cate", "", sb, 1024, iniFile);
            wallhavenIni.Cate = sb.ToString();
            _ = GetPrivateProfileString(WallhavenIni.ID, "unaudited", "0", sb, 1024, iniFile);
            wallhavenIni.Unaudited = "1".Equals(sb.ToString()); // 管理员通途
            ini.SetIni(WallhavenIni.ID, wallhavenIni);
            _ = GetPrivateProfileString(QingbzIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(QingbzIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            QingbzIni qingbzIni = new QingbzIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString(QingbzIni.ID, "order", "random", sb, 1024, iniFile);
            qingbzIni.Order = sb.ToString();
            _ = GetPrivateProfileString(QingbzIni.ID, "cate", "", sb, 1024, iniFile);
            qingbzIni.Cate = sb.ToString();
            _ = GetPrivateProfileString(QingbzIni.ID, "unaudited", "0", sb, 1024, iniFile);
            qingbzIni.Unaudited = "1".Equals(sb.ToString()); // 管理员通途
            ini.SetIni(QingbzIni.ID, qingbzIni);
            _ = GetPrivateProfileString(WallhereIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(WallhereIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            WallhereIni wallhereIni = new WallhereIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString(WallhereIni.ID, "order", "random", sb, 1024, iniFile);
            wallhereIni.Order = sb.ToString();
            _ = GetPrivateProfileString(WallhereIni.ID, "cate", "", sb, 1024, iniFile);
            wallhereIni.Cate = sb.ToString();
            _ = GetPrivateProfileString(WallhereIni.ID, "unaudited", "0", sb, 1024, iniFile);
            wallhereIni.Unaudited = "1".Equals(sb.ToString()); // 管理员通途
            ini.SetIni(wallhereIni.Id, wallhereIni);
            _ = GetPrivateProfileString(InfinityIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(InfinityIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            _ = GetPrivateProfileString(InfinityIni.ID, "order", "random", sb, 1024, iniFile);
            ini.SetIni(InfinityIni.ID, new InfinityIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Order = sb.ToString()
            });
            _ = GetPrivateProfileString(ObzhiIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(ObzhiIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            ObzhiIni obzhiIni = new ObzhiIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString(ObzhiIni.ID, "order", "random", sb, 1024, iniFile);
            obzhiIni.Order = sb.ToString();
            _ = GetPrivateProfileString(ObzhiIni.ID, "cate", "", sb, 1024, iniFile);
            obzhiIni.Cate = sb.ToString();
            ini.SetIni(ObzhiIni.ID, obzhiIni);
            _ = GetPrivateProfileString(LspIni.ID, "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString(LspIni.ID, "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            LspIni lspIni = new LspIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString(LspIni.ID, "order", "random", sb, 1024, iniFile);
            lspIni.Order = sb.ToString();
            _ = GetPrivateProfileString(LspIni.ID, "cate", "", sb, 1024, iniFile);
            lspIni.Cate = sb.ToString();
            _ = GetPrivateProfileString(LspIni.ID, "unaudited", "0", sb, 1024, iniFile);
            lspIni.Unaudited = "1".Equals(sb.ToString()); // 管理员通途
            ini.SetIni(LspIni.ID, lspIni);
            return ini;
        }

        public static async Task<Ini> GetIniAsync() {
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

        public static DateTime? ParseDate(string text) {
            DateTime now = DateTime.Now;
            if (string.IsNullOrEmpty(text)) {
                return null;
            }
            if (Regex.Match(text, @"\d").Success) {
                if (text.Length == 8) {
                    text = text.Substring(0, 4) + "-" + text.Substring(4, 2) + "-" + text.Substring(6);
                } else if (text.Length == 6) {
                    text = text.Substring(0, 2) + "-" + text.Substring(2, 2) + "-" + text.Substring(4);
                } else if (text.Length == 5) {
                    text = text.Substring(0, 2) + "-" + text.Substring(2, 1) + "-" + text.Substring(3);
                } else if (text.Length == 4) {
                    text = text.Substring(0, 2) + "-" + text.Substring(2);
                } else if (text.Length == 3) {
                    text = text.Substring(0, 1) + "-" + text.Substring(1);
                } else if (text.Length == 2) {
                    if (int.Parse(text) > DateTime.DaysInMonth(now.Year, now.Month)) {
                        text = text.Substring(0, 1) + "-" + text.Substring(1);
                    } else {
                        text = now.Month + "-" + text;
                    }
                } else if (text.Length == 1) {
                    text = now.Month + "-" + text;
                }
            }
            if (DateTime.TryParse(text, out DateTime date)) {
                return date;
            }
            return null;
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

        public static async Task<IList<string>> GetGlitterAsync() {
            try {
                StorageFile file = await Package.Current.InstalledLocation.GetFileAsync("Assets\\Config\\glitter.txt");
                if (file != null) {
                    return await FileIO.ReadLinesAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                }
            } catch (Exception e) {
                LogUtil.E("GetGlitterAsync() " + e.Message);
            }
            return new List<string>();
        }

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

            //string key = "Dosage_" + (provider ?? "");
            //return localSettings.Values[key] is string dosage ? dosage.Split(",").Length : 0;
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

            //string key = "dosage_" + (provider ?? "");
            //string[] sec_old = localSettings.Values[key] is string dosage ? dosage.Split(",") : new string[0];
            //List<long> sec_new = new List<long>();
            //long sec_now = DateTime.Now.Ticks / 10000 / 1000;
            //foreach (string item in sec_old) {
            //    long sec = long.Parse(item);
            //    if (sec <= sec_now && sec_now - sec <= 24 * 60 * 60) {
            //        sec_new.Add(sec);
            //    }
            //}
            //sec_new.Add(sec_now);
            //localSettings.Values[key] = string.Join(",", sec_new.ToArray());
        }

        public static async Task<StorageFolder> GetLogFolder() {
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync("logs", CreationCollisionOption.OpenIfExists);
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

        public static void ClearCache(Ini ini) {
            int count_threshold = ini?.Cache ?? 1000; // 缓存量阈值
            StorageFolder folder = ApplicationData.Current.TemporaryFolder; // 缓存文件夹
            try {
                FileInfo[] files = new DirectoryInfo(folder.Path).GetFiles(); // 缓存图片
                Array.Sort(files, (a, b) => (b as FileInfo).CreationTime.CompareTo((a as FileInfo).CreationTime)); // 日期降序排列
                int count_clear = 0;
                for (int i = 1000; i < files.Length; ++i) { // 删除超量图片
                    files[i].Delete();
                    count_clear++;
                }
                LogUtil.I("ClearCache() " + count_clear);
            } catch (Exception e) {
                LogUtil.E("ClearCache() " + e.Message);
            }
        }
    }

    public class SysUtil {
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

        public static Size GetMonitorPhysicalPixels() {
            try {
                DisplayInformation info = DisplayInformation.GetForCurrentView();
                return new Size((int)info.ScreenWidthInRawPixels, (int)info.ScreenHeightInRawPixels);
            } catch (Exception e) {
                LogUtil.E("GetMonitorSize() " + e.Message);
            }
            return new Size();
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
            string logFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs/timeline.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            isInitialized = true;
            Log.Debug("Initialized Serilog");
        }
    }
}
