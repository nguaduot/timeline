using Timeline.Beans;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Timeline.Utils;
using System.Collections.Generic;
using System.Threading;

namespace Timeline.Providers {
    public class TimelineProvider : BaseProvider {
        // 下一页数据索引（从今日开始）（用于按需加载）
        //private DateTime nextPage = DateTime.UtcNow.AddHours(8);

        // 自建图源
        // https://github.com/nguaduot/TimelineApi
        private const string URL_API = "https://api.nguaduot.cn/timeline/v2?client=timelinewallpaper" +
            "&order={0}&cate={1}" +
            "&tag={2}&no={3}&date={4}&score={5}" +
            "&unauthorized={6}&marked={7}";

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
            int no = GetMinNo();
            no = go.No < no ? go.No : no;
            DateTime date = GetMinDate();
            date = go.Date < date ? go.Date : date;
            float score = GetMinScore();
            score = go.Score < score ? go.Score : score;
            string urlApi = string.Format(URL_API, bi.Order, bi.Cate,
                go.Tag, no, date, score,
                ini.Unauthorized, "marked".Equals(ini.Admin) ? SysUtil.GetDeviceId() : "");
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
