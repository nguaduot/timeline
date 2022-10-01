using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class BingApi {
        // 图片信息数组
        [JsonProperty(PropertyName = "images")]
        public List<BingApiImg> Images { set; get; }
    }

    public class BingApiImg {
        // ID
        [JsonProperty(PropertyName = "hsh")]
        public string Hsh { set; get; }

        // 根链接，如：/az/hprichbg/rb/Shanghai_ZH-CN10665657954
        // 前接 http://s.cn.bing.net 或 https://cn.bing.com，后接 _1920x1080.jpg 等
        [JsonProperty(PropertyName = "urlbase")]
        public string UrlBase { set; get; }

        // 本地发布日期：yyyyMMdd
        [JsonProperty(PropertyName = "enddate")]
        public string EndDate { set; get; }

        // UTC发布时间：yyyyMMddHHmm
        [JsonProperty(PropertyName = "fullstartdate")]
        public string FullStartDate { set; get; }

        // 说明+版权
        [JsonProperty(PropertyName = "copyright")]
        public string Copyright { set; get; }

        // 搜索链接
        [JsonProperty(PropertyName = "copyrightlink")]
        public string CopyrightLink { set; get; }

        // 标题
        [JsonProperty(PropertyName = "title")]
        public string Title { set; get; }

        // 副标题
        [JsonProperty(PropertyName = "caption")]
        public string Caption { set; get; }

        // 描述
        [JsonProperty(PropertyName = "desc")]
        public string Desc { set; get; }

        // ...
    }
}
