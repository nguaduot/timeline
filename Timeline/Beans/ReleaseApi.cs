using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
        // 版本
        [JsonProperty(PropertyName = "ver")]

        public string Version { get; set; }

        // 链接
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        // 运营信息
        [JsonProperty(PropertyName = "life")]

        public LifeApiData Life { get; set; }

        // 一句
        [JsonProperty(PropertyName = "glitter")]

        public string[] Glitter { get; set; }

        // 公告板
        [JsonProperty(PropertyName = "bbs")]

        public List<BbsApiData> Bbs { get; set; }
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

        // 赞助用户名单，时间升序，“,”分隔
        [JsonProperty(PropertyName = "donateuser")]
        public string DonateUser { set; get; }

        // 赞助用户名单，金额降序，“,”分隔
        [JsonProperty(PropertyName = "donaterank")]
        public string DonateRank { set; get; }

        // 已运营天数
        [JsonProperty(PropertyName = "past")]
        public int Past { set; get; }

        // 可运营天数
        [JsonProperty(PropertyName = "remain")]
        public int Remain { set; get; }
    }

    public class BbsApiData {
        // 开始日期：yyyy-MM-dd
        [JsonProperty(PropertyName = "start")]
        public string Start { set; get; }

        // 内容
        [JsonProperty(PropertyName = "comment")]
        public string Comment { set; get; }
    }
}
