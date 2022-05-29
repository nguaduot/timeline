using System.Collections.Generic;
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
        public string Provider {
            set => provider = Inis.ContainsKey(value) ? value : BingIni.ID;
            get => provider;
        }

        private string desktopProvider = "";
        public string DesktopProvider {
            set => desktopProvider = Inis.ContainsKey(value) ? value : "";
            get => desktopProvider;
        }

        private string lockProvider = "";
        public string LockProvider {
            set => lockProvider = Inis.ContainsKey(value) ? value : "";
            get => lockProvider;
        }

        private string theme = "";
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
        private int desktopPeriod = 24;
        public int DesktopPeriod {
            set => desktopPeriod = value <= 0 || value > 24 ? 24 : value;
            get => desktopPeriod;
        }

        private int lockPeriod = 24;
        public int LockPeriod {
            set => lockPeriod = value <= 0 || value > 24 ? 24 : value;
            get => lockPeriod;
        }

        public virtual List<string> GetCate() => new List<string>();

        // 时序图源
        public virtual bool IsSequential() => true;

        public virtual BaseProvider GenerateProvider() => new BingProvider();
    }

    public class BingIni : BaseIni {
        public const string ID = "bing";
        public static readonly List<string> LANG = new List<string>() { "", "zh-cn", "en-us", "ja-jp", "de-de", "fr-fr" };

        private string lang = "";
        public string Lang {
            set => lang = LANG.Contains(value) ? value : "";
            get => lang;
        }

        public override BaseProvider GenerateProvider() => new BingProvider() { Id = ID };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&lang={Lang}";
    }

    public class NasaIni : BaseIni {
        public const string ID = "nasa";
        public static readonly List<string> MIRROR = new List<string>() { "", "bjp" };

        private string mirror = "";
        public string Mirror {
            set => mirror = MIRROR.Contains(value) ? value : "";
            get => mirror;
        }

        public override BaseProvider GenerateProvider() {
            switch (mirror) {
                case "bjp":
                    return new NasabjpProvider() { Id = ID };
                default:
                    return new NasaProvider() { Id = ID };
            }
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&mirror={Mirror}";
    }

    public class OneplusIni : BaseIni {
        public const string ID = "oneplus";
        public static readonly List<string> ORDER = new List<string>() { "date", "rate", "view" };

        private string order = "date";
        public string Order {
            set => order = ORDER.Contains(value) ? value : "date";
            get => order;
        }

        public override bool IsSequential() => "date".Equals(order);

        public override BaseProvider GenerateProvider() => new OneplusProvider() { Id = ID };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}";
    }

    public class TimelineIni : BaseIni {
        public const string ID = "timeline";
        public static readonly List<string> ORDER = new List<string>() { "date", "score", "random" };
        public static readonly List<string> CATE = new List<string>() { "", "landscape", "portrait", "culture", "term" };

        private string order = "date";
        public string Order {
            set => order = ORDER.Contains(value) ? value : "date";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = CATE.Contains(value) ? value : "";
            get => cate;
        }

        public int Unauthorized { set; get; } = 0;

        public override List<string> GetCate() {
            return CATE;
        }

        public override bool IsSequential() => "date".Equals(order);

        public override BaseProvider GenerateProvider() => new TimelineProvider() { Id = ID };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}&cate={Cate}&unauthorized={Unauthorized}";
    }

    public class Himawari8Ini : BaseIni {
        public const string ID = "himawari8";
        
        private float offset = 0;
        public float Offset {
            set => offset = value < -1 ? -1 : (value > 1 ? 1 : value);
            get => offset;
        }

        public Himawari8Ini() {
            DesktopPeriod = 1;
            LockPeriod = 2;
        }

        public override bool IsSequential() => true;

        public override BaseProvider GenerateProvider() => new Himawari8Provider() { Id = ID };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}";
    }

    public class YmyouliIni : BaseIni {
        public const string ID = "ymyouli";
        public static readonly List<string> ORDER = new List<string>() { "date", "score", "random" };
        public static readonly List<string> CATE = new List<string>() { "", "acgcharacter", "acgscene", "sky",
            "war", "sword", "artistry", "car", "portrait", "animal", "delicacy", "nature" };

        private string order = "random";
        public string Order {
            set => order = ORDER.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = CATE.Contains(value) ? value : "";
            get => cate;
        }

        public override List<string> GetCate() {
            return CATE;
        }

        public override bool IsSequential() => false;

        public override BaseProvider GenerateProvider() => new YmyouliProvider() { Id = ID };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}&cate={Cate}";
    }

    public class InfinityIni : BaseIni {
        public const string ID = "infinity";
        public static readonly List<string> ORDER = new List<string>() { "", "rate" };

        private string order = "";
        public string Order {
            set => order = ORDER.Contains(value) ? value : "";
            get => order;
        }

        public override bool IsSequential() => false;

        public override BaseProvider GenerateProvider() => new InfinityProvider() { Id = ID };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}";
    }

    public class OneIni : BaseIni {
        public const string ID = "one";
        public static readonly List<string> ORDER = new List<string>() { "date", "random" };
        
        private string order = "date";
        public string Order {
            set => order = ORDER.Contains(value) ? value : "date";
            get => order;
        }

        public override bool IsSequential() => "date".Equals(order);

        public override BaseProvider GenerateProvider() => new OneProvider() { Id = ID };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}";
    }

    public class QingbzIni : BaseIni {
        public const string ID = "qingbz";
        public static readonly List<string> ORDER = new List<string>() { "date", "score", "random" };
        public static readonly List<string> CATE = new List<string>() { "", "portrait", "acg", "nature",
            "star", "color", "car", "game", "animal" };

        private string order = "random";
        public string Order {
            set => order = ORDER.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = CATE.Contains(value) ? value : "";
            get => cate;
        }

        public override List<string> GetCate() {
            return CATE;
        }

        public override bool IsSequential() => false;

        public override BaseProvider GenerateProvider() => new QingbzProvider() { Id = ID };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}&cate={Cate}";
    }

    public class ObzhiIni : BaseIni {
        public const string ID = "obzhi";
        public static readonly List<string> ORDER = new List<string>() { "date", "score", "random" };
        public static readonly List<string> CATE = new List<string>() { "", "acg", "specific", "concise",
            "nature", "portrait", "game", "animal" };

        private string order = "random";
        public string Order {
            set => order = ORDER.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = CATE.Contains(value) ? value : "";
            get => cate;
        }

        public override List<string> GetCate() {
            return CATE;
        }

        public override bool IsSequential() => false;

        public override BaseProvider GenerateProvider() => new ObzhiProvider() { Id = ID };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}&cate={Cate}";
    }

    public class WallhereIni : BaseIni {
        public const string ID = "wallhere";
        public static readonly List<string> ORDER = new List<string>() { "date", "score", "random" };
        public static readonly List<string> CATE = new List<string>() { "", "acg", "photograph" };

        private string order = "random";
        public string Order {
            set => order = ORDER.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = CATE.Contains(value) ? value : "";
            get => cate;
        }

        public override List<string> GetCate() {
            return CATE;
        }

        public override bool IsSequential() => false;

        public override BaseProvider GenerateProvider() => new WallhereProvider() { Id = ID };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}&cate={Cate}";
    }

    public class LspIni : BaseIni {
        public const string ID = "lsp";
        public static readonly List<string> ORDER = new List<string>() { "date", "score", "random" };
        public static readonly List<string> CATE = new List<string>() { "", "acg", "photograph" };

        private string order = "random";
        public string Order {
            set => order = ORDER.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = CATE.Contains(value) ? value : "";
            get => cate;
        }

        public override List<string> GetCate() {
            return CATE;
        }

        public override bool IsSequential() => false;

        public override BaseProvider GenerateProvider() => new LspProvider() { Id = ID };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&order={Order}&cate={Cate}";
    }
}
