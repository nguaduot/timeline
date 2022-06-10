﻿using Newtonsoft.Json;
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
        private const string FILE_INI = "timeline-5.5.ini";

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defValue,
            StringBuilder returnedString, int size, string filePath);

        [DllImport("kernel32")]
        private static extern int WritePrivateProfileString(string section, string key, string value, string filePath);

        private static async Task<StorageFile> GenerateIniFileAsync() {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile iniFile = await folder.TryGetItemAsync(FILE_INI) as StorageFile;
            if (iniFile == null) { // 生成初始配置文件
                FileInfo[] oldFiles = new DirectoryInfo(folder.Path).GetFiles("*.ini");
                Array.Sort(oldFiles, (a, b) => (b as FileInfo).CreationTime.CompareTo((a as FileInfo).CreationTime));
                StorageFile configFile = await Package.Current.InstalledLocation.GetFileAsync("Assets\\Config\\config.txt");
                iniFile = await configFile.CopyAsync(folder, FILE_INI, NameCollisionOption.ReplaceExisting);
                LogUtil.D("GenerateIniFileAsync() copied ini: " + iniFile.Path);
                if (oldFiles.Length > 0) { // 继承设置
                    LogUtil.D("GenerateIniFileAsync() inherit: " + oldFiles[0].Name);
                    StringBuilder sb = new StringBuilder(1024);
                    _ = GetPrivateProfileString("app", "provider", "bing", sb, 1024, oldFiles[0].FullName);
                    _ = WritePrivateProfileString("app", "provider", sb.ToString(), iniFile.Path);
                    _ = GetPrivateProfileString("app", "desktopprovider", "", sb, 1024, oldFiles[0].FullName);
                    _ = WritePrivateProfileString("app", "desktopprovider", sb.ToString(), iniFile.Path);
                    _ = GetPrivateProfileString("app", "lockprovider", "", sb, 1024, oldFiles[0].FullName);
                    _ = WritePrivateProfileString("app", "lockprovider", sb.ToString(), iniFile.Path);
                    _ = GetPrivateProfileString("app", "theme", "", sb, 1024, oldFiles[0].FullName);
                    _ = WritePrivateProfileString("app", "theme", sb.ToString(), iniFile.Path);
                    _ = GetPrivateProfileString("app", "r18", "0", sb, 1024, oldFiles[0].FullName);
                    if (sb.ToString().Equals("0")) {
                        _ = GetPrivateProfileString("ymyouli", "r18", "0", sb, 1024, oldFiles[0].FullName);
                        if (sb.ToString().Equals("0")) {
                            _ = GetPrivateProfileString("qingbz", "r18", "0", sb, 1024, oldFiles[0].FullName);
                            if (sb.ToString().Equals("0")) {
                                _ = GetPrivateProfileString("obzhi", "r18", "0", sb, 1024, oldFiles[0].FullName);
                                if (sb.ToString().Equals("0")) {
                                    _ = GetPrivateProfileString("wallhere", "r18", "0", sb, 1024, oldFiles[0].FullName);
                                }
                            }
                        }
                    }
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
            _ = WritePrivateProfileString("bing", "lang", langCode, iniFile.Path);
        }

        public static async Task SaveNasaMirrorAsync(string mirror) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("nasa", "mirror", mirror, iniFile.Path);
        }

        public static async Task SaveOneplusOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("oneplus", "order", order, iniFile.Path);
        }

        public static async Task SaveTimelineOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("timeline", "order", order, iniFile.Path);
        }

        public static async Task SaveTimelineCateAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("timeline", "cate", order, iniFile.Path);
        }

        public static async Task SaveHimawari8OffsetAsync(float offset) {
            offset = offset < 0.01f ? 0.01f : (offset > 1 ? 1 : offset);
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("himawari8", "offset", offset.ToString("0.00"), iniFile.Path);
        }

        public static async Task SaveHimawari8RatioAsync(float ratio) {
            ratio = ratio < 0.1f ? 0.1f : (ratio > 1 ? 1 : ratio);
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("himawari8", "ratio", ratio.ToString("0.00"), iniFile.Path);
        }

        public static async Task SaveYmyouliOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("ymyouli", "order", order, iniFile.Path);
        }

        public static async Task SaveYmyouliCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("ymyouli", "cate", cate, iniFile.Path);
        }

        public static async Task SaveInfinityOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("infinity", "order", order, iniFile.Path);
        }

        public static async Task SaveOneOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("one", "order", order, iniFile.Path);
        }

        public static async Task SaveQingbzOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("qingbz", "order", order, iniFile.Path);
        }

        public static async Task SaveQingbzCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("qingbz", "cate", cate, iniFile.Path);
        }

        public static async Task SaveObzhiOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("obzhi", "order", order, iniFile.Path);
        }

        public static async Task SaveObzhiCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("obzhi", "cate", cate, iniFile.Path);
        }

        public static async Task SaveWallhereOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("wallhere", "order", order, iniFile.Path);
        }

        public static async Task SaveWallhereCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("wallhere", "cate", cate, iniFile.Path);
        }

        public static async Task SaveLspOrderAsync(string order) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("lsp", "order", order, iniFile.Path);
        }

        public static async Task SaveLspCateAsync(string cate) {
            StorageFile iniFile = await GenerateIniFileAsync();
            _ = WritePrivateProfileString("lsp", "cate", cate, iniFile.Path);
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
            _ = GetPrivateProfileString("app", "provider", "bing", sb, 1024, iniFile);
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
            _ = GetPrivateProfileString("bing", "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out int desktopPeriod);
            _ = GetPrivateProfileString("bing", "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out int lockPeriod);
            _ = GetPrivateProfileString("bing", "lang", "", sb, 1024, iniFile);
            ini.SetIni("bing", new BingIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Lang = sb.ToString()
            });
            _ = GetPrivateProfileString("nasa", "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString("nasa", "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            _ = GetPrivateProfileString("nasa", "mirror", "", sb, 1024, iniFile);
            ini.SetIni("nasa", new NasaIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Mirror = sb.ToString()
            });
            _ = GetPrivateProfileString("oneplus", "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString("oneplus", "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            _ = GetPrivateProfileString("oneplus", "order", "date", sb, 1024, iniFile);
            ini.SetIni("oneplus", new OneplusIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Order = sb.ToString()
            });
            _ = GetPrivateProfileString("timeline", "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString("timeline", "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            TimelineIni timelineIni = new TimelineIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString("timeline", "order", "date", sb, 1024, iniFile);
            timelineIni.Order = sb.ToString();
            _ = GetPrivateProfileString("timeline", "cate", "", sb, 1024, iniFile);
            timelineIni.Cate = sb.ToString();
            _ = GetPrivateProfileString("timeline", "unauthorized", "0", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out int unauthorized);
            timelineIni.Unauthorized = unauthorized;
            ini.SetIni("timeline", timelineIni);
            _ = GetPrivateProfileString("ymyouli", "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString("ymyouli", "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            YmyouliIni ymyouliIni = new YmyouliIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString("ymyouli", "order", "random", sb, 1024, iniFile);
            ymyouliIni.Order = sb.ToString();
            _ = GetPrivateProfileString("ymyouli", "cate", "", sb, 1024, iniFile);
            ymyouliIni.Cate = sb.ToString();
            ini.SetIni("ymyouli", ymyouliIni);
            _ = GetPrivateProfileString("himawari8", "desktopperiod", "1", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString("himawari8", "lockperiod", "2", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            _ = GetPrivateProfileString("himawari8", "offset", "0.50", sb, 1024, iniFile);
            _ = float.TryParse(sb.ToString(), out float offset);
            _ = GetPrivateProfileString("himawari8", "ratio", "0.50", sb, 1024, iniFile);
            _ = float.TryParse(sb.ToString(), out float ratio);
            ini.SetIni("himawari8", new Himawari8Ini {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Offset = offset,
                Ratio = ratio
            });
            _ = GetPrivateProfileString("infinity", "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString("infinity", "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            _ = GetPrivateProfileString("infinity", "order", "random", sb, 1024, iniFile);
            ini.SetIni("infinity", new InfinityIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Order = sb.ToString()
            });
            _ = GetPrivateProfileString("one", "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString("one", "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            _ = GetPrivateProfileString("one", "order", "date", sb, 1024, iniFile);
            ini.SetIni("one", new OneIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod,
                Order = sb.ToString()
            });
            _ = GetPrivateProfileString("qingbz", "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString("qingbz", "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            QingbzIni qingbzIni = new QingbzIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString("qingbz", "order", "random", sb, 1024, iniFile);
            qingbzIni.Order = sb.ToString();
            _ = GetPrivateProfileString("qingbz", "cate", "", sb, 1024, iniFile);
            qingbzIni.Cate = sb.ToString();
            ini.SetIni("qingbz", qingbzIni);
            _ = GetPrivateProfileString("obzhi", "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString("obzhi", "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            ObzhiIni obzhiIni = new ObzhiIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString("obzhi", "order", "random", sb, 1024, iniFile);
            obzhiIni.Order = sb.ToString();
            _ = GetPrivateProfileString("obzhi", "cate", "", sb, 1024, iniFile);
            obzhiIni.Cate = sb.ToString();
            ini.SetIni("obzhi", obzhiIni);
            _ = GetPrivateProfileString("wallhere", "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString("wallhere", "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            WallhereIni wallhereIni = new WallhereIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString("wallhere", "order", "random", sb, 1024, iniFile);
            wallhereIni.Order = sb.ToString();
            _ = GetPrivateProfileString("wallhere", "cate", "", sb, 1024, iniFile);
            wallhereIni.Cate = sb.ToString();
            ini.SetIni("wallhere", wallhereIni);
            _ = GetPrivateProfileString("lsp", "desktopperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out desktopPeriod);
            _ = GetPrivateProfileString("lsp", "lockperiod", "24", sb, 1024, iniFile);
            _ = int.TryParse(sb.ToString(), out lockPeriod);
            LspIni lspIni = new LspIni {
                DesktopPeriod = desktopPeriod,
                LockPeriod = lockPeriod
            };
            _ = GetPrivateProfileString("lsp", "order", "random", sb, 1024, iniFile);
            lspIni.Order = sb.ToString();
            _ = GetPrivateProfileString("lsp", "cate", "", sb, 1024, iniFile);
            lspIni.Cate = sb.ToString();
            ini.SetIni("lsp", lspIni);
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
                    return await FileIO.ReadLinesAsync(file);
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

        public static int ClearCache(Ini ini) {
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
                return count_clear;
            } catch (Exception e) {
                LogUtil.E("ClearCache() " + e.Message);
            }
            return -1;
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
