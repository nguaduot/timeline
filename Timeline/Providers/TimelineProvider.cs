using Timeline.Beans;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Timeline.Utils;
using System.Collections.Generic;
using System.Threading;

namespace Timeline.Providers {
    public class TimelineProvider : BaseProvider {
        // 自建图源
        // https://github.com/nguaduot/TimelineApi
        private const string URL_API = "https://api.nguaduot.cn/timeline/v2" +
            "?client=timelinewallpaper&device={0}" +
            "&order={1}&cate={2}&unauthorized={3}" +
            "&tag={4}&no={5}&date={6}&score={7}&admin={8}";

        private Meta ParseBean(TimelineApiData bean) {
            Meta meta = new Meta {
                Id = bean.Id,
                No = bean.No,
                Uhd = bean.ImgUrl,
                Thumb = bean.ThumbUrl,
                Title = bean.Title,
                Cate = bean.CateName,
                Story = bean.Story?.Trim(),
                Src = bean.SrcUrl,
                Score = bean.Score,
                Format = FileUtil.ParseFormat(bean.ImgUrl)
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
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.RelDate, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, Go go) {
            TimelineIni ini = bi as TimelineIni;
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
                bi.Order, bi.Cate, ini.Unauthorized,
                go.Tag, no, date.ToString("yyyyMMdd"), score, go.Admin);
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
