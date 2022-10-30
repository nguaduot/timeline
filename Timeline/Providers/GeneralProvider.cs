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
    public class GeneralProvider : BaseProvider {
        private Meta ParseBean(GeneralApiData bean) {
            Meta meta = new Meta {
                Id = bean.Id,
                No = bean.No,
                Uhd = bean.ImgUrl,
                Thumb = bean.ThumbUrl,
                Title = bean.Title,
                Story = bean.Story,
                Cate = bean.CateName,
                Src = bean.SrcUrl,
                Score = bean.Score,
                Format = FileUtil.ParseFormat(bean.ImgUrl)
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

        public override async Task<bool> LoadData(CancellationToken token, Ini ai, BaseIni bi, Go go) {
            GeneralIni ini = bi as GeneralIni;
            if (string.IsNullOrEmpty(ini.UrlApi)) {
                return false;
            }
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
            string urlApi = ini.UrlApi.Replace("{client}", "timelinewallpaper")
                .Replace("{device}", SysUtil.GetDeviceId())
                .Replace("{order}", ini.Order)
                .Replace("{cate}", ini.Cate)
                .Replace("{tag}", go.Tag)
                .Replace("{no}", no.ToString())
                .Replace("{date}", date.ToString("yyyyMMdd"))
                .Replace("{score}", score.ToString())
                .Replace("{admin}", go.Admin);
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                GeneralApi api = JsonConvert.DeserializeObject<GeneralApi>(jsonData);
                if (api.Status != 1) {
                    return false;
                }
                List<Meta> metasAdd = new List<Meta>();
                foreach (GeneralApiData item in api.Data) {
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
