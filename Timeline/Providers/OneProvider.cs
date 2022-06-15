using Timeline.Beans;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Timeline.Utils;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;
using System.Globalization;

namespace Timeline.Providers {
    public class OneProvider : BaseProvider {
        // 下一页数据索引（从0开始）（用于按需加载）
        private string nextPage = "0";

        private string cookie = null;
        private string tokenOne = null;

        // 图文 - 「ONE · 一个」
        // http://m.wufazhuce.com/one
        private const string URL_TOKEN = "http://m.wufazhuce.com/one";
        private const string URL_API = "http://m.wufazhuce.com/one/ajaxlist/{0}?_token={1}";

        private Meta ParseBean(OneApiData bean) {
            Meta meta = new Meta {
                Id = bean.Id,
                Uhd = bean.ImgUrl,
                Thumb = bean.ImgUrl,
                Title = bean.Title,
                Story = bean.Content,
                Copyright = bean.PictureAuthor
            };
            if (!string.IsNullOrEmpty(bean.Content)) {
                meta.Title = "";
                foreach (Match match in Regex.Matches(bean.Content, @"([^  ，、。！？；：(?:——)\n(?:\r\n)]+)([  ，、。！？；：(?:——)\n(?:\r\n)])")) {
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
                return metas.Count > 0;
            }

            if ("random".Equals(bi.Order)) {
                nextPage = (3012 + new Random().Next((DateTime.Now - DateTime.Parse("2020-11-10")).Days)).ToString();
            } else {
                nextPage = metas.Count > 0 ? metas[metas.Count - 1].Id : "0";
            }
            string urlApi = string.Format(URL_API, nextPage, tokenOne);
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
