using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class LspApi {
        // 图片信息数组
        [JsonProperty(PropertyName = "data")]
        public List<LspApiData> Data { set; get; }
    }

    public class LspApiData {
        // ID
        [JsonProperty(PropertyName = "id")]
        public string Id { set; get; }

        // 收录顺序
        [JsonProperty(PropertyName = "no")]
        public int No { set; get; }

        // 标题
        [JsonProperty(PropertyName = "title")]
        public string Title { set; get; }

        // 故事（null）
        [JsonProperty(PropertyName = "story")]
        public string Story { set; get; }

        // 版权所有（可能为null）
        [JsonProperty(PropertyName = "copyright")]
        public string Copyright { set; get; }

        // 收录日期：yyyy-MM-dd
        [JsonProperty(PropertyName = "reldate")]
        public string RelDate { set; get; }

        // R22
        [JsonProperty(PropertyName = "r22")]
        public int R22 { set; get; }

        // 热度分
        [JsonProperty(PropertyName = "score")]
        public float Score { set; get; }

        // 分类ID
        [JsonProperty(PropertyName = "cateid")]
        public string CateId { set; get; }

        // 分类名
        [JsonProperty(PropertyName = "catename")]
        public string CateName { set; get; }

        // 图片URL
        [JsonProperty(PropertyName = "imgurl")]
        public string ImgUrl { set; get; }

        // 缩略图URL
        [JsonProperty(PropertyName = "thumburl")]
        public string ThumbUrl { set; get; }
    }
}
