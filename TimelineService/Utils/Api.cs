using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using TimelineService.Beans;
using Windows.Foundation;
using Windows.System.Profile;
using Windows.System.UserProfile;

namespace TimelineService.Utils {
    public sealed class Api {
        public static IAsyncOperation<bool> Stats(Ini ini, bool status) {
            return Stats_Impl(ini, status).AsAsyncOperation();
        }

        private static async Task<bool> Stats_Impl(Ini ini, bool status) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            const string URL_API_STATS = "https://api.nguaduot.cn/appstats";
            StatsApiReq req = new StatsApiReq {
                App = "拾光推送服务",
                Package = "TWPushService.winmd",
                Version = "5.0", // TODO
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
                HttpResponseMessage response = await client.PostAsync(URL_API_STATS, content);
                _ = response.EnsureSuccessStatusCode();
                string jsonData = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("stats2: " + jsonData.Trim());
                return jsonData.Contains(@"""status"":1");
            } catch (Exception e) {
                Debug.WriteLine(e);
            }
            return false;
        }
    }
}
