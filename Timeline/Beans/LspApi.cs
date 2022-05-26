using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class LspApi {
        // 图片信息数组
        [JsonProperty(PropertyName = "data")]
        public List<LspApiData> Data { set; get; }
    }

    public class LspApiData {
        // 排序编号
        [JsonProperty(PropertyName = "no")]
        public int No { set; get; }

        // 类别内排序序号
        [JsonProperty(PropertyName = "cateno")]
        public int CateNo { set; get; }

        // 类别ID
        [JsonProperty(PropertyName = "cateid")]
        public string CateId { set; get; }

        // 类别
        [JsonProperty(PropertyName = "cate")]
        public string Cate { set; get; }

        // 图片ID
        [JsonProperty(PropertyName = "imgid")]
        public int ImgId { set; get; }

        // 图片URL
        [JsonProperty(PropertyName = "imgurl")]
        public string ImgUrl { set; get; }

        // 缩略图URL
        [JsonProperty(PropertyName = "thumburl")]
        public string ThumbUrl { set; get; }

        // 图源
        [JsonProperty(PropertyName = "provider")]
        public string Provider { set; get; }

        // 推送日期
        [JsonProperty(PropertyName = "reldate")]
        public string RelDate { set; get; }

        // 热度分
        [JsonProperty(PropertyName = "score")]
        public float Score { set; get; }

        // ...
    }
}
