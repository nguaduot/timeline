using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Profile;
using Windows.UI.Xaml;

namespace Timeline.Pages {
    class OsVerTrigger : StateTriggerBase {
        private int osVer = 11;
        public int OsVer {
            get { return osVer; }
            set {
                osVer = value;
                SetActive(GetOsVer() == osVer);
            }
        }

        public static int GetOsVer() {
            // Win11：10.0.22000.194
            ulong version = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
            ulong major = (version & 0xFFFF000000000000L) >> 48;
            ulong minor = (version & 0x0000FFFF00000000L) >> 32;
            ulong build = (version & 0x00000000FFFF0000L) >> 16;
            ulong revision = (version & 0x000000000000FFFFL);
            if (major > 10) {
                return 11;
            } else if (major == 10) {
                if (minor > 0) {
                    return 11;
                } else if (minor == 0) {
                    if (build > 22000) {
                        return 11;
                    } else if (build == 22000) {
                        if (revision >= 194) {
                            return 11;
                        }
                    }
                }
            }
            return 10;
        }
    }
}
