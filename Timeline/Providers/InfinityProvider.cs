using Timeline.Beans;
using Timeline.Utils;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace Timeline.Providers {
    public class InfinityProvider : BaseProvider {
        // 页数据索引（从0开始）（用于按需加载）
        private int pageIndex = -1;

        // Infinity新标签页 - 壁纸库
        // http://cn.infinitynewtab.com/
        //private const string URL_API = "https://infinity-api.infinitynewtab.com/get-wallpaper?source=&tag=&order=1&page={0}";
        private const string URL_API = "https://api.infinitynewtab.com/v2/get_wallpaper_list?client=pc&order=like&page={0}";
        private const string URL_API_LANDSCAPE = "https://api.infinitynewtab.com/v2/get_wallpaper_list?client=pc&source=InfinityLandscape&page={0}";
        private const string URL_API_ACG = "https://api.infinitynewtab.com/v2/get_wallpaper_list?client=pc&source=Infinity&page={0}";
        private const string URL_API_RANDOM = "https://infinity-api.infinitynewtab.com/random-wallpaper?_={0}";

        private Meta ParseBean(InfinityApiData bean) {
            Meta meta = new Meta {
                Id = bean.Id,
                Uhd = bean.Src?.RawSrc,
                Thumb = bean.Src?.SmallSrc ?? bean.Src?.RawSrc,
                Date = DateTime.Now
            };

            string[] nodes = (bean.No ?? "").Split("/");
            meta.Title = string.Format("{0} #{1}", bean.Source, nodes[nodes.Length - 1]);
            if (bean.Tags != null) {
                meta.Story = string.Join(" ", bean.Tags ?? new List<string>());
            }
            if (!string.IsNullOrEmpty(bean.Src?.RawSrc)) {
                Uri uri = new Uri(bean.Src.RawSrc);
                string[] nameSuffix = uri.Segments[uri.Segments.Length - 1].Split(".");
                meta.Format = nameSuffix.Length > 1 ? "." + nameSuffix[nameSuffix.Length - 1] : ".jpg";
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni ini, DateTime? date = null) {
            // 现有数据未浏览完，无需加载更多，或已无更多数据
            if (indexFocus < metas.Count - 1) {
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(token, ini, date);

            string urlApi = "rate".Equals(((InfinityIni)ini).Order) ? String.Format(URL_API, ++pageIndex)
                : string.Format(URL_API_RANDOM, DateUtil.CurrentTimeMillis());
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                List<Meta> metasAdd = new List<Meta>();
                if ("rate".Equals(((InfinityIni)ini).Order)) {
                    InfinityApi2 api = JsonConvert.DeserializeObject<InfinityApi2>(jsonData);
                    foreach (InfinityApiData item in api.Data.List) {
                        metasAdd.Add(ParseBean(item));
                    }
                    RandomMetas(metasAdd);
                } else {
                    InfinityApi1 api = JsonConvert.DeserializeObject<InfinityApi1>(jsonData);
                    foreach (InfinityApiData item in api.Data) {
                        metasAdd.Add(ParseBean(item));
                    }
                    AppendMetas(metasAdd);
                }
            } catch (Exception e) {
                // 情况1：任务被取消
                // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                LogUtil.E("LoadData() " + e.Message);
            }

            return metas.Count > 0;
        }
    }
}
