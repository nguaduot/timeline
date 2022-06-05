using System.Collections.Generic;
using Timeline.Beans;
using Timeline.Providers;

namespace Timeline.Utils {
    public class Ini {
        private readonly Dictionary<string, BaseIni> Inis = new Dictionary<string, BaseIni>() {
            { BingIni.ID, new BingIni() },
            { NasaIni.ID, new NasaIni() },
            { OneplusIni.ID, new OneplusIni() },
            { TimelineIni.ID, new TimelineIni() },
            { Himawari8Ini.ID, new Himawari8Ini() },
            { YmyouliIni.ID, new YmyouliIni() },
            { InfinityIni.ID, new InfinityIni() },
            { OneIni.ID, new OneIni() },
            { QingbzIni.ID, new QingbzIni() },
            { ObzhiIni.ID, new ObzhiIni() },
            { WallhereIni.ID, new WallhereIni() },
            { LspIni.ID, new LspIni() }
        };
        private readonly HashSet<string> THEME = new HashSet<string>() { "", "light", "dark" };
        private string provider = BingIni.ID;
        private string desktopProvider = ""; // 非null
        private string lockProvider = ""; // 非null
        private string theme = ""; // 非null

        public string Provider {
            set => provider = Inis.ContainsKey(value) ? value : BingIni.ID;
            get => provider;
        }

        public string DesktopProvider {
            set => desktopProvider = Inis.ContainsKey(value) ? value : "";
            get => desktopProvider;
        }

        public string LockProvider {
            set => lockProvider = Inis.ContainsKey(value) ? value : "";
            get => lockProvider;
        }

        public string Theme {
            set => theme = THEME.Contains(value) ? value : "";
            get => theme;
        }

        public int R18 { set; get; } = 0;

        public bool SetIni(string provider, BaseIni ini) {
            if (Inis.ContainsKey(provider) && ini != null) {
                Inis[provider] = ini;
                return true;
            }
            return false;
        }

        public BaseIni GetIni(string provider = null) {
            return provider != null && Inis.ContainsKey(provider)
                ? Inis[provider] : Inis[this.provider];
        }

        public BaseProvider GenerateProvider(string provider = null) {
            return provider != null && Inis.ContainsKey(provider)
                ? Inis[provider].GenerateProvider() : Inis[this.provider].GenerateProvider();
        }

        override public string ToString() {
            string paras = Inis[provider].ToString();
            return $"/{Provider}?desktopprovider={DesktopProvider}&lockprovider={LockProvider}&theme={Theme}&r18={R18}" + (paras.Length > 0 ? "&" : "") + paras;
        }
    }

    public class BaseIni {
        private string id = "base"; // 非null
        private readonly List<CateMeta> cates = new List<CateMeta>();
        private readonly List<string> orders = new List<string>();
        private string order;
        private string cate = ""; // 非null，""为全部
        private int desktopPeriod = 24;
        private int lockPeriod = 24;

        public string Id {
            set => id = value ?? "base";
            get => id;
        }

        public List<CateMeta> Cates {
            set {
                cates.Clear();
                cates.AddRange(value);
            }
            get => cates;
        }

        public List<string> Orders {
            set {
                orders.Clear();
                orders.AddRange(value);
            }
            get => orders;
        }

        public string Order {
            set => order = value;
            get => order;
        }

        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        public int DesktopPeriod {
            set => desktopPeriod = value <= 0 || value > 24 ? 24 : value;
            get => desktopPeriod;
        }

        public int LockPeriod {
            set => lockPeriod = value <= 0 || value > 24 ? 24 : value;
            get => lockPeriod;
        }

        public virtual string GetCateApi() => null;

        // 时序图源
        public virtual bool IsSequential() => false;

        public virtual BaseProvider GenerateProvider() => new BaseProvider();
    }

    public class BingIni : BaseIni {
        public const string ID = "bing";
        public static readonly List<string> LANGS = new List<string>() { "", "zh-cn", "en-us", "ja-jp", "de-de", "fr-fr" };
        private string lang = ""; // 非null

        public BingIni() {
            Id = ID;
        }

        public List<string> Langs {
            get => LANGS;
        }

        public string Lang {
            set => lang = LANGS.Contains(value) ? value : "";
            get => lang;
        }

        public override bool IsSequential() => true;

        public override BaseProvider GenerateProvider() => new BingProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&lang={Lang}";
    }

    public class NasaIni : BaseIni {
        public const string ID = "nasa";
        public static readonly List<string> MIRRORS = new List<string>() { "", "bjp" };
        private string mirror = ""; // 非null

        public NasaIni() {
            Id = ID;
        }

        public List<string> Mirrors {
            get => MIRRORS;
        }

        public string Mirror {
            set => mirror = MIRRORS.Contains(value) ? value : "";
            get => mirror;
        }

        public override bool IsSequential() => string.IsNullOrEmpty(mirror);

        public override BaseProvider GenerateProvider() {
            switch (mirror) {
                case "bjp":
                    return new NasabjpProvider { Id = this.Id };
                default:
                    return new NasaProvider { Id = this.Id };
            }
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&mirror={Mirror}";
    }

    public class OneplusIni : BaseIni {
        public const string ID = "oneplus";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "view" };

        public OneplusIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "date";
        }

        public override bool IsSequential() => "date".Equals(Order);

        public override BaseProvider GenerateProvider() => new OneplusProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}";
    }

    public class TimelineIni : BaseIni {
        public const string ID = "timeline";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        //public static readonly List<string> CATES = new List<string>() { "", "landscape", "portrait", "culture", "term" };
        public const string URL_API_CATE = "https://api.nguaduot.cn/timeline/cate?client=timelinewallpaper";

        public TimelineIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "date";
        }

        public int Unauthorized { set; get; } = 0;

        public override string GetCateApi() => URL_API_CATE;

        public override bool IsSequential() => "date".Equals(Order);

        public override BaseProvider GenerateProvider() => new TimelineProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}&cate={Cate}&unauthorized={Unauthorized}";
    }

    public class Himawari8Ini : BaseIni {
        public const string ID = "himawari8";
        
        private float offset = 0.5f;
        public float Offset {
            set => offset = value < 0.01f ? 0.01f : (value > 1 ? 1 : value);
            get => offset;
        }

        private float ratio = 0.5f;
        public float Ratio {
            set => ratio = value < 0.1f ? 0.1f : (value > 1 ? 1 : value);
            get => ratio;
        }

        public Himawari8Ini() {
            Id = ID;
            DesktopPeriod = 1;
            LockPeriod = 2;
        }

        public override bool IsSequential() => true;

        public override BaseProvider GenerateProvider() => new Himawari8Provider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&offset={Offset}&ratio={Ratio}";
    }

    public class YmyouliIni : BaseIni {
        public const string ID = "ymyouli";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        //public static readonly List<string> CATE = new List<string>() { "", "acgcharacter", "acgscene", "sky",
        //    "war", "sword", "artistry", "car", "portrait", "animal", "delicacy", "nature" };
        public const string URL_API_CATE = "https://api.nguaduot.cn/ymyouli/cate?client=timelinewallpaper";

        public YmyouliIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "random";
        }

        public override string GetCateApi() => URL_API_CATE;

        public override BaseProvider GenerateProvider() => new YmyouliProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}&cate={Cate}";
    }

    public class InfinityIni : BaseIni {
        public const string ID = "infinity";
        public static readonly List<string> ORDERS = new List<string>() { "random", "score" };

        public InfinityIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "random";
        }

        public override BaseProvider GenerateProvider() => new InfinityProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}";
    }

    public class OneIni : BaseIni {
        public const string ID = "one";
        public static readonly List<string> ORDERS = new List<string>() { "date", "random" };

        public OneIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "date";
        }

        public override bool IsSequential() => "date".Equals(Order);

        public override BaseProvider GenerateProvider() => new OneProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}";
    }

    public class QingbzIni : BaseIni {
        public const string ID = "qingbz";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        //public static readonly List<string> CATE = new List<string>() { "", "portrait", "acg", "nature",
        //    "star", "color", "car", "game", "animal" };
        public const string URL_API_CATE = "https://api.nguaduot.cn/qingbz/cate?client=timelinewallpaper";

        public QingbzIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "random";
        }

        public override string GetCateApi() => URL_API_CATE;

        public override BaseProvider GenerateProvider() => new QingbzProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}&cate={Cate}";
    }

    public class ObzhiIni : BaseIni {
        public const string ID = "obzhi";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        //public static readonly List<string> CATE = new List<string>() { "", "acg", "specific", "concise",
        //    "nature", "portrait", "game", "animal" };
        public const string URL_API_CATE = "https://api.nguaduot.cn/obzhi/cate?client=timelinewallpaper";

        public ObzhiIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "random";
        }

        public override string GetCateApi() => URL_API_CATE;

        public override BaseProvider GenerateProvider() => new ObzhiProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}&cate={Cate}";
    }

    public class WallhereIni : BaseIni {
        public const string ID = "wallhere";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        //public static readonly List<string> CATE = new List<string>() { "", "acg", "photograph" };
        public const string URL_API_CATE = "https://api.nguaduot.cn/wallhere/cate?client=timelinewallpaper";

        public WallhereIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "random";
        }

        public override string GetCateApi() => URL_API_CATE;

        public override BaseProvider GenerateProvider() => new WallhereProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}&cate={Cate}";
    }

    public class LspIni : BaseIni {
        public const string ID = "lsp";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        //public static readonly List<string> CATE = new List<string>() { "", "acg", "photograph" };
        public const string URL_API_CATE = "https://api.nguaduot.cn/lsp/cate?client=timelinewallpaper";

        public LspIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "random";
        }

        public override string GetCateApi() => URL_API_CATE;

        public override BaseProvider GenerateProvider() => new LspProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}&cate={Cate}";
    }
}
