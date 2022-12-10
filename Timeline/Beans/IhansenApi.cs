using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class IhansenApi {
        // ID
        [JsonProperty(PropertyName = "id")]
        public string Id { set; get; }

        // 发布日期：yyyy-MM-dd
        [JsonProperty(PropertyName = "todayStr")]
        public string TodayStr { set; get; }

        // 图片宽度
        [JsonProperty(PropertyName = "width")]
        public int Width { set; get; }

        // 图片高度
        [JsonProperty(PropertyName = "height")]
        public int Height { set; get; }

        // 详细信息
        [JsonProperty(PropertyName = "info")]
        public IhansenApiInfo Info { set; get; }

        // 图片URL
        [JsonProperty(PropertyName = "raw")]
        public string Raw { set; get; }

        // 缩略图URL
        [JsonProperty(PropertyName = "smallUrl")]
        public string SmallUrl { set; get; }
    }

    public class IhansenApiInfo {
        // 标题
        [JsonProperty(PropertyName = "title")]
        public string Title { set; get; }

        // 描述
        [JsonProperty(PropertyName = "description")]
        public string Description { set; get; }

        // 标签组
        [JsonProperty(PropertyName = "tags")]
        public List<IhansenApiTag> Tags { set; get; }
    }

    public class IhansenApiTag {
        // 标签标题
        [JsonProperty(PropertyName = "title")]
        public string Title { set; get; }
    }
}
