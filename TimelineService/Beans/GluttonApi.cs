using Newtonsoft.Json;
using System.Collections.Generic;

namespace TimelineService.Beans {
    public sealed class GluttonApi {
        // 状态
        [JsonProperty(PropertyName = "status")]
        public int Status { set; get; }

        // 图片信息数组
        [JsonProperty(PropertyName = "data")]
        public IList<GluttonApiData> Data { set; get; }
    }

    public sealed class GluttonApiData {
        // 图片URL
        [JsonProperty(PropertyName = "imgurl")]
        public string ImgUrl { set; get; }
    }
}
