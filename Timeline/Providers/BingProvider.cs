using Timeline.Beans;
using Timeline.Utils;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;
using System.Web;
using Windows.System.UserProfile;

namespace Timeline.Providers {
    public class BingProvider : BaseProvider {
        // 页数据索引（从0开始）（用于按需加载）
        private int pageIndex = 0;

        private const int PAGE_SIZE = 8;

        private const string URL_API_HOST = "https://global.bing.com";
        private const string URL_CND_HOST = "http://s.cn.bing.net";
        // Bing搜索 官方提供的 API
        // GET 参数：
        // pid: hp（缺省会导致数据不全）
        // format：hp-HTML，js-JSON，xml/其他值-XML（默认）
        // ensearch：国际版置1，国内版置0（默认）
        // setmkt: HOST为“global.bing.com”时生效，zh-cn、en-us、ja-jp、de-de、fr-fr
        // idx：回溯天数，0为当天，-1为明天，1为昨天，依次类推（最大值7）
        // n：返回数据的条数。若为2，会包含前一天的数据，依次类推（最大值8）
        // uhd：0-1920x1080，1-uhdwidth/uhdheight生效
        // uhdwidth：uhd置1时生效，最大值3840
        // uhdheight：uhd置1时生效
        // API 第三方文档：http://www.lib4dev.in/info/facefruit/daily-bing-wallpaper/209719167
        // 语言代码表：http://www.lingoes.net/zh/translator/langcode.htm
        private readonly string[] URL_API_PAGES = new string[] {
            URL_API_HOST + "/HPImageArchive.aspx?pid=hp&format=js&uhd=1&idx=0&n=" + PAGE_SIZE,
            URL_API_HOST + "/HPImageArchive.aspx?pid=hp&format=js&uhd=1&idx=7&n=" + PAGE_SIZE
        };

        private Meta ParseBean(BingApiImg bean, string lang) {
            bool cn = "zh-cn".Equals(lang) || (string.IsNullOrEmpty(lang) && "CN".Equals(GlobalizationPreferences.HomeGeographicRegion));
            Meta meta = new Meta {
                Id = bean.Hsh,
                Uhd = string.Format("{0}{1}_UHD.jpg", cn ? URL_CND_HOST : URL_API_HOST, bean.UrlBase),
                Thumb = string.Format("{0}{1}_400x240.jpg", cn ? URL_CND_HOST : URL_API_HOST, bean.UrlBase),
                Title = !"Info".Equals(bean.Title) ? bean.Title : "", // ko-kr等未支持地区为“Info”
                Story = bean.Desc, // 部分区域无该字段
                Caption = bean.Copyright
            };
            // zh-cn: 正爬上唐娜·诺克沙滩的灰海豹，英格兰北林肯郡 (© Frederic Desmette/Minden Pictures)
            // en-us: Aerial view of the island of Mainau on Lake Constance, Germany (© Amazing Aerial Agency/Offset by Shutterstock)
            // ja-jp: ｢ドナヌックのハイイロアザラシ｣英国, ノースリンカーンシャー (© Frederic Desmette/Minden Pictures)
            Match match = Regex.Match(meta.Caption, @"(.+)[\(（]©(.+)[\)）]");
            if (match.Success) {
                meta.Caption = match.Groups[1].Value.Trim();
                meta.Copyright = "© " + match.Groups[2].Value.Trim();
                match = Regex.Match(meta.Caption, @"｢(.+)｣(.+)");
                if (match.Success) { // 国内版（日本）
                    meta.Caption = match.Groups[1].Value.Trim();
                    meta.Location = match.Groups[2].Value.Trim();
                } else { // 国内版（中国）
                    match = Regex.Match(meta.Caption, @"(.+)[，](.+)");
                    if (match.Success) {
                        meta.Caption = match.Groups[1].Value.Trim();
                        meta.Location = match.Groups[2].Value.Trim();
                    }
                }
            }

            if (!bean.CopyrightLink.Contains("filters=HpDate") && DateTime.TryParseExact(bean.FullStartDate, "yyyyMMddHHmm", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime time)) {
                meta.Src = string.Format("{0}{1}&filters={2}", URL_API_HOST, bean.CopyrightLink,
                    HttpUtility.UrlEncode(string.Format("HpDate:\"{0}\"", time.ToString("yyyyMMdd_HHmm"))));
            } else {
                meta.Src = URL_API_HOST + bean.CopyrightLink;
            }
            meta.Src += "&ensearch=" + (cn ? 0 : 1);
            
            if (DateTime.TryParseExact(bean.EndDate, "yyyyMMdd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date)) {
                meta.Date = date;
            }
            meta.SortFactor = meta.Date.Ticks;

            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, int index, DateTime date = new DateTime()) {
            if (pageIndex >= URL_API_PAGES.Length) { // 已无更多数据
                return true;
            }
            if (date.Ticks > 0) {
                if (metas.Count > 0 && date.Date > metas[metas.Count - 1].Date) {
                    return true;
                }
            } else if (index < metas.Count) { // 现有数据未浏览完，无需加载更多
                return true;
            }
            if (!NetworkInterface.GetIsNetworkAvailable()) { // 无网络连接
                return false;
            }
            await base.LoadData(token, bi, index, date);

            BingIni ini = bi as BingIni;
            string urlApi = URL_API_PAGES[pageIndex];
            if (ini.Lang.Length > 0) {
                urlApi += "&setmkt=" + ini.Lang;
            }
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                BingApi api = JsonConvert.DeserializeObject<BingApi>(jsonData);
                List<Meta> metasAdd = new List<Meta>();
                foreach (BingApiImg img in api.Images) {
                    metasAdd.Add(ParseBean(img, ini.Lang));
                }
                SortMetas(metasAdd); // 按时序倒序排列
                pageIndex += 1;
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
