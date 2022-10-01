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
    public class QingbzProvider : BaseProvider {
        private const string URL_API = "https://api.nguaduot.cn/qingbz/v2?client=timelinewallpaper" +
            "&order={0}&cate={1}" +
            "&tag={2}&no={3}&date={6}&score={5}" +
            "&unaudited={6}&marked={7}";

        private Meta ParseBean(QingbzApiData bean, string order) {
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
            if (!string.IsNullOrEmpty(bean.Copyright)) {
                meta.Copyright = "© " + bean.Copyright;
            }
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.RelDate, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, Go go) {
            await base.LoadData(token, bi, go);

            int no = GetMinNo();
            no = go.No < no ? go.No : no;
            DateTime date = GetMinDate();
            date = go.Date < date ? go.Date : date;
            float score = GetMinScore();
            score = go.Score < score ? go.Score : score;
            string urlApi = string.Format(URL_API, bi.Order, bi.Cate,
                go.Tag, no, date, score,
                "unaudited".Equals(bi.Admin) ? SysUtil.GetDeviceId() : "",
                "marked".Equals(bi.Admin) ? SysUtil.GetDeviceId() : "");
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                QingbzApi api = JsonConvert.DeserializeObject<QingbzApi>(jsonData);
                if (api.Status != 1) {
                    return false;
                }
                List<Meta> metasAdd = new List<Meta>();
                foreach (QingbzApiData item in api.Data) {
                    metasAdd.Add(ParseBean(item, bi.Order));
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
