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

namespace Timeline.Providers {
    public class GluttonProvider : BaseProvider {
        private const string URL_API = "https://api.nguaduot.cn/glutton/v2?client=timelinewallpaper&order={0}";
        
        private Meta ParseBean(GluttonApiData bean) {
            Meta meta = new Meta {
                Id = bean.Id,
                Uhd = bean.ImgUrl,
                Title = bean.Title,
                SortFactor = bean.Score
            };
            if (!string.IsNullOrEmpty(bean.Copyright)) {
                meta.Copyright = "© " + bean.Copyright;
            }
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.RelDate, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, DateTime date = new DateTime()) {
            // 已加载过无需加载
            if (metas.Count > 0) {
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(token, bi, date);

            string urlApi = string.Format(URL_API, bi.Order);
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
                    metasAdd.Add(ParseBean(item));
                }
                if ("score".Equals(bi.Order)) { // 有序排列
                    SortMetas(metasAdd);
                } else {
                    AppendMetas(metasAdd);
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
