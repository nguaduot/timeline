using System;
using System.Collections.Generic;
using System.IO;
using Windows.UI.Xaml.Controls;

namespace TimelineService.Utils {
    public sealed class Ini {
        private readonly HashSet<string> PROVIDER = new HashSet<string>() {
            LocalIni.GetId(),
            BingIni.GetId(),
            NasaIni.GetId(),
            TimelineIni.GetId(),
            OneIni.GetId(),
            Himawari8Ini.GetId(),
            YmyouliIni.GetId(),
            QingbzIni.GetId(),
            WallhavenIni.GetId(),
            WallhereIni.GetId(),
            WallpaperupIni.GetId(),
            ToopicIni.GetId(),
            NetbianIni.GetId(),
            BackieeIni.GetId(),
            InfinityIni.GetId(),
            GluttonIni.GetId(),
            LspIni.GetId(),
            OneplusIni.GetId(),
            ObzhiIni.GetId()
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

        public LocalIni Local { set; get; } = new LocalIni();

        public BingIni Bing { set; get; } = new BingIni();

        public NasaIni Nasa { set; get; } = new NasaIni();

        public TimelineIni Timeline { set; get; } = new TimelineIni();

        public OneIni One { set; get; } = new OneIni();

        public Himawari8Ini Himawari8 { set; get; } = new Himawari8Ini();

        public YmyouliIni Ymyouli { set; get; } = new YmyouliIni();

        public QingbzIni Qingbz { set; get; } = new QingbzIni();

        public WallhavenIni Wallhaven { set; get; } = new WallhavenIni();
        
        public WallhereIni Wallhere { set; get; } = new WallhereIni();

        public WallpaperupIni Wallpaperup { set; get; } = new WallpaperupIni();

        public ToopicIni Toopic { set; get; } = new ToopicIni();

        public NetbianIni Netbian { set; get; } = new NetbianIni();

        public BackieeIni Backiee { set; get; } = new BackieeIni();

        public InfinityIni Infinity { set; get; } = new InfinityIni();

        public GluttonIni Glutton { set; get; } = new GluttonIni();

        public LspIni Lsp { set; get; } = new LspIni();

        public OneplusIni Oneplus { set; get; } = new OneplusIni();

        public ObzhiIni Obzhi { set; get; } = new ObzhiIni();

        public float GetDesktopPeriod(string provider) {
            if (LocalIni.GetId().Equals(provider)) {
                return Local.DesktopPeriod;
            } else if (NasaIni.GetId().Equals(provider)) {
                return Nasa.DesktopPeriod;
            } else if (OneplusIni.GetId().Equals(provider)) {
                return Oneplus.DesktopPeriod;
            } else if (TimelineIni.GetId().Equals(provider)) {
                return Timeline.DesktopPeriod;
            } else if (OneIni.GetId().Equals(provider)) {
                return One.DesktopPeriod;
            } else if (Himawari8Ini.GetId().Equals(provider)) {
                return Himawari8.DesktopPeriod;
            } else if (YmyouliIni.GetId().Equals(provider)) {
                return Ymyouli.DesktopPeriod;
            } else if (QingbzIni.GetId().Equals(provider)) {
                return Qingbz.DesktopPeriod;
            } else if (WallhavenIni.GetId().Equals(provider)) {
                return Wallhaven.DesktopPeriod;
            } else if (WallhereIni.GetId().Equals(provider)) {
                return Wallhere.DesktopPeriod;
            } else if (WallpaperupIni.GetId().Equals(provider)) {
                return Wallpaperup.DesktopPeriod;
            } else if (ToopicIni.GetId().Equals(provider)) {
                return Toopic.DesktopPeriod;
            } else if (NetbianIni.GetId().Equals(provider)) {
                return Netbian.DesktopPeriod;
            } else if (BackieeIni.GetId().Equals(provider)) {
                return Backiee.DesktopPeriod;
            } else if (InfinityIni.GetId().Equals(provider)) {
                return Infinity.DesktopPeriod;
            } else if (ObzhiIni.GetId().Equals(provider)) {
                return Obzhi.DesktopPeriod;
            } else if (GluttonIni.GetId().Equals(provider)) {
                return Glutton.DesktopPeriod;
            } else if (LspIni.GetId().Equals(provider)) {
                return Lsp.DesktopPeriod;
            } else {
                return Bing.DesktopPeriod;
            }
        }

        public float GetLockPeriod(string provider) {
            if (LocalIni.GetId().Equals(provider)) {
                return Local.LockPeriod;
            } else if (NasaIni.GetId().Equals(provider)) {
                return Nasa.LockPeriod;
            } else if (OneplusIni.GetId().Equals(provider)) {
                return Oneplus.LockPeriod;
            } else if (TimelineIni.GetId().Equals(provider)) {
                return Timeline.LockPeriod;
            } else if (OneIni.GetId().Equals(provider)) {
                return One.LockPeriod;
            } else if (Himawari8Ini.GetId().Equals(provider)) {
                return Himawari8.LockPeriod;
            } else if (YmyouliIni.GetId().Equals(provider)) {
                return Ymyouli.LockPeriod;
            } else if (QingbzIni.GetId().Equals(provider)) {
                return Qingbz.LockPeriod;
            } else if (WallhavenIni.GetId().Equals(provider)) {
                return Wallhaven.LockPeriod;
            } else if (WallhereIni.GetId().Equals(provider)) {
                return Wallhere.LockPeriod;
            } else if (WallpaperupIni.GetId().Equals(provider)) {
                return Wallpaperup.LockPeriod;
            } else if (ToopicIni.GetId().Equals(provider)) {
                return Toopic.LockPeriod;
            } else if (NetbianIni.GetId().Equals(provider)) {
                return Netbian.LockPeriod;
            } else if (BackieeIni.GetId().Equals(provider)) {
                return Backiee.LockPeriod;
            } else if (InfinityIni.GetId().Equals(provider)) {
                return Infinity.LockPeriod;
            } else if (ObzhiIni.GetId().Equals(provider)) {
                return Obzhi.LockPeriod;
            } else if (GluttonIni.GetId().Equals(provider)) {
                return Glutton.LockPeriod;
            } else if (LspIni.GetId().Equals(provider)) {
                return Lsp.LockPeriod;
            } else {
                return Bing.LockPeriod;
            }
        }

        public float GetToastPeriod(string provider) {
            if (LocalIni.GetId().Equals(provider)) {
                return Local.ToastPeriod;
            } else if (NasaIni.GetId().Equals(provider)) {
                return Nasa.ToastPeriod;
            } else if (OneplusIni.GetId().Equals(provider)) {
                return Oneplus.ToastPeriod;
            } else if (TimelineIni.GetId().Equals(provider)) {
                return Timeline.ToastPeriod;
            } else if (OneIni.GetId().Equals(provider)) {
                return One.ToastPeriod;
            } else if (Himawari8Ini.GetId().Equals(provider)) {
                return Himawari8.ToastPeriod;
            } else if (YmyouliIni.GetId().Equals(provider)) {
                return Ymyouli.ToastPeriod;
            } else if (QingbzIni.GetId().Equals(provider)) {
                return Qingbz.ToastPeriod;
            } else if (WallhavenIni.GetId().Equals(provider)) {
                return Wallhaven.ToastPeriod;
            } else if (WallhereIni.GetId().Equals(provider)) {
                return Wallhere.ToastPeriod;
            } else if (WallpaperupIni.GetId().Equals(provider)) {
                return Wallpaperup.ToastPeriod;
            } else if (ToopicIni.GetId().Equals(provider)) {
                return Toopic.ToastPeriod;
            } else if (NetbianIni.GetId().Equals(provider)) {
                return Netbian.ToastPeriod;
            } else if (BackieeIni.GetId().Equals(provider)) {
                return Backiee.ToastPeriod;
            } else if (InfinityIni.GetId().Equals(provider)) {
                return Infinity.ToastPeriod;
            } else if (ObzhiIni.GetId().Equals(provider)) {
                return Obzhi.ToastPeriod;
            } else if (GluttonIni.GetId().Equals(provider)) {
                return Glutton.ToastPeriod;
            } else if (LspIni.GetId().Equals(provider)) {
                return Lsp.ToastPeriod;
            } else {
                return Bing.ToastPeriod;
            }
        }

        public float GetTilePeriod(string provider) {
            if (LocalIni.GetId().Equals(provider)) {
                return Local.TilePeriod;
            } else if (NasaIni.GetId().Equals(provider)) {
                return Nasa.TilePeriod;
            } else if (OneplusIni.GetId().Equals(provider)) {
                return Oneplus.TilePeriod;
            } else if (TimelineIni.GetId().Equals(provider)) {
                return Timeline.TilePeriod;
            } else if (OneIni.GetId().Equals(provider)) {
                return One.TilePeriod;
            } else if (Himawari8Ini.GetId().Equals(provider)) {
                return Himawari8.TilePeriod;
            } else if (YmyouliIni.GetId().Equals(provider)) {
                return Ymyouli.TilePeriod;
            } else if (QingbzIni.GetId().Equals(provider)) {
                return Qingbz.TilePeriod;
            } else if (WallhavenIni.GetId().Equals(provider)) {
                return Wallhaven.TilePeriod;
            } else if (WallhereIni.GetId().Equals(provider)) {
                return Wallhere.TilePeriod;
            } else if (WallpaperupIni.GetId().Equals(provider)) {
                return Wallpaperup.TilePeriod;
            } else if (ToopicIni.GetId().Equals(provider)) {
                return Toopic.TilePeriod;
            } else if (NetbianIni.GetId().Equals(provider)) {
                return Netbian.TilePeriod;
            } else if (BackieeIni.GetId().Equals(provider)) {
                return Backiee.TilePeriod;
            } else if (InfinityIni.GetId().Equals(provider)) {
                return Infinity.TilePeriod;
            } else if (ObzhiIni.GetId().Equals(provider)) {
                return Obzhi.TilePeriod;
            } else if (GluttonIni.GetId().Equals(provider)) {
                return Glutton.TilePeriod;
            } else if (LspIni.GetId().Equals(provider)) {
                return Lsp.TilePeriod;
            } else {
                return Bing.TilePeriod;
            }
        }

        override public string ToString() {
            string paras;
            if (LocalIni.GetId().Equals(provider)) {
                paras = Local.ToString();
            } else if (NasaIni.GetId().Equals(provider)) {
                paras = Nasa.ToString();
            } else if (OneplusIni.GetId().Equals(provider)) {
                paras = Oneplus.ToString();
            } else if (TimelineIni.GetId().Equals(provider)) {
                paras = Timeline.ToString();
            } else if (OneIni.GetId().Equals(provider)) {
                paras = One.ToString();
            } else if (Himawari8Ini.GetId().Equals(provider)) {
                paras = Himawari8.ToString();
            } else if (YmyouliIni.GetId().Equals(provider)) {
                paras = Ymyouli.ToString();
            } else if (QingbzIni.GetId().Equals(provider)) {
                paras = Qingbz.ToString();
            } else if (WallhavenIni.GetId().Equals(provider)) {
                paras = Qingbz.ToString();
            } else if (WallhereIni.GetId().Equals(provider)) {
                paras = Wallhere.ToString();
            } else if (WallpaperupIni.GetId().Equals(provider)) {
                paras = Wallpaperup.ToString();
            } else if (ToopicIni.GetId().Equals(provider)) {
                paras = Toopic.ToString();
            } else if (NetbianIni.GetId().Equals(provider)) {
                paras = Netbian.ToString();
            } else if (BackieeIni.GetId().Equals(provider)) {
                paras = Backiee.ToString();
            } else if (InfinityIni.GetId().Equals(provider)) {
                paras = Infinity.ToString();
            } else if (ObzhiIni.GetId().Equals(provider)) {
                paras = Obzhi.ToString();
            } else if (GluttonIni.GetId().Equals(provider)) {
                paras = Glutton.ToString();
            } else if (LspIni.GetId().Equals(provider)) {
                paras = Lsp.ToString();
            } else {
                paras = Bing.ToString();
            }
            return $"/{Provider}?desktopprovider={DesktopProvider}&lockprovider={LockProvider}&toastprovider={ToastProvider}&tileprovider={TileProvider}"
                + (paras.Length > 0 ? "&" : "") + paras;
        }
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

    public sealed class GluttonIni {
        private readonly List<string> ALBUMS = new List<string>() { "journal", "rank" };
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
}
