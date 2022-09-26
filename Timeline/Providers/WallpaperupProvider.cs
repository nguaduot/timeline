using Timeline.Beans;
using Timeline.Utils;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace Timeline.Providers {
    public class WallpaperupProvider : BaseProvider {
        // 页数据索引（从1开始）（用于按需加载）
        private int pageIndex = 1;

        private const string URL_API = "https://api.nguaduot.cn/wallpaperup/v2?client=timelinewallpaper&cate={0}&order={1}&page={2}&unaudited={3}&marked={4}";
        
        private Meta ParseBean(WallpaperupApiData bean, string order) {
            Meta meta = new Meta {
                Id = bean.Id,
                Uhd = bean.ImgUrl,
                Thumb = bean.ThumbUrl,
                Title = bean.Title,
                Story = bean.Story,
                Cate = bean.CateName,
                Src = bean.SrcUrl,
                Format = FileUtil.ParseFormat(bean.ImgUrl),
                SortFactor = "score".Equals(order) ? bean.Score : bean.No
            };
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.RelDate, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, KeyValuePair<GoCmd, string> cmd) {
            int index = cmd.Key == GoCmd.Index ? int.Parse(cmd.Value) : 0;
            // 现有数据未浏览完，无需加载更多
            if (index < metas.Count) {
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(token, bi, cmd);

            string urlApi = string.Format(URL_API, bi.Cate, bi.Order, pageIndex,
                "unaudited".Equals(bi.Admin) ? SysUtil.GetDeviceId() : "",
                "marked".Equals(bi.Admin) ? SysUtil.GetDeviceId() : "");
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                WallpaperupApi api = JsonConvert.DeserializeObject<WallpaperupApi>(jsonData);
                if (api.Status != 1) {
                    return false;
                }
                List<Meta> metasAdd = new List<Meta>();
                foreach (WallpaperupApiData item in api.Data) {
                    metasAdd.Add(ParseBean(item, bi.Order));
                }
                if ("date".Equals(bi.Order) || "score".Equals(bi.Order)) { // 有序排列
                    SortMetas(metasAdd);
                } else {
                    AppendMetas(metasAdd);
                }
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
