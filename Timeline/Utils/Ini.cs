using System;
using System.Collections.Generic;
using Timeline.Beans;
using Timeline.Providers;

namespace Timeline.Utils {
    public class Ini {
        private readonly Dictionary<string, BaseIni> Inis = new Dictionary<string, BaseIni>() {
            { LocalIni.ID, new LocalIni() },
            { BingIni.ID, new BingIni() },
            { NasaIni.ID, new NasaIni() },
            { OneplusIni.ID, new OneplusIni() },
            { TimelineIni.ID, new TimelineIni() },
            { OneIni.ID, new OneIni() },
            { Himawari8Ini.ID, new Himawari8Ini() },
            { YmyouliIni.ID, new YmyouliIni() },
            { QingbzIni.ID, new QingbzIni() },
            { WallhavenIni.ID, new WallhavenIni() },
            { WallhereIni.ID, new WallhereIni() },
            { WallpaperupIni.ID, new WallpaperupIni() },
            { ToopicIni.ID, new ToopicIni() },
            { NetbianIni.ID, new NetbianIni() },
            { InfinityIni.ID, new InfinityIni() },
            { ObzhiIni.ID, new ObzhiIni() },
            { GluttonIni.ID, new GluttonIni() },
            { LspIni.ID, new LspIni() }
        };
        private readonly HashSet<string> THEME = new HashSet<string>() { "", "light", "dark" };
        private string provider = BingIni.ID;
        private string desktopProvider = ""; // 非null
        private string lockProvider = ""; // 非null
        private string toastProvider = ""; // 非null
        private string tileProvider = ""; // 非null
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

        public string ToastProvider {
            set => toastProvider = Inis.ContainsKey(value) ? value : "";
            get => toastProvider;
        }

        public string TileProvider {
            set => tileProvider = Inis.ContainsKey(value) ? value : "";
            get => tileProvider;
        }

        public string Theme {
            set => theme = THEME.Contains(value) ? value : "";
            get => theme;
        }

        public int Cache { set; get; } = 600;

        public int R18 { set; get; } = 0;

        public bool SetIni(string provider, BaseIni ini) {
            if (ini == null) {
                return false;
            }
            if (Inis.ContainsKey(provider)) {
                Inis[provider] = ini;
            } else {
                Inis.Add(provider, ini);
            }
            return true;
        }

        public BaseIni GetIni(string provider = null) {
            return provider != null && Inis.ContainsKey(provider)
                ? Inis[provider] : Inis[this.provider];
        }

        public BaseProvider GenerateProvider(string provider = null) {
            return provider != null && Inis.ContainsKey(provider)
                ? Inis[provider].GenerateProvider() : Inis[this.provider].GenerateProvider();
        }

        public bool ContainsProvider(string provider) {
            return provider != null && Inis.ContainsKey(provider);
        }

        override public string ToString() {
            string paras = Inis[provider].ToString();
            return $"/{Provider}?desktopprovider={DesktopProvider}&lockprovider={LockProvider}&toastprovider={ToastProvider}&tileprovider={TileProvider}"
                + $"&theme={Theme}&cache={Cache}&r18={R18}"
                + (paras.Length > 0 ? "&" : "") + paras;
        }
    }

    public class BaseIni {
        public static readonly List<string> ADMIN = new List<string>() { "", "unaudited", "marked" };

        private string id = "base"; // 非null
        private readonly List<CateMeta> cates = new List<CateMeta>();
        private readonly List<string> orders = new List<string>();
        private readonly List<string> tags = new List<string>();
        private string order;
        private string cate = ""; // 非null，""为全部
        private float desktopPeriod = 24;
        private float lockPeriod = 24;
        private float toastPeriod = 24;
        private float tilePeriod = 2;

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

        public List<string> Tags {
            set {
                tags.Clear();
                tags.AddRange(value);
            }
            get => tags;
        }

        public string Order {
            set => order = value; // TODO
            get => order;
        }

        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        public float ToastPeriod {
            set => toastPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => toastPeriod;
        }

        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        public virtual string GetCateApi() => null;

        public virtual BaseProvider GenerateProvider() => new BaseProvider();
    }

    public class LocalIni : BaseIni {
        public const string ID = "local";
        public static readonly List<string> ORDERS = new List<string>() { "date", "random" };
        private int appetite = 20;
        private string folder = ""; // 非null
        private int depth = 0;

        public LocalIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "random";
        }

        public int Appetite {
            set => appetite = value <= 0 ? 18 : (value > 99 ? 99 : value);
            get => appetite;
        }

        public string Folder {
            //set => folder = string.Concat((value ?? "").Split(Path.GetInvalidFileNameChars()));
            set => folder = value ?? "";
            get => folder;
        }

        public int Depth {
            set => depth = value > 0 ? value : 0;
            get => depth;
        }

        public override BaseProvider GenerateProvider() => new LocalProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&folder={Uri.EscapeDataString(Folder)}&depth={Depth}&appetite={Appetite}";
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

        public override BaseProvider GenerateProvider() => new BingProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&lang={Lang}";
    }

    public class NasaIni : BaseIni {
        public const string ID = "nasa";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        public static readonly List<string> MIRRORS = new List<string>() { "", "bjp" };
        private string mirror = ""; // 非null

        public NasaIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "date";
        }

        public List<string> Mirrors {
            get => MIRRORS;
        }

        public string Mirror {
            set => mirror = MIRRORS.Contains(value) ? value : "";
            get => mirror;
        }

        public override BaseProvider GenerateProvider() => new NasaProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&mirror={Mirror}";
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

        public override BaseProvider GenerateProvider() => new TimelineProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}&unauthorized={Unauthorized}";
    }

    public class OneIni : BaseIni {
        public const string ID = "one";
        public static readonly List<string> ORDERS = new List<string>() { "date", "random" };

        public OneIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "date";
        }

        public override BaseProvider GenerateProvider() => new OneProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}";
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

        public override BaseProvider GenerateProvider() => new Himawari8Provider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&offset={Offset}&ratio={Ratio}";
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

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";
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

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";
    }

    public class WallhavenIni : BaseIni {
        public const string ID = "wallhaven";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        //public static readonly List<string> CATE = new List<string>() { "", "acg", "specific", "concise",
        //    "nature", "portrait", "game", "animal" };
        public const string URL_API_CATE = "https://api.nguaduot.cn/wallhaven/cate?client=timelinewallpaper";

        public WallhavenIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "random";
        }

        public override string GetCateApi() => URL_API_CATE;

        public override BaseProvider GenerateProvider() => new WallhavenProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";
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

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";
    }

    public class WallpaperupIni : BaseIni {
        public const string ID = "wallpaperup";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        public const string URL_API_CATE = "https://api.nguaduot.cn/wallpaperup/cate?client=timelinewallpaper";

        public WallpaperupIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "random";
        }

        public override string GetCateApi() => URL_API_CATE;

        public override BaseProvider GenerateProvider() => new WallpaperupProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";
    }

    public class ToopicIni : BaseIni {
        public const string ID = "toopic";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        public const string URL_API_CATE = "https://api.nguaduot.cn/toopic/cate?client=timelinewallpaper";

        public ToopicIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "random";
        }

        public override string GetCateApi() => URL_API_CATE;

        public override BaseProvider GenerateProvider() => new ToopicProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";
    }

    public class NetbianIni : BaseIni {
        public const string ID = "netbian";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        public const string URL_API_CATE = "https://api.nguaduot.cn/netbian/cate?client=timelinewallpaper";

        public NetbianIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "random";
        }

        public override string GetCateApi() => URL_API_CATE;

        public override BaseProvider GenerateProvider() => new NetbianProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";
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

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}";
    }

    public class GluttonIni : BaseIni {
        public const string ID = "glutton";
        public static readonly List<string> ALBUMS = new List<string>() { "journal", "rank" };
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        private string album = "journal"; // 非null

        public GluttonIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "date";
        }

        public string Album {
            set => album = ALBUMS.Contains(value) ? value : "journal";
            get => album;
        }

        public override BaseProvider GenerateProvider() => new GluttonProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&album={Album}&order={Order}";
    }

    public class LspIni : BaseIni {
        public const string ID = "lsp";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "random" };
        //public static readonly List<string> CATE = new List<string>() { "", "acg", "photograph" };
        public const string URL_API_CATE = "https://api.nguaduot.cn/lsp/cate?client=timelinewallpaper&r22={0}";

        public LspIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "random";
        }

        public bool R22 { set; get; }

        public override string GetCateApi() => string.Format(URL_API_CATE, R22);

        public override BaseProvider GenerateProvider() => new LspProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";
    }

    public class GeneralIni : BaseIni {
        public string Name { set; get; }

        public string Slogan { set; get; }
        
        public string UrlApi { set; get; }

        public override BaseProvider GenerateProvider() => new GeneralProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}&urlApi={Uri.EscapeDataString(UrlApi)}";
    }

    // deprecated
    public class OneplusIni : BaseIni {
        public const string ID = "oneplus";
        public static readonly List<string> ORDERS = new List<string>() { "date", "score", "view" };

        public OneplusIni() {
            Id = ID;
            Orders = ORDERS;
            Order = "date";
        }

        public override BaseProvider GenerateProvider() => new OneplusProvider { Id = this.Id };

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}";
    }

    // deprecated
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

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";
    }
}
