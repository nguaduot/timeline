using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Timeline.Beans;
using Windows.ApplicationModel;
using Windows.System.Profile;
using Windows.System.UserProfile;

namespace Timeline.Utils {
    public class Api {
        public static async Task StatsAsync(Ini ini, int dosageApp, int dosageApi) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return;
            }
            const string URL_API = "https://api.nguaduot.cn/appstats";
            Windows.Foundation.Size resolution = SysUtil.GetMonitorPixels(false); // 分辨率
            double scaleFactor = SysUtil.GetMonitorScale(); // 缩放因子
            double diagonalInch = SysUtil.GetMonitorDiagonal(); // 屏幕物理大小（吋）
            string screen = string.Format("{0}x{1},{2},{3}", (int)resolution.Width, (int)resolution.Height,
                scaleFactor.ToString("0.00"), diagonalInch.ToString("0.0"));
            StatsApiReq req = new StatsApiReq {
                App = Package.Current.DisplayName, // 不会随语言改变
                Package = Package.Current.Id.FamilyName,
                Version = SysUtil.GetPkgVer(false),
                Architecture = Package.Current.Id.Architecture.ToString(),
                Api = ini?.ToString(),
                DosageApp = dosageApp,
                DosageApi = dosageApi,
                Pc = SysUtil.GetPcType(),
                Os = AnalyticsInfo.VersionInfo.DeviceFamily,
                OsVersion = SysUtil.GetOsVer(),
                Screen = screen,
                Device = SysUtil.GetDevice(),
                DeviceName = SysUtil.GetDeviceName(),
                DeviceId = SysUtil.GetDeviceId(),
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
            LogUtil.D("RankAsync() " + action);
            const string URL_API = "https://api.nguaduot.cn/appstats/rank";
            // 解决合集图源动作拆分
            string providerFix = !string.IsNullOrEmpty(meta?.IdGlobalPrefix) ? meta?.IdGlobalPrefix : provider;
            string imgIdFix = !string.IsNullOrEmpty(meta?.IdGlobalSuffix) ? meta?.IdGlobalSuffix : meta?.Id;
            RankApiReq req = new RankApiReq {
                Provider = providerFix,
                ImgId = imgIdFix,
                ImgUrl = meta?.Uhd,
                Action = action,
                Target = target,
                Undo = undo,
                Version = SysUtil.GetPkgVer(false),
                DeviceId = SysUtil.GetDeviceId(),
                Region = GlobalizationPreferences.HomeGeographicRegion
            };
            try {
                HttpClient client = new HttpClient();
                HttpContent content = new StringContent(JsonConvert.SerializeObject(req),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(URL_API, content);
                _ = response.EnsureSuccessStatusCode();
                string jsonData = await response.Content.ReadAsStringAsync();
                LogUtil.D("RankAsync() " + jsonData.Trim());
            } catch (Exception e) {
                LogUtil.E("RankAsync() " + e.Message);
            }
        }

        public static async Task<ReleaseApiData> VersionAsync() {
            ReleaseApiData res = new ReleaseApiData();
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return res;
            }
            const string URL_VERSION = "https://api.nguaduot.cn/appstats/version?pkg={0}";
            string urlApi = string.Format(URL_VERSION, Uri.EscapeUriString(Package.Current.Id.FamilyName));
            try {
                HttpClient client = new HttpClient();
                string jsonData = await client.GetStringAsync(urlApi);
                //LogUtil.D("CheckUpdateAsync() " + jsonData.Trim());
                ReleaseApi api = JsonConvert.DeserializeObject<ReleaseApi>(jsonData);
                if (api.Status != 1) {
                    return res;
                }
                return api.Data;
            } catch (Exception e) {
                LogUtil.E("VersionAsync() " + e.Message);
            }
            return res;
        }

        public static async Task CrashAsync(Exception e) {
            if (e == null || !NetworkInterface.GetIsNetworkAvailable()) {
                return;
            }
            const string URL_API = "https://api.nguaduot.cn/appstats/crash";
            CrashApiReq req = new CrashApiReq {
                App = Package.Current.DisplayName, // 不会随语言改变
                Package = Package.Current.Id.FamilyName,
                Version = SysUtil.GetPkgVer(false),
                Os = AnalyticsInfo.VersionInfo.DeviceFamily,
                OsVersion = SysUtil.GetOsVer(),
                Device = SysUtil.GetDevice(),
                DeviceId = SysUtil.GetDeviceId(),
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

        public static async Task<List<CateApiData>> CateAsync(string urlApi) {
            List<CateApiData> data = new List<CateApiData>();
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return data;
            }
            if (string.IsNullOrEmpty(urlApi)) {
                return data;
            }
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi);
                string jsonData = await res.Content.ReadAsStringAsync();
                LogUtil.D("CateAsync(): " + jsonData.Trim());
                CateApi api = JsonConvert.DeserializeObject<CateApi>(jsonData);
                if (api.Data != null) {
                    data = api.Data;
                    data.Sort((a, b) => b.Score.CompareTo(a.Score));
                }
            } catch (Exception e) {
                LogUtil.E("CateAsync() " + e.Message);
            }
            return data;
        }

        public static async Task<R22AuthApiData> LspR22AuthAsync(string comment = null) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return new R22AuthApiData();
            }
            const string URL_API = "https://api.nguaduot.cn/lsp/auth?deviceid={0}&comment={1}";
            string urlApi = string.Format(URL_API, SysUtil.GetDeviceId(), comment ?? "");
            try {
                HttpClient client = new HttpClient();
                string jsonData = await client.GetStringAsync(urlApi);
                LogUtil.D("LspR22AuthAsync(): " + jsonData.Trim());
                R22AuthApi api = JsonConvert.DeserializeObject<R22AuthApi>(jsonData);
                return api.Data ?? new R22AuthApiData();
            } catch (Exception e) {
                LogUtil.E("LspR22AuthAsync() " + e.Message);
            }
            return new R22AuthApiData();
        }
    }
}
