using Timeline.Beans;
using Timeline.Utils;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;

namespace Timeline.Providers {
    public class GluttonProvider : BaseProvider {
        private const string URL_API_RANK = "https://api.nguaduot.cn/glutton/rank" +
            "?client=timelinewallpaper&device={0}" +
            "&order={1}" +
            "&no={2}&score={3}&admin={4}";
        private const string URL_API_JOURNAL = "https://api.nguaduot.cn/glutton/journal" +
            "?client=timelinewallpaper&device={0}" +
            "&order={1}" +
            "&no={2}&date={3}&score={4}&admin={5}";

        private Meta ParseBean(GluttonApiData bean, string album) {
            Meta meta = new Meta {
                Id = bean.Id,
                No = bean.No,
                Uhd = bean.ImgUrl,
                Title = bean.Title,
                Score = bean.Score,
                Format = FileUtil.ParseFormat(bean.ImgUrl)
            };
            if ("journal".Equals(album)) {
                if (bean.Phase > 0) {
                    meta.Title = string.Format(ResourceLoader.GetForCurrentView().GetString("GluttonPhase"),
                        bean.Phase, bean.Title);
                }
            } else { // rank or null
                meta.Title = ResourceLoader.GetForCurrentView().GetString("Album_rank") + " " + bean.Title;
            }
            if (!string.IsNullOrEmpty(bean.Copyright)) {
                meta.Copyright = "© " + bean.Copyright;
            }
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.RelDate, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, Go go) {
            GluttonIni ini = bi as GluttonIni;
            int no = go.No;
            DateTime date = go.Date.Ticks > 0 ? go.Date : DateTime.Now;
            float score = go.Score;
            if ("date".Equals(ini.Order)) {
                no = Math.Min(no, GetMinNo());
                date = GetMinDate() < date ? GetMinDate() : date;
                score = Math.Min(score, GetMinScore());
            } else if ("score".Equals(ini.Order)) {
                score = Math.Min(score, GetMinScore());
            }
            string urlApi;
            if ("journal".Equals(ini.Album)) {
                urlApi = string.Format(URL_API_JOURNAL, SysUtil.GetDeviceId(),
                    ini.Order, no, date.ToString("yyyyMMdd"), score, go.Admin);
            } else {
                urlApi = string.Format(URL_API_RANK, SysUtil.GetDeviceId(),
                    ini.Order, no, score);
            }
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                GluttonApi api = JsonConvert.DeserializeObject<GluttonApi>(jsonData);
                if (api.Status != 1) {
                    return false;
                }
                List<Meta> metasAdd = new List<Meta>();
                foreach (GluttonApiData item in api.Data) {
                    metasAdd.Add(ParseBean(item, ini.Album));
                }
                AppendMetas(metasAdd);
                return true;
            } catch (Exception e) {
                // 情况1：任务被取消
                // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                LogUtil.E("LoadData() " + e.Message);
            }
            return false;
        }
    }
}
