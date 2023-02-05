using Timeline.Beans;
using Timeline.Utils;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Timeline.Providers {
    public class SimpleProvider : BaseProvider {
        private const string URL_API = "https://api.nguaduot.cn/simple/v2" +
            "?client=timelinewallpaper&device={0}" +
            "&order={1}&tag={2}&no={3}&date={4}&score={5:F4}&admin={6}";

        private Meta ParseBean(SimpleApiData bean) {
            Meta meta = new Meta {
                Id = bean.Id,
                No = bean.No,
                Uhd = bean.ImgUrl,
                Thumb = bean.ThumbUrl,
                Title = bean.Title,
                Src = bean.SrcUrl,
                Score = bean.Score,
                Format = FileUtil.ParseFormat(bean.ImgUrl)
            };
            if (!string.IsNullOrEmpty(bean.Copyright)) {
                meta.Copyright = "@" + bean.Copyright;
            }
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.RelDate, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, Ini ai, BaseIni bi, Go go) {
            int no = go.No;
            DateTime date = go.Date;
            float score = go.Score;
            if (GetCount() > 0) {
                if ("date".Equals(bi.Order)) {
                    no = Math.Min(no, GetMinNo());
                    date = GetMinDate() < date ? GetMinDate() : date;
                } else if ("score".Equals(bi.Order)) {
                    score = Math.Min(score, GetMinScore());
                }
            }
            string urlApi = string.Format(URL_API, SysUtil.GetDeviceId(),
                bi.Order, go.Tag, no, date.ToString("yyyyMMdd"), score, go.Admin);
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                SimpleApi api = JsonConvert.DeserializeObject<SimpleApi>(jsonData);
                if (api.Status != 1) {
                    return false;
                }
                List<Meta> metasAdd = new List<Meta>();
                foreach (SimpleApiData item in api.Data) {
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
