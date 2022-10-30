using Timeline.Beans;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Timeline.Utils;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;
using System.Linq;

namespace Timeline.Providers {
    public class OneProvider : BaseProvider {
        private const int PAGE_SIZE = 10;

        private string cookie = null;
        private string tokenOne = null;

        // 图文 - 「ONE · 一个」
        // http://m.wufazhuce.com/one
        private const string URL_TOKEN = "http://m.wufazhuce.com/one";
        // {0}：最大ID（返回结果不含该ID）
        private const string URL_API = "http://m.wufazhuce.com/one/ajaxlist/{0}?_token={1}";
        private const string URL_API_SEARCH = "http://m.wufazhuce.com/searchPic?searchString={0}&page={1}";

        private Meta ParseBean(OneApiData bean) {
            Meta meta = new Meta {
                Id = bean.Id,
                Uhd = bean.ImgUrl,
                Thumb = bean.ImgUrl,
                Caption = bean.Title,
                Story = bean.Content?.Trim(),
                Copyright = bean.PictureAuthor,
                Src = bean.Url,
                Format = FileUtil.ParseFormat(bean.ImgUrl)
            };
            if (int.TryParse(bean.Id, out int id)) {
                meta.No = id;
            }
            if (!string.IsNullOrEmpty(bean.Content)) {
                meta.Title = "";
                string content = bean.Content.Replace("\r\n", " ").Replace("\n", " ");
                foreach (Match match in Regex.Matches(content, @"([^  ，、。！？；：(?:——)]+)([  ，、。！？；：(?:——)])").Cast<Match>()) {
                    meta.Title += match.Groups[1].Value;
                    if (meta.Title.Length < 6) {
                        meta.Title += match.Groups[2].Value;
                    } else {
                        if (meta.Title.Length > 16) {
                            meta.Title = meta.Title.Substring(0, 16);
                        }
                        break;
                    }
                }
                meta.Title += "……";
            }
            if (!string.IsNullOrEmpty(bean.TextAuthors)) {
                meta.Story += "\n——" + bean.TextAuthors;
            }
            if (DateTime.TryParseExact(bean.Date, "yyyy / MM / dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        private Meta ParseBean(string html) {
            Meta meta = new Meta();
            Match match = Regex.Match(html, "href=\"(http.+?/(\\d+))\"");
            if (match.Success) {
                meta.Id = match.Groups[2].Value;
                meta.Src = match.Groups[1].Value;
            }
            match = Regex.Match(html, "class=\"date\">(.+?)<");
            if (match.Success && DateTime.TryParse(match.Groups[1].Value, out DateTime date)) { // Oct 13, 2022
                meta.Date = date;
            }
            match = Regex.Match(html, "src=\"(.+?)\"");
            if (match.Success) {
                meta.Uhd = match.Groups[1].Value;
                meta.Thumb = match.Groups[1].Value;
            }
            match = Regex.Match(html, "\"text-content-short\">(.+?)<", RegexOptions.Singleline);
            if (match.Success) {
                meta.Story = match.Groups[1].Value.Trim();
                meta.Title = "";
                string content = meta.Story.Replace("\r\n", " ").Replace("\n", " ");
                foreach (Match m in Regex.Matches(content, @"([^  ，、。！？；：(?:——)]+)([  ，、。！？；：(?:——)])").Cast<Match>()) {
                    meta.Title += m.Groups[1].Value;
                    if (meta.Title.Length < 6) {
                        meta.Title += m.Groups[2].Value;
                    } else {
                        if (meta.Title.Length > 16) {
                            meta.Title = meta.Title.Substring(0, 16);
                        }
                        break;
                    }
                }
                meta.Title += "……";
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, Ini ai, BaseIni bi, Go go) {
            if (go.Tag.Length > 0) {
                int page = (int)Math.Ceiling(GetCount() * 1.0 / PAGE_SIZE) + 1; // 从1开始
                string urlApi = string.Format(URL_API_SEARCH, Uri.EscapeDataString(go.Tag), page);
                LogUtil.D("LoadData() provider url: " + urlApi);
                try {
                    HttpClient client = new HttpClient();
                    HttpResponseMessage res = await client.GetAsync(urlApi, token);
                    string htmlData = await res.Content.ReadAsStringAsync();
                    //LogUtil.D("LoadData() provider data: " + htmlData.Trim());
                    MatchCollection mc = Regex.Matches(htmlData, "<div class=\"item-picture.+?</div>", RegexOptions.Singleline);
                    List<Meta> metasAdd = new List<Meta>();
                    foreach (Match m in mc.Cast<Match>()) {
                        metasAdd.Add(ParseBean(m.Value));
                    }
                    AppendMetas(metasAdd);
                    return true;
                } catch (Exception e) {
                    // 情况1：任务被取消
                    // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                    LogUtil.E("LoadData() " + e.Message);
                }
            } else {
                if (string.IsNullOrEmpty(cookie) || string.IsNullOrEmpty(tokenOne)) {
                    try {
                        HttpClient client = new HttpClient();
                        HttpResponseMessage msg = await client.GetAsync(URL_TOKEN, token);
                        cookie = new List<string>(msg.Headers.GetValues("Set-Cookie"))[0];
                        LogUtil.D("LoadData() cookie: " + cookie);
                        string htmlData = await msg.Content.ReadAsStringAsync();
                        Match match = Regex.Match(htmlData, @"One.token ?= ?[""'](.+?)[""']");
                        if (match.Success) {
                            tokenOne = match.Groups[1].Value;
                            LogUtil.D("LoadData() token: " + token);
                        }
                    } catch (Exception e) {
                        // 情况1：任务被取消
                        // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                        LogUtil.E("LoadData() " + e.Message);
                    }
                }
                if (string.IsNullOrEmpty(cookie) || string.IsNullOrEmpty(tokenOne)) {
                    return false;
                }
                int no;
                if ("random".Equals(bi.Order)) {
                    // ID 非连续，会有少量缺失，通过系数修正
                    no = (3012 + new Random().Next((int)((DateTime.Now - DateTime.Parse("2020-11-10")).Days * 1.02449)));
                } else {
                    no = GetMinNo();
                    // 不含，需+1；注意 int.MaxValue + 1 为负
                    no = go.No < int.MaxValue && go.No + 1 < no ? go.No + 1 : no;
                }
                string urlApi = string.Format(URL_API, no, tokenOne);
                LogUtil.D("LoadData() provider url: " + urlApi);
                try {
                    HttpClientHandler handler = new HttpClientHandler() {
                        UseCookies = false
                    };
                    HttpClient client = new HttpClient(handler);
                    HttpRequestMessage msgReq = new HttpRequestMessage(HttpMethod.Get, urlApi);
                    msgReq.Headers.Add("Cookie", cookie);
                    HttpResponseMessage msgRes = await client.SendAsync(msgReq, token);
                    string jsonData = await msgRes.Content.ReadAsStringAsync();
                    //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                    OneApi api = JsonConvert.DeserializeObject<OneApi>(jsonData);
                    List<Meta> metasAdd = new List<Meta>();
                    foreach (OneApiData item in api.Data) {
                        metasAdd.Add(ParseBean(item));
                    }
                    AppendMetas(metasAdd);
                    return true;
                } catch (Exception e) {
                    // 情况1：任务被取消
                    // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                    LogUtil.E("LoadData() " + e.Message);
                }
            }
            return false;
        }
    }
}
