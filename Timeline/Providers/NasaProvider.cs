using Timeline.Beans;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Windows.Data.Html;
using Timeline.Utils;
using System.Threading;

namespace Timeline.Providers {
    public class NasaProvider : BaseProvider {
        // 下一页数据索引（日期编号）（用于按需加载）
        private DateTime nextPage = DateTime.UtcNow.AddHours(-4);

        private const int PAGE_SIZE = 14;

        // https://api.nasa.gov/
        // Query Parameters
        // date: The date of the APOD image to retrieve
        // start_date: The start of a date range, when requesting date for a range of dates. Cannot be used with "date".
        // end_date: The end of the date range, when used with "start_date".
        // count: If this is specified then "count" randomly chosen images will be returned. Cannot be used with "date" or "start_date" and "end_date".
        // thumbs: Return the URL of video thumbnail. If an APOD is not a video, this parameter is ignored.
        // api_key: api.nasa.gov key for expanded usage
        private const string URL_API_PAGE = "https://api.nasa.gov/planetary/apod?api_key=DEMO_KEY&thumbs=True&start_date={0}&end_date={1}";

        private Meta ParseBean(NasaApiItem bean) {
            Meta meta = new Meta {
                Title = bean.Title,
                Story = bean.Explanation
            };
            if ("image".Equals(bean.MediaType)) {
                meta.Uhd = bean.HdUrl;
                meta.Thumb = bean.Url;
                meta.Format = bean.HdUrl.Substring(bean.HdUrl.LastIndexOf("."));
            }/* else if ("video".Equals(bean.MediaType)) { // 放弃，非直链视频地址
                meta.Video = bean.Url;
                meta.Thumb = bean.ThumbnailUrl;
            }*/
            if (!string.IsNullOrEmpty(bean.Date)) {
                //DateTime.TryParseExact(bean.Date, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
                if (DateTime.TryParse(bean.Date, out DateTime date)) { // 无效日期则该条数据会被剔除
                    meta.Date = date;
                }
                meta.SortFactor = meta.Date.Ticks;
                meta.Id = bean.MediaType + meta.Date.ToString("yyyyMMdd");
            }
            if (!string.IsNullOrEmpty(bean.Copyright)) {
                meta.Copyright = "© " + bean.Copyright.Replace("\n", "").Replace(" Music:", "");
            }

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

            nextPage = date.Ticks > 0 ? date : nextPage;
            string urlApi = string.Format(URL_API_PAGE, nextPage.AddDays(-PAGE_SIZE + 1).ToString("yyyy-MM-dd"),
                nextPage.ToString("yyyy-MM-dd"));
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                List<NasaApiItem> items = JsonConvert.DeserializeObject<List<NasaApiItem>>(jsonData);
                List<Meta> metasAdd = new List<Meta>();
                foreach (NasaApiItem item in items) {
                    metasAdd.Add(ParseBean(item));
                }
                SortMetas(metasAdd); // 按时序倒序排列
                nextPage = nextPage.AddDays(-PAGE_SIZE);
                return true;
            } catch (Exception e) {
                // 情况1：任务被取消
                // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                LogUtil.E("LoadData() " + e.Message);
            }
            return false;
        }
    }

    public class NasabjpProvider : BaseProvider {
        // 下一页数据索引（从0开始）（用于按需加载）
        private int nextPage = 0;

        private readonly List<string> pageUrls = new List<string>();

        private const string URL_API_HOST = "http://www.bjp.org.cn";
        private const string URL_API_TODAY = URL_API_HOST + "/mryt/list.shtml";
        private const string URL_API_DAY = URL_API_HOST + "/mryt/list_{0}.shtml";

        private void ParsePages(string html) {
            foreach (Match match in Regex.Matches(html, @">([\d-]+)：<.+?href=""(.+?)""")) {
                pageUrls.Add(URL_API_HOST + match.Groups[2].Value);
            }
        }

        private Meta ParseBean(string srcUrl, string htmlData) {
            Meta meta = new Meta {
                Src = srcUrl
            };
            Match match = Regex.Match(htmlData, @"contentid ?= ?""(.+?)"";");
            if (match.Success) {
                meta.Id = match.Groups[1].Value;
            }
            match = Regex.Match(htmlData, @"<img src=""(.+?(\..+?))""");
            if (match.Success) {
                meta.Uhd = URL_API_HOST + match.Groups[1].Value;
                meta.Thumb = URL_API_HOST + match.Groups[1].Value;
                meta.Format = match.Groups[2].Value;
            }
            //match = Regex.Match(htmlData, @"<source src=""(.+?(\..+?))""");
            //if (match.Success) {
            //    meta.Video = URL_API_HOST + match.Groups[1].Value;
            //    meta.Format = match.Groups[2].Value;
            //}
            match = Regex.Match(htmlData, @"<strong>([^=<]+)(<.+?>)说明");
            if (match.Success) {
                meta.Title = HtmlUtilities.ConvertToText(match.Groups[1].Value);
                meta.Copyright = HtmlUtilities.ConvertToText(match.Groups[2].Value).Trim(new char[] { '\n', ' ' });
            }
            match = Regex.Match(htmlData, @">说明：(.+?)</p>");
            if (match.Success) {
                meta.Story = HtmlUtilities.ConvertToText(match.Groups[1].Value).Trim(new char[] { '\n', ' ' }) + "（翻译：北京天文馆）";
            }
            match = Regex.Match(htmlData, @">(\d+\-\d+\-\d+)<");
            if (match.Success) {
                //DateTime.TryParseExact(match.Groups[1].Value, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
                if (DateTime.TryParse(match.Groups[1].Value, out DateTime date)) { // 无效日期则该条数据会被剔除
                    meta.Date = date;
                }
                meta.SortFactor = meta.Date.Ticks;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, DateTime date = new DateTime()) {
            if (indexFocus < metas.Count - 1) { // 现有数据未浏览完，无需加载更多
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(token, bi, date);

            if (nextPage >= pageUrls.Count) {
                string urlBjp = nextPage >= 100 ? string.Format(URL_API_DAY, (int)Math.Ceiling((nextPage + 1) / 100.0)) : URL_API_TODAY;
                try {
                    HttpClient client = new HttpClient();
                    HttpResponseMessage res = await client.GetAsync(urlBjp, token);
                    string htmlData = await res.Content.ReadAsStringAsync();
                    ParsePages(htmlData);
                } catch (Exception e) {
                    // 情况1：任务被取消
                    // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                    LogUtil.E("LoadData() " + e.Message);
                }
            }
            if (nextPage >= pageUrls.Count) {
                return metas.Count > 0;
            }

            string url = pageUrls[nextPage++];
            LogUtil.D("provider url: " + url);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(url, token);
                string htmlData = await res.Content.ReadAsStringAsync();
                List<Meta> metasAdd = new List<Meta> {
                    ParseBean(url, htmlData)
                };
                SortMetas(metasAdd); // 按时序倒序排列
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
