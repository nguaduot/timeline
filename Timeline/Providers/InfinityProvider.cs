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
    public class InfinityProvider : BaseProvider {
        private const int PAGE_SIZE = 50;

        // Infinity新标签页 - 壁纸库
        // http://cn.infinitynewtab.com/
        // page：从0开始
        private const string URL_API = "https://api.infinitynewtab.com/v2/get_wallpaper_list?client=pc&order=like&page={0}";
        //private const string URL_API_LANDSCAPE = "https://api.infinitynewtab.com/v2/get_wallpaper_list?client=pc&source=InfinityLandscape&page={0}";
        //private const string URL_API_ACG = "https://api.infinitynewtab.com/v2/get_wallpaper_list?client=pc&source=Infinity&page={0}";
        private const string URL_API_RANDOM = "https://infinity-api.infinitynewtab.com/random-wallpaper?_={0}";

        private Meta ParseBean(InfinityApiData bean) {
            Meta meta = new Meta {
                Id = bean.Id,
                Uhd = bean.Src?.RawSrc,
                Thumb = bean.Src?.SmallSrc ?? bean.Src?.RawSrc
            };

            string[] nodes = (bean.No ?? "").Split("/");
            meta.Title = string.Format("{0} #{1}", bean.Source,
                nodes[nodes.Length - 1].Substring(0, Math.Min(nodes[nodes.Length - 1].Length, 10)));
            if (bean.Tags != null) {
                meta.Story = string.Join(", ", bean.Tags ?? new List<string>());
            }
            if (!string.IsNullOrEmpty(bean.Src?.RawSrc)) {
                Uri uri = new Uri(bean.Src.RawSrc);
                string[] nameSuffix = uri.Segments[uri.Segments.Length - 1].Split(".");
                meta.Format = nameSuffix.Length > 1 ? "." + nameSuffix[nameSuffix.Length - 1] : ".jpg";
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, Ini ai, BaseIni bi, Go go) {
            string urlApi;
            if ("score".Equals(bi.Order)) {
                urlApi = string.Format(URL_API, (int)Math.Ceiling(GetCount() * 1.0 / PAGE_SIZE));
            } else {
                urlApi = string.Format(URL_API_RANDOM, DateUtil.CurrentTimeMillis());
            }
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                List<Meta> metasAdd = new List<Meta>();
                if ("score".Equals(bi.Order)) {
                    InfinityApi2 api = JsonConvert.DeserializeObject<InfinityApi2>(jsonData);
                    foreach (InfinityApiData item in api.Data.List) {
                        metasAdd.Add(ParseBean(item));
                    }
                    AppendMetas(metasAdd);
                } else {
                    InfinityApi1 api = JsonConvert.DeserializeObject<InfinityApi1>(jsonData);
                    foreach (InfinityApiData item in api.Data) {
                        metasAdd.Add(ParseBean(item));
                    }
                    AppendMetas(metasAdd);
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
