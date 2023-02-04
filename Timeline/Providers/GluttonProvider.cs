using Timeline.Beans;
using Timeline.Utils;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Windows.ApplicationModel.Resources;

namespace Timeline.Providers {
    public class GluttonProvider : BaseProvider {
        private const string URL_API_JOURNAL = "https://api.nguaduot.cn/glutton/journal" +
            "?client=timelinewallpaper&device={0}" +
            "&order={1}" +
            "&no={2}&date={3}&score={4:F4}&admin={5}";
        private const string URL_API_MERGE = "https://api.nguaduot.cn/glutton/merge" +
            "?client=timelinewallpaper&device={0}" +
            "&order={1}" +
            "&no={2}&date={3}&score={4:F4}&admin={5}";

        private Meta ParseBean(GluttonApiData bean, string album) {
            Meta meta = new Meta {
                Id = bean.Id,
                No = bean.No,
                Uhd = bean.ImgUrl,
                Thumb = bean.ThumbUrl,
                Title = bean.Title,
                Story = bean.Story,
                Cate = bean.CateName,
                Score = bean.Score,
                Src = bean.SrcUrl,
                Format = FileUtil.ParseFormat(bean.ImgUrl),
                IdGlobalPrefix = bean.RawProvider,
                IdGlobalSuffix = bean.RawId
            };
            if (!"merge".Equals(album)) { // journal or null
                if (bean.Phase > 0) {
                    meta.Title = string.Format(ResourceLoader.GetForCurrentView().GetString("GluttonPhase"),
                        bean.Phase, bean.Title);
                }
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

        public override async Task<bool> LoadData(CancellationToken token, Ini ai, BaseIni bi, Go go) {
            GluttonIni ini = bi as GluttonIni;
            int no = go.No;
            DateTime date = go.Date;
            float score = go.Score;
            if (GetCount() > 0) {
                if ("date".Equals(ini.Order)) {
                    no = Math.Min(no, GetMinNo());
                    date = GetMinDate() < date ? GetMinDate() : date;
                } else if ("score".Equals(ini.Order)) {
                    score = Math.Min(score, GetMinScore());
                }
            }
            string urlApi;
            if ("merge".Equals(ini.Album)) {
                urlApi = string.Format(URL_API_MERGE, SysUtil.GetDeviceId(),
                    ini.Order, no, date.ToString("yyyyMMdd"), score, go.Admin);
            } else { // journal or null
                urlApi = string.Format(URL_API_JOURNAL, SysUtil.GetDeviceId(),
                    ini.Order, no, date.ToString("yyyyMMdd"), score, go.Admin);
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
