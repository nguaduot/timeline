using Timeline.Beans;
using Timeline.Utils;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Globalization;

namespace Timeline.Providers {
    public class OneplusProvider : BaseProvider {
        private const int PAGE_SIZE = 99;

        private const string URL_API = "https://photos.oneplus.com/cn/shot/photo/schedule";

        private Meta ParseBean(OneplusApiItem bean) {
            Meta meta = new Meta {
                Id = bean.PhotoCode,
                Uhd = bean.PhotoUrl,
                Thumb = bean.PhotoUrl.Replace(".jpg", "_400_0.jpg"),
                Title = (bean.PhotoTopic ?? "").Trim(),
                Story = (bean.Remark ?? "").Trim(),
                Copyright = "@" + bean.Author,
                Format = FileUtil.ParseFormat(bean.PhotoUrl)
            };

            if (!string.IsNullOrEmpty(bean.PhotoLocation) && !bean.PhotoLocation.Trim().Equals(meta.Title)) {
                meta.Caption = bean.PhotoLocation.Trim();
            }
            if (!string.IsNullOrEmpty(bean.CountryCodeStr)) {
                meta.Copyright += " | " + bean.CountryCodeStr;
            }
            if (DateTime.TryParseExact(bean.ScheduleTime, "yyyyMMdd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, Go go) {
            // "1"：最新添加，"2"：点赞最多，"3"：浏览最多
            string sort = "score".Equals(bi.Order) ? "2" : ("view".Equals(bi.Order) ? "3" : "1");
            int page = (int)Math.Ceiling(GetCount() * 1.0 / PAGE_SIZE) + 1;
            OneplusRequest request = new OneplusRequest {
                PageSize = PAGE_SIZE, // 实际无限制
                CurrentPage = page, // 从1开始
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
