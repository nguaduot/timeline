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

namespace Timeline.Providers {
    public class GluttonProvider : BaseProvider {
        // 页数据索引（从最大值开始）（用于按需加载）
        private int nextPage = int.MaxValue;

        private const string URL_API = "https://api.nguaduot.cn/glutton/v2?client=timelinewallpaper&album={0}&order={1}&phase={2}";
        
        private Meta ParseBean(GluttonApiData bean, string album) {
            Meta meta = new Meta {
                Id = bean.Id,
                Uhd = bean.ImgUrl,
                Title = bean.Title
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

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, int index, DateTime date = new DateTime()) {
            // 已加载过无需加载
            if (metas.Count > 0) {
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(token, bi, index, date);

            GluttonIni ini = bi as GluttonIni;
            string urlApi = string.Format(URL_API, ini.Album, ini.Order, nextPage);
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
                if ("journal".Equals(ini.Album) && !"random".Equals(ini.Order)) {
                    int phase = int.MaxValue;
                    api.Data.ForEach(item => phase = Math.Min(phase, item.Phase));
                    nextPage = phase > 0 ? phase - 1 : nextPage;
                }
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
