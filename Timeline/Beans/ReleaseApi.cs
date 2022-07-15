using Newtonsoft.Json;
using System;

namespace Timeline.Beans {
    public class ReleaseApi {
        // 状态
        [JsonProperty(PropertyName = "status")]
        public int Status { set; get; }

        // 结果
        [JsonProperty(PropertyName = "data")]
        public ReleaseApiData Data { set; get; }
    }

    public class ReleaseApiData {
        // 版本信息
        [JsonProperty(PropertyName = "version")]

        public VersionApiData Version { get; set; }

        // 运营信息
        [JsonProperty(PropertyName = "life")]

        public LifeApiData Life { get; set; }

        // 一句
        [JsonProperty(PropertyName = "glitter")]

        public string[] Glitter { get; set; }
    }

    public class VersionApiData {
        // 版本
        [JsonProperty(PropertyName = "ver")]

        public string Version { get; set; }

        // 链接
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
    }

    public class LifeApiData {
        // 历史成本
        [JsonProperty(PropertyName = "cost")]
        public float Cost { set; get; }

        // 当前日均成本
        [JsonProperty(PropertyName = "costdaily")]
        public float CostDaily { set; get; }

        // 历史赞助
        [JsonProperty(PropertyName = "donate")]
        public float Donate { set; get; }

        // 历史赞助笔数
        [JsonProperty(PropertyName = "donatecount")]
        public int DonateCount { set; get; }

        // 赞助用户名单，金额降序，“,”分隔
        [JsonProperty(PropertyName = "donateuser")]
        public string DonateUser { set; get; }

        // 已运营天数
        [JsonProperty(PropertyName = "past")]
        public int Past { set; get; }

        // 可运营天数
        [JsonProperty(PropertyName = "remain")]
        public int Remain { set; get; }
    }
}
