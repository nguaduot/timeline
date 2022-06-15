using Timeline.Beans;
using Timeline.Utils;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Globalization;

namespace Timeline.Providers {
    public class OneplusProvider : BaseProvider {
        // 页数据索引（从1开始）（用于按需加载）
        private int pageIndex = 0;

        private const int PAGE_SIZE = 99;

        private const string URL_API = "https://photos.oneplus.com/cn/shot/photo/schedule";

        private Meta ParseBean(OneplusApiItem bean) {
            Meta meta = new Meta {
                Id = bean.PhotoCode,
                Uhd = bean.PhotoUrl,
                Thumb = bean.PhotoUrl.Replace(".jpg", "_400_0.jpg"),
                Title = bean.PhotoTopic?.Trim(),
                Copyright = "@" + bean.Author
            };

            if (!bean.PhotoTopic.Equals(bean.Remark?.Trim())) {
                meta.Caption = bean.Remark?.Trim();
            }
            if (!bean.PhotoTopic.Equals(bean.PhotoLocation?.Trim())) {
                meta.Location = bean.PhotoLocation?.Trim();
            }
            if (!string.IsNullOrEmpty(bean.CountryCodeStr)) {
                meta.Copyright += " | " + bean.CountryCodeStr;
            }
            if (DateTime.TryParseExact(bean.ScheduleTime, "yyyyMMdd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date)) {
                meta.Date = date;
            }
            meta.SortFactor = meta.Date.Ticks;
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, DateTime date = new DateTime()) {
            if (date.Ticks > 0) {
                if (metas.Count > 0 && date.Date > metas[metas.Count - 1].Date) {
                    return true;
                }
            } else if (indexFocus < metas.Count - 1) { // 现有数据未浏览完，无需加载更多
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(token, bi, date);

            // "1"：最新添加，"2"：点赞最多，"3"：浏览最多
            string sort = "score".Equals(bi.Order) ? "2" : ("view".Equals(bi.Order) ? "3" : "1");
            OneplusRequest request = new OneplusRequest {
                PageSize = PAGE_SIZE, // 不限
                CurrentPage = ++pageIndex,
                SortMethod = sort
            };
            string requestStr = JsonConvert.SerializeObject(request);
            LogUtil.D("LoadData() provider url: " + URL_API + " " + requestStr);
            try {
                HttpClient client = new HttpClient();
                HttpRequestMessage msgReq = new HttpRequestMessage(HttpMethod.Post, URL_API);
                //msgReq.Headers.Add("Cookie", "LOCALE=zh_CN; Path=/");
                msgReq.Content = new StringContent(requestStr, Encoding.UTF8, "application/json");
                HttpResponseMessage msgRes = await client.SendAsync(msgReq, token);
                _ = msgRes.EnsureSuccessStatusCode();
                string jsonData = await msgRes.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                OneplusApi api = JsonConvert.DeserializeObject<OneplusApi>(jsonData);
                List<Meta> metasAdd = new List<Meta>();
                foreach (OneplusApiItem item in api.Items) {
                    metasAdd.Add(ParseBean(item));
                }
                if ("date".Equals(bi.Order)) { // 按时序倒序排列
                    SortMetas(metasAdd);
                } else {
                    RandomMetas(metasAdd);
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
