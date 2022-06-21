using Newtonsoft.Json;
using System.Collections.Generic;

namespace TimelineService.Beans {
    public sealed class BingApi {
        // 图片信息数组
        [JsonProperty(PropertyName = "images")]
        public IList<BingApiImg> Images { set; get; }
    }

    public sealed class BingApiImg {
        // 根链接，如：/az/hprichbg/rb/Shanghai_ZH-CN10665657954
        // 前接 http://s.cn.bing.net 或 https://cn.bing.com，后接 _1920x1080.jpg 等
        [JsonProperty(PropertyName = "urlbase")]
        public string UrlBase { set; get; }
    }
}
