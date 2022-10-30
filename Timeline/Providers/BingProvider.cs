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
        private const string URL_API = URL_API_HOST + "/HPImageArchive.aspx?pid=hp&format=js&uhd=1&idx={0}&n=8";

        private Meta ParseBean(BingApiImg bean, string lang) {
            // 基础字段：title、copyright（ko-kr 等未支持地区 title 为“Info”）
            // 扩展字段：caption、desc（en-us 等支持地区，ja-jp 地区 caption 与 title 相同）
            // copyright：
            //   zh-cn：在新湾潜水的南露脊鲸，阿根廷瓦尔德斯半岛 (© Gabriel Rojo/Minden Pictures)
            //   en-us：Infini-D, modeled during the World of WearableArt Awards in 2019 in Wellington, New Zealand (© Hagen Hopkins/Getty Images for World of WearableArt)
            //   ja-jp：世界ウェアラブルアートショー, ニュージーランド ウェリントン (© Hagen Hopkins/Getty Images for World of WearableArt)
            //   ko-kr：Johnston Canyon, Banff National Park, Canada (© Jason Hatfield/TANDEM Stills + Motion)
            string title = !"Info".Equals(bean.Title) ? bean.Title : "";
            string copyrightTitle = bean.Copyright;
            string copyright = "";
            Match match = Regex.Match(bean.Copyright, @"(.+)\(©(.+)\)");
            if (match.Success) {
                copyrightTitle = match.Groups[1].Value.Trim();
                copyright = match.Groups[2].Value.Trim();
            }
            string caption = !string.IsNullOrEmpty(bean.Caption) && bean.Caption.Equals(title) ? "" : bean.Caption;
            string desc = bean.Desc;

            bool cn = "zh-cn".Equals(lang) || (string.IsNullOrEmpty(lang) && "CN".Equals(GlobalizationPreferences.HomeGeographicRegion));
            Meta meta = new Meta {
                Id = bean.Hsh,
                Uhd = string.Format("{0}{1}_UHD.jpg", cn ? URL_CND_HOST : URL_API_HOST, bean.UrlBase),
                Thumb = string.Format("{0}{1}_400x240.jpg", cn ? URL_CND_HOST : URL_API_HOST, bean.UrlBase),
                Copyright = !string.IsNullOrEmpty(copyright) ? "© " + copyright : ""
            };
            if (!string.IsNullOrEmpty(title)) {
                meta.Title = title;
                if (!string.IsNullOrEmpty(caption)) {
                    meta.Caption = caption;
                    if (!string.IsNullOrEmpty(copyrightTitle)) {
                        if (!string.IsNullOrEmpty(desc)) {
                            meta.Story = copyrightTitle + "\n" + desc;
                        } else {
                            meta.Story = copyrightTitle;
                        }
                    } else {
                        meta.Story = desc;
                    }
                } else if (!string.IsNullOrEmpty(copyrightTitle)) {
                    meta.Caption = copyrightTitle;
                    meta.Story = desc;
                } else {
                    meta.Story = desc;
                }
            } else if (!string.IsNullOrEmpty(caption)) {
                meta.Title = caption;
                if (!string.IsNullOrEmpty(copyrightTitle)) {
                    meta.Caption = copyrightTitle;
                    meta.Story = desc;
                } else {
                    meta.Story = desc;
                }
            } else if (!string.IsNullOrEmpty(copyrightTitle)) {
                meta.Title = copyrightTitle;
                meta.Story = desc;
            }

            //Match match = Regex.Match(meta.Caption, @"(.+)\(©(.+)\)");
            //if (match.Success) {
            //    meta.Caption = match.Groups[1].Value.Trim();
            //    meta.Copyright = "© " + match.Groups[2].Value.Trim();
            //    match = Regex.Match(meta.Caption, @"｢(.+)｣(.+)");
            //    if (match.Success) { // 国内版（日本）
            //        meta.Caption = match.Groups[1].Value.Trim();
            //        meta.Location = match.Groups[2].Value.Trim();
            //    } else { // 国内版（中国）
            //        match = Regex.Match(meta.Caption, @"(.+)[，](.+)");
            //        if (match.Success) {
            //            meta.Caption = match.Groups[1].Value.Trim();
            //            meta.Location = match.Groups[2].Value.Trim();
            //        }
            //    }
            //}

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

            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, Ini ai, BaseIni bi, Go go) {
            BingIni ini = bi as BingIni;
            string urlApi = string.Format(URL_API, GetMaxIndex() + 1);
            if (ini.Lang.Length > 0) {
                urlApi += "&setmkt=" + ini.Lang;
            }
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                BingApi api = JsonConvert.DeserializeObject<BingApi>(jsonData);
                List<Meta> metasAdd = new List<Meta>();
                foreach (BingApiImg img in api.Images) {
                    metasAdd.Add(ParseBean(img, ini.Lang));
                }
                AppendMetas(metasAdd); // 倒序时间
                return true;
            } catch (Exception e) {
                // TODO Object reference not set to an instance of an object.
                // 情况1：任务被取消
                // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                LogUtil.E("LoadData() " + e.Message);
            }
            return false;
        }
    }
}
