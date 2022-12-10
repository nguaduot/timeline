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
using System.Linq;

namespace Timeline.Providers {
    public class IhansenProvider : BaseProvider {
        private const int PAGE_SIZE = 10;
        private readonly DateTime PAGE_MIN = DateTime.Parse("2022-04-25");
        private DateTime page = DateTime.Today;

        // 美图集 - 看好的壁纸、风景、素材库
        // https://photo.ihansen.org/today
        private const string URL_API = "https://api.ihansen.org/img/detail?page={0}&perPage={1}&index=&orderBy=today&tag=&favorites=";

        private Meta ParseBean(IhansenApi bean) {
            Meta meta = new Meta {
                Id = bean.Id,
                Uhd = bean.Raw,
                Thumb = bean.SmallUrl,
                //Title = bean.Info?.Title,
                Caption = bean.Info?.Description
            };

            if (bean.Info.Tags != null && bean.Info.Tags.Count > 0) {
                List<string> tags = new List<string>();
                foreach (IhansenApiTag tag in bean.Info.Tags) {
                    tags.Add(tag.Title);
                }
                meta.Story = string.Join(", ", tags);
            }
            if (!string.IsNullOrEmpty(bean.Raw)) {
                Uri uri = new Uri(bean.Raw);
                string[] nameSuffix = uri.Segments[uri.Segments.Length - 1].Split(".");
                meta.Format = nameSuffix.Length > 1 ? "." + nameSuffix[nameSuffix.Length - 1].ToLower() : ".jpg";
            }
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.TodayStr, out DateTime date)) {
                meta.Date = date;
                meta.Title = date.ToString("M");
            }
            return meta;
        }

        private List<Meta> FixMetas(List<Meta> metas) {
            List<Meta> metasNew = metas.OrderBy(p => p.Id).ToList();
            for (int i = 0; i < metasNew.Count; ++i) {
                if (string.IsNullOrEmpty(metasNew[i].Title)) {
                    metasNew[i].Title = "#" + (i + 1);
                } else {
                    metasNew[i].Title += " #" + (i + 1);
                }
            }
            return metasNew.OrderBy(p => new Random().NextDouble()).ToList();
        }

        public override async Task<bool> LoadData(CancellationToken token, Ini ai, BaseIni bi, Go go) {
            if (page < PAGE_MIN) { // 没有更多数据
                return true;
            }
            int index;
            if ("random".Equals(bi.Order)) {
                index = new Random().Next(DateTime.Today.Subtract(PAGE_MIN).Days + 1);
            } else { // date
                index = DateTime.Today.Subtract(page).Days;
            }
            string urlApi = string.Format(URL_API, index, PAGE_SIZE);
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                List<Meta> metasAdd = new List<Meta>();
                List<IhansenApi> api = JsonConvert.DeserializeObject<List<IhansenApi>>(jsonData);
                foreach (IhansenApi item in api) {
                    if (item.Width >= item.Height) { // 仅保留横图
                        metasAdd.Add(ParseBean(item));
                    }
                }
                AppendMetas(FixMetas(metasAdd));
                page = page.AddDays(-1);
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
