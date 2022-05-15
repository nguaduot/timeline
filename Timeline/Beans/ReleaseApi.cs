using Newtonsoft.Json;
using System;

namespace Timeline.Beans {
    public class ReleaseApi {
        public string Version { get; set; }

        public string Url { get; set; }
    }

    public class GiteeApi {
        // 标签
        [JsonProperty(PropertyName = "tag_name")]
        public string TagName { get; set; }

        // 标题
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        // 描述
        [JsonProperty(PropertyName = "body")]
        public string Body { get; set; }

        // 预览版本
        [JsonProperty(PropertyName = "prerelease")]
        public bool Prerelease { get; set; }
    }
}
