using Timeline.Beans;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Timeline.Utils;
using System.Threading;

namespace Timeline.Providers {
    public class NasaProvider : BaseProvider {
        // 下一页数据索引（从今日开始）（用于按需加载）
        private DateTime nextPage = DateTime.UtcNow.AddHours(-4);

        private const string URL_API = "https://api.nguaduot.cn/nasa/v2?client=timelinewallpaper&order={0}&mirror={1}&enddate={2}&unaudited={3}&marked={4}";

        private Meta ParseBean(NasaApiData bean, string order) {
            Meta meta = new Meta {
                Id = bean.Id,
                Uhd = bean.ImgUrl,
                Thumb = bean.ThumbUrl,
                Title = bean.Title?.Trim(),
                Story = bean.Story?.Trim(),
                Src = bean.SrcUrl,
                Format = FileUtil.ParseFormat(bean.ImgUrl),
                SortFactor = "score".Equals(order) ? bean.Score : bean.No
            };
            if (!string.IsNullOrEmpty(bean.Copyright)) {
                meta.Copyright = "© " + bean.Copyright;
            }
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.RelDate, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, KeyValuePair<GoCmd, string> cmd) {
            DateTime date = cmd.Key == GoCmd.Date ? DateUtil.ParseDate(cmd.Value).Value : new DateTime();
            int index = cmd.Key == GoCmd.Index ? int.Parse(cmd.Value) : 0;
            // 现有数据未浏览完，无需加载更多
            if (index < metas.Count && date.Ticks == 0) {
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(token, bi, cmd);

            NasaIni ini = bi as NasaIni;
            nextPage = date.Ticks > 0 ? date : nextPage;
            string urlApi = string.Format(URL_API, ini.Order, ini.Mirror, nextPage.ToString("yyyyMMdd"),
                "unaudited".Equals(ini.Admin) ? SysUtil.GetDeviceId() : "",
                "marked".Equals(ini.Admin) ? SysUtil.GetDeviceId() : "");
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                NasaApi api = JsonConvert.DeserializeObject<NasaApi>(jsonData);
                if (api.Status != 1) {
                    return false;
                }
                List<Meta> metasAdd = new List<Meta>();
                foreach (NasaApiData item in api.Data) {
                    metasAdd.Add(ParseBean(item, ini.Order));
                }
                if ("date".Equals(ini.Order) || "score".Equals(ini.Order)) { // 有序排列
                    SortMetas(metasAdd);
                } else {
                    AppendMetas(metasAdd);
                }
                if ("date".Equals(ini.Order) && metas.Count > 0) { // 下一页日期索引
                    nextPage = metas[metas.Count - 1].Date.AddDays(-1);
                } else { // 默认索引
                    nextPage = DateTime.UtcNow.AddHours(-4);
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
