﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace TimelineService.Beans {
    public sealed class WallhereApi {
        // 状态
        [JsonProperty(PropertyName = "status")]
        public int Status { set; get; }

        // 图片信息数组
        [JsonProperty(PropertyName = "data")]
        public IList<WallhereApiData> Data { set; get; }
    }

    public sealed class WallhereApiData {
        // 图片URL
        [JsonProperty(PropertyName = "imgurl")]
        public string ImgUrl { set; get; }

        // 缩略图URL
        [JsonProperty(PropertyName = "thumburl")]
        public string ThumbUrl { set; get; }
    }
}