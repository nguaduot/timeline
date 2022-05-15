using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Timeline.Beans;
using Windows.ApplicationModel;
using Windows.Services.Store;
using Windows.System.Profile;
using Windows.System.UserProfile;

namespace Timeline.Utils {
    public class Api {
        //public const string URI_STORE = "ms-windows-store://pdp/?productid=9N7VHQ989BB7";

        public static async void Stats(Ini ini, bool status) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return;
            }
            const string URL_API = "https://api.nguaduot.cn/appstats";
            StatsApiReq req = new StatsApiReq {
                App = Package.Current.DisplayName, // 不会随语言改变
                Package = Package.Current.Id.FamilyName,
                Version = VerUtil.GetPkgVer(false),
                Api = ini?.ToString(),
                Status = status ? 1 : 0,
                Os = AnalyticsInfo.VersionInfo.DeviceFamily,
                OsVersion = VerUtil.GetOsVer(),
                Device = VerUtil.GetDevice(),
                DeviceId = VerUtil.GetDeviceId(),
                Region = GlobalizationPreferences.HomeGeographicRegion
            };
            try {
                HttpClient client = new HttpClient();
                HttpContent content = new StringContent(JsonConvert.SerializeObject(req),
                    Encoding.UTF8, "application/json");
                //content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.PostAsync(URL_API, content);
                _ = response.EnsureSuccessStatusCode();
                string jsonData = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("stats: " + jsonData.Trim());
            } catch (Exception e) {
                Debug.WriteLine(e);
            }
        }

        public static async void Rank(Ini ini, Meta meta, string action, bool undo = false) {
            if (ini == null || meta == null) {
                return;
            }
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return;
            }
            Debug.WriteLine("Rank() " + action);
            const string URL_API = "https://api.nguaduot.cn/appstats/rank";
            RankApiReq req = new RankApiReq {
                Provider = ini?.Provider,
                ImgId = meta?.Id,
                ImgUrl = meta?.Uhd,
                Action = action,
                Undo = undo,
                DeviceId = VerUtil.GetDeviceId(),
                Region = GlobalizationPreferences.HomeGeographicRegion
            };
            try {
                HttpClient client = new HttpClient();
                HttpContent content = new StringContent(JsonConvert.SerializeObject(req),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(URL_API, content);
                _ = response.EnsureSuccessStatusCode();
                string jsonData = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("rank: " + jsonData.Trim());
            } catch (Exception e) {
                Debug.WriteLine(e);
            }
        }

        public static async Task<bool> Contribute(ContributeApiReq req) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            const string URL_API = "https://api.nguaduot.cn/timeline/contribute";
            req.AppVer = VerUtil.GetPkgVer(false);
            try {
                HttpClient client = new HttpClient();
                HttpContent content = new StringContent(JsonConvert.SerializeObject(req),
                    Encoding.UTF8, "application/json");
                //content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.PostAsync(URL_API, content);
                _ = response.EnsureSuccessStatusCode();
                string jsonData = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("stats: " + jsonData.Trim());
                return jsonData.Contains(@"""status"":1");
            } catch (Exception e) {
                Debug.WriteLine(e);
            }
            return false;
        }

        public static async void Crash(Exception e) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return;
            }
            const string URL_API = "https://api.nguaduot.cn/appstats/crash";
            CrashApiReq req = new CrashApiReq {
                App = Package.Current.DisplayName, // 不会随语言改变
                Package = Package.Current.Id.FamilyName,
                Version = VerUtil.GetPkgVer(false),
                Os = AnalyticsInfo.VersionInfo.DeviceFamily,
                OsVersion = VerUtil.GetOsVer(),
                Device = VerUtil.GetDevice(),
                DeviceId = VerUtil.GetDeviceId(),
                Exception = e.ToString()
            };
            try {
                HttpClient client = new HttpClient();
                HttpContent content = new StringContent(JsonConvert.SerializeObject(req),
                    Encoding.UTF8, "application/json");
                //content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.PostAsync(URL_API, content);
                _ = response.EnsureSuccessStatusCode();
                string jsonData = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("crash: " + jsonData.Trim());
            } catch (Exception ex) {
                Debug.WriteLine(ex);
            }
        }

        public static async Task<ReleaseApi> CheckUpdate() {
            if ("fp51msqsmzpvr".Equals(Package.Current.Id.PublisherId)) {
                return await CheckUpdateFromStore();
            }
            return await CheckUpdateFromGitee();
        }

        public static async Task<ReleaseApi> CheckUpdateFromStore() {
            ReleaseApi res = new ReleaseApi();
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return res;
            }
            try {
                StoreContext context = StoreContext.GetDefault();
                IReadOnlyList<StorePackageUpdate> updates = await context.GetAppAndOptionalStorePackageUpdatesAsync();
                if (updates.Count > 0) {
                    res.Url = "ms-windows-store://pdp/?productid=9N7VHQ989BB7";
                }
            } catch (Exception e) {
                Debug.WriteLine(e);
            }
            return res;
        }

        public static async Task<ReleaseApi> CheckUpdateFromGitee() {
            ReleaseApi res = new ReleaseApi();
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return res;
            }
            const string URL_RELEASE = "https://gitee.com/api/v5/repos/nguaduot/timeline/releases/latest";
            try {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("timelinewallpaper", VerUtil.GetPkgVer(true)));
                string jsonData = await client.GetStringAsync(URL_RELEASE);
                Debug.WriteLine("release: " + jsonData.Trim());
                GiteeApi api = JsonConvert.DeserializeObject<GiteeApi>(jsonData);
                if (api.Prerelease) { // 忽略预览版本
                    return res;
                }
                string[] versions = api.TagName.Split(".");
                if (versions.Length < 2) {
                    return res;
                }
                int major = Package.Current.Id.Version.Major;
                int minor = Package.Current.Id.Version.Minor;
                _ = int.TryParse(versions[0], out int majorNew);
                _ = int.TryParse(versions[1], out int minorNew);
                if (majorNew > major || (majorNew == major && minorNew > minor)) {
                    res.Version = " v" + majorNew + "." + minorNew;
                    res.Url = string.Format("https://gitee.com/nguaduot/timeline/releases/{0}", api.TagName);
                }
            } catch (Exception e) {
                Debug.WriteLine(e);
            }
            return res;
        }
    }
}
