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
    public class QingbzProvider : BaseProvider {
        // 页数据索引（从1开始）（用于按需加载）
        private int pageIndex = 0;

        private const string URL_API = "https://api.nguaduot.cn/qingbz?client=timelinewallpaper&cate={0}&order={1}&r18={2}&page={3}";
        
        private Meta ParseBean(QingbzApiData bean, string order) {
            Meta meta = new Meta {
                Id = bean.ImgId.ToString(),
                Uhd = bean.ImgUrl,
                Thumb = bean.ThumbUrl,
                Cate = bean.CateAlt,
                Date = DateTime.ParseExact(bean.RelDate, "yyyy-MM-dd", new System.Globalization.CultureInfo("en-US")),
                SortFactor = "score".Equals(order) ? bean.Score : bean.ImgId
            };
            meta.Title = string.Format("{0} #{1}", bean.CateAlt, bean.CateAltNo);
            if (bean.R18 == 1) {
                meta.Title = "🚫 " + meta.Title;
            }
            if (!string.IsNullOrEmpty(bean.Copyright)) {
                meta.Copyright = "© " + bean.Copyright;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni ini, DateTime? date = null) {
            // 现有数据未浏览完，无需加载更多
            if (indexFocus < metas.Count - 1) {
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(token, ini, date);

            string urlApi = string.Format(URL_API, ((QingbzIni)ini).Cate, ((QingbzIni)ini).Order,
                ((QingbzIni)ini).R18, ++pageIndex);
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                QingbzApi api = JsonConvert.DeserializeObject<QingbzApi>(jsonData);
                List<Meta> metasAdd = new List<Meta>();
                foreach (QingbzApiData item in api.Data) {
                    metasAdd.Add(ParseBean(item, ((QingbzIni)ini).Order));
                }
                if ("date".Equals(((QingbzIni)ini).Order) || "score".Equals(((QingbzIni)ini).Order)) { // 有序排列
                    SortMetas(metasAdd);
                } else {
                    AppendMetas(metasAdd);
                }
            } catch (Exception e) {
                // 情况1：任务被取消
                // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                LogUtil.E("LoadData() " + e.Message);
            }

            return metas.Count > 0;
        }
    }
}
