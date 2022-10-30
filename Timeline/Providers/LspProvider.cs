using Timeline.Beans;
using Timeline.Utils;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace Timeline.Providers {
    public class LspProvider : BaseProvider {
        private const string URL_API = "https://api.nguaduot.cn/lsp/v2" +
            "?client=timelinewallpaper&device={0}" +
            "&order={1}&cate={2}&r22={3}" +
            "&tag={4}&no={5}&date={6}&score={7}&admin={8}";

        private Meta ParseBean(LspApiData bean) {
            Meta meta = new Meta {
                Id = bean.Id,
                No = bean.No,
                Uhd = bean.ImgUrl,
                Thumb = bean.ThumbUrl,
                Title = bean.Album,
                Caption = bean.Title,
                Cate = bean.CateName,
                Score = bean.Score,
                Format = FileUtil.ParseFormat(bean.ImgUrl)
            };
            if (bean.R22 != 0) {
                if (!string.IsNullOrEmpty(meta.Title)) {
                    meta.Title = "🚫 " + meta.Title;
                } else if (!string.IsNullOrEmpty(meta.Caption)) {
                    meta.Caption = "🚫 " + meta.Caption;
                }
            }
            if (!string.IsNullOrEmpty(bean.Character)) {
                meta.Story = "@" + bean.Character;
            }
            if (!string.IsNullOrEmpty(bean.Copyright)) {
                meta.Copyright = "© " + bean.Copyright;
            }
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.RelDate, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, Ini ai, BaseIni bi, Go go) {
            LspIni ini = bi as LspIni;
            int no = go.No;
            DateTime date = go.Date;
            float score = go.Score;
            if (GetCount() > 0) {
                if ("date".Equals(ini.Order)) {
                    no = Math.Min(no, GetMinNo());
                    date = GetMinDate() < date ? GetMinDate() : date;
                } else if ("score".Equals(ini.Order)) {
                    score = Math.Min(score, GetMinScore());
                }
            }
            string urlApi = string.Format(URL_API, SysUtil.GetDeviceId(),
                bi.Order, bi.Cate, ini.R22 ? 1 : 0,
                go.Tag, no, date.ToString("yyyyMMdd"), score, go.Admin);
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                LspApi api = JsonConvert.DeserializeObject<LspApi>(jsonData);
                if (api.Status != 1) {
                    return false;
                }
                List<Meta> metasAdd = new List<Meta>();
                foreach (LspApiData item in api.Data) {
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
