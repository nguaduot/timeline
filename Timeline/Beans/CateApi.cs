using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class CateApi {
        // 状态
        [JsonProperty(PropertyName = "status")]
        public int Status { set; get; }

        // 分类信息数组
        [JsonProperty(PropertyName = "data")]
        public List<CateApiData> Data { set; get; }
    }

    public class CateApiData {
        // 分类ID
        [JsonProperty(PropertyName = "id")]
        public string Id { set; get; }

        // 分类名
        [JsonProperty(PropertyName = "name")]
        public string Name { set; get; }

        // 图片数
        [JsonProperty(PropertyName = "count")]
        public int Count { set; get; }

        // 热度分
        [JsonProperty(PropertyName = "score")]
        public float Score { set; get; }
    }

    public class CateMeta {
        public string Id { get; set; }

        public string Name { get; set; }

        override public string ToString() => Name;
    }
}
