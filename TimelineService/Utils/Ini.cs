using System;
using System.Collections.Generic;

namespace TimelineService.Utils {
    public sealed class Ini {
        private readonly HashSet<string> PROVIDER = new HashSet<string>() {
            AbyssIni.GetId(),
            BackieeIni.GetId(),
            BingIni.GetId(),
            GluttonIni.GetId(),
            Himawari8Ini.GetId(),
            IhansenIni.GetId(),
            InfinityIni.GetId(),
            LocalIni.GetId(),
            LspIni.GetId(),
            NasaIni.GetId(),
            NetbianIni.GetId(),
            ObzhiIni.GetId(),
            OneIni.GetId(),
            OneplusIni.GetId(),
            QingbzIni.GetId(),
            SimpleIni.GetId(),
            SkitterIni.GetId(),
            TimelineIni.GetId(),
            ToopicIni.GetId(),
            YmyouliIni.GetId(),
            WallhavenIni.GetId(),
            WallhereIni.GetId(),
            WallpaperupIni.GetId(),
            ZzzmhIni.GetId()
        };

        private string provider = BingIni.GetId();
        public string Provider {
            set => provider = PROVIDER.Contains(value) ? value : BingIni.GetId();
            get => provider;
        }

        private string folder = "";
        public string Folder {
            //set => folder = string.Concat((value ?? "").Split(Path.GetInvalidFileNameChars()));
            set => folder = value ?? "";
            get => folder;
        }

        public int Cache { set; get; } = 600;

        public string DesktopProvider { set; get; }

        public string LockProvider { set; get; }

        public string ToastProvider { set; get; }

        public string TileProvider { set; get; }

        public AbyssIni Abyss { set; get; } = new AbyssIni();
        public BackieeIni Backiee { set; get; } = new BackieeIni();
        public BingIni Bing { set; get; } = new BingIni();
        public GluttonIni Glutton { set; get; } = new GluttonIni();
        public Himawari8Ini Himawari8 { set; get; } = new Himawari8Ini();
        public IhansenIni Ihansen { set; get; } = new IhansenIni();
        public InfinityIni Infinity { set; get; } = new InfinityIni();
        public LocalIni Local { set; get; } = new LocalIni();
        public LspIni Lsp { set; get; } = new LspIni();
        public NasaIni Nasa { set; get; } = new NasaIni();
        public NetbianIni Netbian { set; get; } = new NetbianIni();
        public ObzhiIni Obzhi { set; get; } = new ObzhiIni();
        public OneIni One { set; get; } = new OneIni();
        public OneplusIni Oneplus { set; get; } = new OneplusIni();
        public QingbzIni Qingbz { set; get; } = new QingbzIni();
        public SimpleIni Simple { set; get; } = new SimpleIni();
        public SkitterIni Skitter { set; get; } = new SkitterIni();
        public TimelineIni Timeline { set; get; } = new TimelineIni();
        public ToopicIni Toopic { set; get; } = new ToopicIni();
        public WallhavenIni Wallhaven { set; get; } = new WallhavenIni();
        public WallhereIni Wallhere { set; get; } = new WallhereIni();
        public WallpaperupIni Wallpaperup { set; get; } = new WallpaperupIni();
        public YmyouliIni Ymyouli { set; get; } = new YmyouliIni();
        public ZzzmhIni Zzzmh { set; get; } = new ZzzmhIni();

        public float GetDesktopPeriod(string provider) {
            Dictionary<string, float> dict = new Dictionary<string, float>() {
                { AbyssIni.GetId(), Abyss.DesktopPeriod },
                { BackieeIni.GetId(), Backiee.DesktopPeriod },
                { BingIni.GetId(), Bing.DesktopPeriod },
                { GluttonIni.GetId(), Glutton.DesktopPeriod },
                { Himawari8Ini.GetId(), Himawari8.DesktopPeriod },
                { IhansenIni.GetId(), Ihansen.DesktopPeriod },
                { InfinityIni.GetId(), Infinity.DesktopPeriod },
                { LocalIni.GetId(), Local.DesktopPeriod },
                { LspIni.GetId(), Lsp.DesktopPeriod },
                { NasaIni.GetId(), Nasa.DesktopPeriod },
                { NetbianIni.GetId(), Netbian.DesktopPeriod },
                { ObzhiIni.GetId(), Obzhi.DesktopPeriod },
                { OneIni.GetId(), One.DesktopPeriod },
                { OneplusIni.GetId(), Oneplus.DesktopPeriod },
                { QingbzIni.GetId(), Qingbz.DesktopPeriod },
                { SimpleIni.GetId(), Simple.DesktopPeriod },
                { SkitterIni.GetId(), Skitter.DesktopPeriod },
                { TimelineIni.GetId(), Timeline.DesktopPeriod },
                { ToopicIni.GetId(), Toopic.DesktopPeriod },
                { WallhavenIni.GetId(), Wallhaven.DesktopPeriod },
                { WallhereIni.GetId(), Wallhere.DesktopPeriod },
                { WallpaperupIni.GetId(), Wallpaperup.DesktopPeriod },
                { YmyouliIni.GetId(), Ymyouli.DesktopPeriod },
                { ZzzmhIni.GetId(), Zzzmh.DesktopPeriod }
            };
            return dict.GetValueOrDefault(provider, Bing.DesktopPeriod);
            //if (LocalIni.GetId().Equals(provider)) {
            //    return Local.DesktopPeriod;
            //} else if (NasaIni.GetId().Equals(provider)) {
            //    return Nasa.DesktopPeriod;
            //} else if (TimelineIni.GetId().Equals(provider)) {
            //    return Timeline.DesktopPeriod;
            //} else if (OneIni.GetId().Equals(provider)) {
            //    return One.DesktopPeriod;
            //} else if (IhansenIni.GetId().Equals(provider)) {
            //    return Ihansen.DesktopPeriod;
            //} else if (Himawari8Ini.GetId().Equals(provider)) {
            //    return Himawari8.DesktopPeriod;
            //} else if (YmyouliIni.GetId().Equals(provider)) {
            //    return Ymyouli.DesktopPeriod;
            //} else if (QingbzIni.GetId().Equals(provider)) {
            //    return Qingbz.DesktopPeriod;
            //} else if (WallhavenIni.GetId().Equals(provider)) {
            //    return Wallhaven.DesktopPeriod;
            //} else if (WallhereIni.GetId().Equals(provider)) {
            //    return Wallhere.DesktopPeriod;
            //} else if (ZzzmhIni.GetId().Equals(provider)) {
            //    return Zzzmh.DesktopPeriod;
            //} else if (ToopicIni.GetId().Equals(provider)) {
            //    return Toopic.DesktopPeriod;
            //} else if (NetbianIni.GetId().Equals(provider)) {
            //    return Netbian.DesktopPeriod;
            //} else if (BackieeIni.GetId().Equals(provider)) {
            //    return Backiee.DesktopPeriod;
            //} else if (SkitterIni.GetId().Equals(provider)) {
            //    return Skitter.DesktopPeriod;
            //} else if (AbyssIni.GetId().Equals(provider)) {
            //    return Abyss.DesktopPeriod;
            //} else if (SimpleIni.GetId().Equals(provider)) {
            //    return Simple.DesktopPeriod;
            //} else if (InfinityIni.GetId().Equals(provider)) {
            //    return Infinity.DesktopPeriod;
            //} else if (GluttonIni.GetId().Equals(provider)) {
            //    return Glutton.DesktopPeriod;
            //} else if (LspIni.GetId().Equals(provider)) {
            //    return Lsp.DesktopPeriod;
            //} else if (OneplusIni.GetId().Equals(provider)) {
            //    return Oneplus.DesktopPeriod;
            //} else if (WallpaperupIni.GetId().Equals(provider)) {
            //    return Wallpaperup.DesktopPeriod;
            //} else if (ObzhiIni.GetId().Equals(provider)) {
            //    return Obzhi.DesktopPeriod;
            //} else {
            //    return Bing.DesktopPeriod;
            //}
        }

        public float GetLockPeriod(string provider) {
            Dictionary<string, float> dict = new Dictionary<string, float>() {
                { AbyssIni.GetId(), Abyss.LockPeriod },
                { BackieeIni.GetId(), Backiee.LockPeriod },
                { BingIni.GetId(), Bing.LockPeriod },
                { GluttonIni.GetId(), Glutton.LockPeriod },
                { Himawari8Ini.GetId(), Himawari8.LockPeriod },
                { IhansenIni.GetId(), Ihansen.LockPeriod },
                { InfinityIni.GetId(), Infinity.LockPeriod },
                { LocalIni.GetId(), Local.LockPeriod },
                { LspIni.GetId(), Lsp.LockPeriod },
                { NasaIni.GetId(), Nasa.LockPeriod },
                { NetbianIni.GetId(), Netbian.LockPeriod },
                { ObzhiIni.GetId(), Obzhi.LockPeriod },
                { OneIni.GetId(), One.LockPeriod },
                { OneplusIni.GetId(), Oneplus.LockPeriod },
                { QingbzIni.GetId(), Qingbz.LockPeriod },
                { SimpleIni.GetId(), Simple.LockPeriod },
                { SkitterIni.GetId(), Skitter.LockPeriod },
                { TimelineIni.GetId(), Timeline.LockPeriod },
                { ToopicIni.GetId(), Toopic.LockPeriod },
                { WallhavenIni.GetId(), Wallhaven.LockPeriod },
                { WallhereIni.GetId(), Wallhere.LockPeriod },
                { WallpaperupIni.GetId(), Wallpaperup.LockPeriod },
                { YmyouliIni.GetId(), Ymyouli.LockPeriod },
                { ZzzmhIni.GetId(), Zzzmh.LockPeriod }
            };
            return dict.GetValueOrDefault(provider, Bing.LockPeriod);
        }

        public float GetToastPeriod(string provider) {
            Dictionary<string, float> dict = new Dictionary<string, float>() {
                { AbyssIni.GetId(), Abyss.ToastPeriod },
                { BackieeIni.GetId(), Backiee.ToastPeriod },
                { BingIni.GetId(), Bing.ToastPeriod },
                { GluttonIni.GetId(), Glutton.ToastPeriod },
                { Himawari8Ini.GetId(), Himawari8.ToastPeriod },
                { IhansenIni.GetId(), Ihansen.ToastPeriod },
                { InfinityIni.GetId(), Infinity.ToastPeriod },
                { LocalIni.GetId(), Local.ToastPeriod },
                { LspIni.GetId(), Lsp.ToastPeriod },
                { NasaIni.GetId(), Nasa.ToastPeriod },
                { NetbianIni.GetId(), Netbian.ToastPeriod },
                { ObzhiIni.GetId(), Obzhi.ToastPeriod },
                { OneIni.GetId(), One.ToastPeriod },
                { OneplusIni.GetId(), Oneplus.ToastPeriod },
                { QingbzIni.GetId(), Qingbz.ToastPeriod },
                { SimpleIni.GetId(), Simple.ToastPeriod },
                { SkitterIni.GetId(), Skitter.ToastPeriod },
                { TimelineIni.GetId(), Timeline.ToastPeriod },
                { ToopicIni.GetId(), Toopic.ToastPeriod },
                { WallhavenIni.GetId(), Wallhaven.ToastPeriod },
                { WallhereIni.GetId(), Wallhere.ToastPeriod },
                { WallpaperupIni.GetId(), Wallpaperup.ToastPeriod },
                { YmyouliIni.GetId(), Ymyouli.ToastPeriod },
                { ZzzmhIni.GetId(), Zzzmh.ToastPeriod }
            };
            return dict.GetValueOrDefault(provider, Bing.ToastPeriod);
        }

        public float GetTilePeriod(string provider) {
            Dictionary<string, float> dict = new Dictionary<string, float>() {
                { AbyssIni.GetId(), Abyss.TilePeriod },
                { BackieeIni.GetId(), Backiee.TilePeriod },
                { BingIni.GetId(), Bing.TilePeriod },
                { GluttonIni.GetId(), Glutton.TilePeriod },
                { Himawari8Ini.GetId(), Himawari8.TilePeriod },
                { IhansenIni.GetId(), Ihansen.TilePeriod },
                { InfinityIni.GetId(), Infinity.TilePeriod },
                { LocalIni.GetId(), Local.TilePeriod },
                { LspIni.GetId(), Lsp.TilePeriod },
                { NasaIni.GetId(), Nasa.TilePeriod },
                { NetbianIni.GetId(), Netbian.TilePeriod },
                { ObzhiIni.GetId(), Obzhi.TilePeriod },
                { OneIni.GetId(), One.TilePeriod },
                { OneplusIni.GetId(), Oneplus.TilePeriod },
                { QingbzIni.GetId(), Qingbz.TilePeriod },
                { SimpleIni.GetId(), Simple.TilePeriod },
                { SkitterIni.GetId(), Skitter.TilePeriod },
                { TimelineIni.GetId(), Timeline.TilePeriod },
                { ToopicIni.GetId(), Toopic.TilePeriod },
                { WallhavenIni.GetId(), Wallhaven.TilePeriod },
                { WallhereIni.GetId(), Wallhere.TilePeriod },
                { WallpaperupIni.GetId(), Wallpaperup.TilePeriod },
                { YmyouliIni.GetId(), Ymyouli.TilePeriod },
                { ZzzmhIni.GetId(), Zzzmh.TilePeriod }
            };
            return dict.GetValueOrDefault(provider, Bing.TilePeriod);
        }

        override public string ToString() {
            Dictionary<string, string> dict = new Dictionary<string, string>() {
                { AbyssIni.GetId(), Abyss.ToString() },
                { BackieeIni.GetId(), Backiee.ToString() },
                { BingIni.GetId(), Bing.ToString() },
                { GluttonIni.GetId(), Glutton.ToString() },
                { Himawari8Ini.GetId(), Himawari8.ToString() },
                { IhansenIni.GetId(), Ihansen.ToString() },
                { InfinityIni.GetId(), Infinity.ToString() },
                { LocalIni.GetId(), Local.ToString() },
                { LspIni.GetId(), Lsp.ToString() },
                { NasaIni.GetId(), Nasa.ToString() },
                { NetbianIni.GetId(), Netbian.ToString() },
                { ObzhiIni.GetId(), Obzhi.ToString() },
                { OneIni.GetId(), One.ToString() },
                { OneplusIni.GetId(), Oneplus.ToString() },
                { QingbzIni.GetId(), Qingbz.ToString() },
                { SimpleIni.GetId(), Simple.ToString() },
                { SkitterIni.GetId(), Skitter.ToString() },
                { TimelineIni.GetId(), Timeline.ToString() },
                { ToopicIni.GetId(), Toopic.ToString() },
                { WallhavenIni.GetId(), Wallhaven.ToString() },
                { WallhereIni.GetId(), Wallhere.ToString() },
                { WallpaperupIni.GetId(), Wallpaperup.ToString() },
                { YmyouliIni.GetId(), Ymyouli.ToString() },
                { ZzzmhIni.GetId(), Zzzmh.ToString() }
            };
            string paras = dict.GetValueOrDefault(provider, Bing.ToString());
            return $"/{Provider}?desktopprovider={DesktopProvider}&lockprovider={LockProvider}&toastprovider={ToastProvider}&tileprovider={TileProvider}"
                + (paras.Length > 0 ? "&" : "") + paras;
        }
    }

    public sealed class AbyssIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "abyss";
    }

    public sealed class BackieeIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "backiee";
    }

    public sealed class BingIni {
        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}";

        public static string GetId() => "bing";
    }

    public sealed class GluttonIni {
        private readonly List<string> ALBUMS = new List<string>() { "journal", "merge" };
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string album = "journal";
        public string Album {
            set => album = ALBUMS.Contains(value) ? value : "journal";
            get => album;
        }

        private string order = "date";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "date";
            get => order;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&album={Album}";

        public static string GetId() => "glutton";
    }

    public sealed class Himawari8Ini {
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

        private float desktopPeriod = 1;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 2;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&offset={Offset}&ratio={Ratio}";

        public static string GetId() => "himawari8";
    }

    public sealed class IhansenIni {
        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}";

        public static string GetId() => "ihansen";
    }

    public sealed class InfinityIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "random", "score" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}";

        public static string GetId() => "infinity";
    }

    public sealed class LocalIni {
        private string folder = "";
        public string Folder {
            //set => folder = string.Concat((value ?? "").Split(Path.GetInvalidFileNameChars()));
            set => folder = value ?? "";
            get => folder;
        }

        private int depth = 0;
        public int Depth {
            set => depth = value > 0 ? value : 0;
            get => depth;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&folder={Uri.EscapeDataString(Folder)}&depth={Depth}";

        public static string GetId() => "local";
    }

    public sealed class LspIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "date";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "date";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "lsp";
    }

    public sealed class NasaIni {
        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}";

        public static string GetId() => "nasa";
    }

    public sealed class NetbianIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "netbian";
    }

    // deprecated
    public sealed class ObzhiIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "obzhi";
    }

    public sealed class OneIni {
        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}";

        public static string GetId() => "one";
    }

    // deprecated
    public sealed class OneplusIni {
        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}";

        public static string GetId() => "oneplus";
    }

    public sealed class QingbzIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "qingbz";
    }

    public sealed class SimpleIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}";

        public static string GetId() => "simple";
    }

    public sealed class SkitterIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "skitter";
    }

    public sealed class TimelineIni {
        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}";

        public static string GetId() => "timeline";
    }

    public sealed class ToopicIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "toopic";
    }

    public sealed class WallhavenIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "wallhaven";
    }

    public sealed class WallhereIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "wallhere";
    }

    // deprecated
    public sealed class WallpaperupIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "wallpaperup";
    }

    public sealed class YmyouliIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "ymyouli";
    }

    public sealed class ZzzmhIni {
        private readonly HashSet<string> ORDERS = new HashSet<string>() { "date", "score", "random" };

        private string order = "random";
        public string Order {
            set => order = ORDERS.Contains(value) ? value : "random";
            get => order;
        }

        private string cate = "";
        public string Cate {
            set => cate = value ?? "";
            get => cate;
        }

        private float desktopPeriod = 24;
        public float DesktopPeriod {
            set => desktopPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => desktopPeriod;
        }

        private float lockPeriod = 24;
        public float LockPeriod {
            set => lockPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => lockPeriod;
        }

        private float tostPeriod = 24;
        public float ToastPeriod {
            set => tostPeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tostPeriod;
        }

        private float tilePeriod = 2;
        public float TilePeriod {
            set => tilePeriod = Math.Max(Math.Min(value, 24), 0.25f);
            get => tilePeriod;
        }

        override public string ToString() => $"desktopperiod={DesktopPeriod}&lockperiod={LockPeriod}&toastperiod={ToastPeriod}&tileperiod={TilePeriod}" +
            $"&order={Order}&cate={Cate}";

        public static string GetId() => "zzzmh";
    }
}
