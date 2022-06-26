using Timeline.Beans;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Timeline.Utils;
using System.Collections.Generic;
using System.Threading;

namespace Timeline.Providers {
    public class TimelineProvider : BaseProvider {
        // 下一页数据索引（从0开始）（用于按需加载）
        private DateTime nextPage = DateTime.UtcNow.AddHours(8);

        // 自建图源
        // https://github.com/nguaduot/TimelineApi
        private const string URL_API = "https://api.nguaduot.cn/timeline/v2?client=timelinewallpaper&cate={0}&order={1}&enddate={2}&unauthorized={3}";
        
        private Meta ParseBean(TimelineApiData bean, string order) {
            Meta meta = new Meta {
                Id = bean.Id,
                Uhd = bean.ImgUrl,
                Thumb = bean.ThumbUrl,
                Title = bean.Title,
                Cate = bean.CateName,
                Story = bean.Story?.Trim(),
                Src = bean.SrcUrl
            };
            if (bean.Unauthorized != 0) {
                meta.Title = "🚫 " + meta.Title;
            }
            if (!string.IsNullOrEmpty(bean.Copyright)) {
                meta.Copyright = "@" + bean.Copyright;
            }
            if (!string.IsNullOrEmpty(bean.Platform)) {
                meta.Copyright = bean.Platform + meta.Copyright;
            }
            if (bean.ImgUrl != null) {
                Uri uri = new Uri(bean.ImgUrl);
                string[] name = uri.Segments[uri.Segments.Length - 1].Split(".");
                meta.Format = "." + name[1];
            }
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.RelDate, out DateTime date)) {
                meta.Date = date;
            }
            meta.SortFactor = "score".Equals(order) ? bean.Score : meta.Date.Ticks;
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, DateTime date = new DateTime()) {
            // 现有数据未浏览完，无需加载更多，或已无更多数据
            if (indexFocus < metas.Count - 1 && date.Ticks == 0) {
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(token, bi, date);

            TimelineIni ini = bi as TimelineIni;
            nextPage = date.Ticks > 0 ? date : nextPage;
            string urlApi = string.Format(URL_API, ini.Cate, ini.Order, nextPage.ToString("yyyyMMdd"), ini.Unauthorized);
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                TimelineApi api = JsonConvert.DeserializeObject<TimelineApi>(jsonData);
                if (api.Status != 1) {
                    return false;
                }
                List<Meta> metasAdd = new List<Meta>();
                foreach (TimelineApiData item in api.Data) {
                    metasAdd.Add(ParseBean(item, ini.Order));
                }
                if ("date".Equals(ini.Order) || "score".Equals(ini.Order)) { // 有序排列
                    SortMetas(metasAdd);
                } else {
                    AppendMetas(metasAdd);
                }
                nextPage = "date".Equals(ini.Order) ? nextPage.AddDays(-api.Data.Count) : nextPage;
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
