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
                meta.Date = DateTime.ParseExact(bean.Date, "yyyy-MM-dd", new System.Globalization.CultureInfo("en-US"));
                meta.SortFactor = meta.Date.Value.Subtract(new DateTime(1970, 1, 1)).TotalDays;
                meta.Id = bean.MediaType + meta.Date?.ToString("yyyyMMdd");
            }
            if (!string.IsNullOrEmpty(bean.Copyright)) {
                meta.Copyright = "© " + bean.Copyright.Replace("\n", "").Replace(" Music:", "");
            }

            return meta;
        }

        public override async Task<bool> LoadData(BaseIni ini, DateTime? date = null) {
            // 现有数据未浏览完，无需加载更多，或已无更多数据
            if (indexFocus < metas.Count - 1 && date == null) {
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(ini, date);

            nextPage = date ?? nextPage;
            string urlApi = string.Format(URL_API_PAGE, nextPage.AddDays(-PAGE_SIZE + 1).ToString("yyyy-MM-dd"),
                nextPage.ToString("yyyy-MM-dd"));
            Debug.WriteLine("provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                string jsonData = await client.GetStringAsync(urlApi);
                Debug.WriteLine("provider data: " + jsonData.Trim());
                List<NasaApiItem> items = JsonConvert.DeserializeObject<List<NasaApiItem>>(jsonData);
                List<Meta> metasAdd = new List<Meta>();
                foreach (NasaApiItem item in items) {
                    metasAdd.Add(ParseBean(item));
                }
                SortMetas(metasAdd); // 按时序倒序排列
                nextPage = nextPage.AddDays(-PAGE_SIZE);
            } catch (Exception e) {
                Debug.WriteLine(e);
            }
            return metas.Count > 0;
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

        private Meta ParseBean(string htmlData) {
            Meta meta = new Meta();
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
                meta.Date = DateTime.ParseExact(match.Groups[1].Value, "yyyy-MM-dd", new System.Globalization.CultureInfo("en-US"));
                meta.SortFactor = meta.Date.Value.Subtract(new DateTime(1970, 1, 1)).TotalDays;
            }
            return meta;
        }

        public override async Task<bool> LoadData(BaseIni ini, DateTime? date = null) {
            // 现有数据未浏览完，无需加载更多，或已无更多数据
            if (indexFocus < metas.Count - 1) {
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(ini, date);

            if (nextPage >= pageUrls.Count) {
                string urlBjp = nextPage >= 100 ? string.Format(URL_API_DAY, (int)Math.Ceiling((nextPage + 1) / 100.0)) : URL_API_TODAY;
                try {
                    HttpClient client = new HttpClient();
                    string htmlData = await client.GetStringAsync(urlBjp);
                    ParsePages(htmlData);
                } catch (Exception e) {
                    Debug.WriteLine(e);
                }
            }
            if (nextPage >= pageUrls.Count) {
                return metas.Count > 0;
            }

            string url = pageUrls[nextPage++];
            Debug.WriteLine("provider url: " + url);
            try {
                HttpClient client = new HttpClient();
                string htmlData = await client.GetStringAsync(url);
                List<Meta> metasAdd = new List<Meta> {
                    ParseBean(htmlData)
                };
                AppendMetas(metasAdd);
            } catch (Exception e) {
                Debug.WriteLine(e);
            }

            return metas.Count > 0;
        }
    }
}
