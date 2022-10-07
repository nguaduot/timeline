using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class GluttonApi {
        // 状态
        [JsonProperty(PropertyName = "status")]
        public int Status { set; get; }

        // 图片信息数组
        [JsonProperty(PropertyName = "data")]
        public List<GluttonApiData> Data { set; get; }
    }

    public class GluttonApiData {
        // ID
        [JsonProperty(PropertyName = "id")]
        public string Id { set; get; }

        // 收录顺序
        [JsonProperty(PropertyName = "no")]
        public int No { set; get; }

        // 标题
        [JsonProperty(PropertyName = "title")]
        public string Title { set; get; }

        // 版权所有
        [JsonProperty(PropertyName = "copyright")]
        public string Copyright { set; get; }

        // 收录日期：yyyy-MM-dd
        [JsonProperty(PropertyName = "reldate")]
        public string RelDate { set; get; }

        // 期数（仅限周选集）
        [JsonProperty(PropertyName = "phase")]
        public int Phase { set; get; }

        // 热度分
        [JsonProperty(PropertyName = "score")]
        public float Score { set; get; }

        // 图片URL
        [JsonProperty(PropertyName = "imgurl")]
        public string ImgUrl { set; get; }

        // 缩略图URL（可能为null）
        [JsonProperty(PropertyName = "thumburl")]
        public string ThumbUrl { set; get; }
    }
}
