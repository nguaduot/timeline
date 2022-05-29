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

        public static async Task StatsAsync(Ini ini, bool status) {
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
                LogUtil.D("Stats() " + jsonData.Trim());
            } catch (Exception e) {
                LogUtil.E("Stats() " + e.Message);
            }
        }

        public static async Task RankAsync(string provider, Meta meta, string action, string target = null, bool undo = false) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return;
            }
            LogUtil.D("Rank() " + action);
            const string URL_API = "https://api.nguaduot.cn/appstats/rank";
            RankApiReq req = new RankApiReq {
                Provider = provider,
                ImgId = meta?.Id,
                ImgUrl = meta?.Uhd,
                Action = action,
                Target = target,
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
                LogUtil.D("Rank() " + jsonData.Trim());
            } catch (Exception e) {
                LogUtil.E("Rank() " + e.Message);
            }
        }

        public static async Task<bool> ContributeAsync(ContributeApiReq req) {
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
                LogUtil.D("Contribute() " + jsonData.Trim());
                return jsonData.Contains(@"""status"":1");
            } catch (Exception e) {
                LogUtil.E("Contribute() " + e.Message);
            }
            return false;
        }

        public static async Task CrashAsync(Exception e) {
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
                LogUtil.D("Crash() " + jsonData.Trim());
            } catch (Exception ex) {
                LogUtil.E("Crash() " + ex.Message);
            }
        }

        public static async Task<ReleaseApi> CheckUpdateAsync() {
            ReleaseApi res = new ReleaseApi();
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return res;
            }
            const string URL_VERSION = "https://api.nguaduot.cn/appstats/version?pkg={0}";
            string urlApi = string.Format(URL_VERSION, Uri.EscapeUriString(Package.Current.Id.FamilyName));
            try {
                HttpClient client = new HttpClient();
                string jsonData = await client.GetStringAsync(urlApi);
                LogUtil.D("CheckUpdateAsync() " + jsonData.Trim());
                AppstatsApi api = JsonConvert.DeserializeObject<AppstatsApi>(jsonData);
                if (api.Status != 1) {
                    return res;
                }
                string[] versions = api.Data.Version.Split(".");
                if (versions.Length < 2) {
                    return res;
                }
                int major = Package.Current.Id.Version.Major;
                int minor = Package.Current.Id.Version.Minor;
                _ = int.TryParse(versions[0], out int majorNew);
                _ = int.TryParse(versions[1], out int minorNew);
                if (majorNew > major || (majorNew == major && minorNew > minor)) {
                    res.Version = " v" + majorNew + "." + minorNew;
                    res.Url = api.Data.Url;
                }
            } catch (Exception e) {
                LogUtil.E("CheckUpdateAsync() " + e.Message);
            }
            return res;
        }
    }
}
