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

        // 故事（可能为null）
        [JsonProperty(PropertyName = "story")]
        public string Story { set; get; }

        // 版权所有（可能为null）
        [JsonProperty(PropertyName = "copyright")]
        public string Copyright { set; get; }

        // 收录日期：yyyy-MM-dd
        [JsonProperty(PropertyName = "reldate")]
        public string RelDate { set; get; }

        // 热度分
        [JsonProperty(PropertyName = "score")]
        public float Score { set; get; }

        // 分类ID
        [JsonProperty(PropertyName = "cateid")]
        public string CateId { set; get; }

        // 分类名
        [JsonProperty(PropertyName = "catename")]
        public string CateName { set; get; }

        // 平台发布链接（可能为null）
        [JsonProperty(PropertyName = "srcurl")]
        public string SrcUrl { set; get; }

        // 图片URL
        [JsonProperty(PropertyName = "imgurl")]
        public string ImgUrl { set; get; }

        // 缩略图URL（可能为null）
        [JsonProperty(PropertyName = "thumburl")]
        public string ThumbUrl { set; get; }

        // 期数（仅限周选集）
        [JsonProperty(PropertyName = "phase")]
        public int Phase { set; get; }

        // 原图源ID
        [JsonProperty(PropertyName = "rawprovider")]
        public string RawProvider { set; get; }

        // 原图源图片ID
        [JsonProperty(PropertyName = "rawid")]
        public string RawId { set; get; }
    }
}
