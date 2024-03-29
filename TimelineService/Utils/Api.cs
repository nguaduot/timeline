﻿using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using TimelineService.Beans;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.System.Profile;
using Windows.System.UserProfile;

namespace TimelineService.Utils {
    public sealed class Api {
        public static IAsyncOperation<bool> Stats(Ini ini, int dosageApp, int dosageApi, string screen) {
            return Stats_Impl(ini, dosageApp, dosageApi, screen).AsAsyncOperation();
        }

        private static async Task<bool> Stats_Impl(Ini ini, int dosageApp, int dosageApi, string screen) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            const string URL_API_STATS = "https://api.nguaduot.cn/appstats";
            StatsApiReq req = new StatsApiReq {
                App = "TimelineService",
                Package = "TWPushService.winmd",
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
                HttpResponseMessage response = await client.PostAsync(URL_API_STATS, content);
                _ = response.EnsureSuccessStatusCode();
                string jsonData = await response.Content.ReadAsStringAsync();
                LogUtil.I("Stats() " + jsonData.Trim());
                return jsonData.Contains(@"""status"":1");
            } catch (Exception e) {
                LogUtil.E("Stats() " + e.Message);
            }
            return false;
        }
    }
}
