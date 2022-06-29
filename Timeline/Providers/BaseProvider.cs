using Timeline.Beans;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Timeline.Utils;
using Windows.Graphics.Imaging;
using System.Linq;
using Windows.Media.FaceAnalysis;
using System.Threading;
using Windows.Storage.Streams;
using Windows.ApplicationModel;

namespace Timeline.Providers {
    public class BaseProvider {
        public string Id { set; get; }

        // 索引顺序为「回顾」顺序
        // 注：并非都按时间降序排列，因图源配置而异
        protected readonly List<Meta> metas = new List<Meta>();

        // 当前浏览索引
        protected int indexFocus = 0;

        protected Dictionary<string, int> dicHistory = new Dictionary<string, int>();

        private readonly BackgroundDownloader downloader = new BackgroundDownloader();
        private readonly Queue<DownloadOperation> activeDownloads = new Queue<DownloadOperation>();

        // 缓存图片量
        private const int POOL_CACHE = 5;

        protected void AppendMetas(List<Meta> metasAdd) {
            List<string> list = metas.Select(t => t.Id).ToList();
            foreach (Meta meta in metasAdd) {
                if (!list.Contains(meta.Id) && meta.IsValid()) {
                    metas.Add(meta);
                }
            }
        }

        protected void SortMetas(List<Meta> metasAdd) {
            AppendMetas(metasAdd);
            string idFocus = GetFocus()?.Id;
            // 按日期降序排列
            metas.Sort((m1, m2) => {
                if (m1.SortFactor > m2.SortFactor) {
                    return -1;
                }
                if (m1.SortFactor < m2.SortFactor) {
                    return 1;
                }
                return m1.Id.CompareTo(m2.Id);
            });
            // 恢复当前索引
            if (indexFocus > 0) {
                for (int i = 0; i < metas.Count; i++) {
                    if (metas[i].Id == idFocus) {
                        indexFocus = i;
                        break;
                    }
                }
            }
        }

        protected void RandomMetas(List<Meta> metasAdd) {
            //List<Meta> metasNew = new List<Meta>();
            //Random random = new Random();
            //foreach (Meta meta in metas) {
            //    metasNew.Insert(random.Next(metasNew.Count + 1), meta);
            //}
            //metas.Clear();
            //metas.AddRange(metasNew);
            Random random = new Random();
            foreach (Meta meta in metasAdd) {
                dicHistory.TryGetValue(meta.Id, out int times);
                meta.SortFactor = random.NextDouble() + Math.Min(times / 10.0, 0.9);
            }
            // 升序排列，已阅图降低出现在前排的概率
            metasAdd.Sort((m1, m2) => m1.SortFactor.CompareTo(m2.SortFactor));
            AppendMetas(metasAdd);
        }

        public virtual async Task<bool> LoadData(CancellationToken token, BaseIni bi, DateTime date = new DateTime()) {
            dicHistory.Clear();
            Dictionary<string, int> dicNew = await FileUtil.GetHistoryAsync(Id);
            foreach (var item in dicNew) {
                dicHistory.Add(item.Key, item.Value);
            }
            return false;
        }

        public int GetIndexFocus() {
            return indexFocus;
        }

        public async Task<Meta> Focus() {
            if (metas.Count == 0) {
                indexFocus = 0;
                return null;
            }
            if (indexFocus >= metas.Count - 1) {
                indexFocus = metas.Count - 1;
            } else if (indexFocus < 0) {
                indexFocus = 0;
            }

            // 更新沉底机制数据
            if (dicHistory.ContainsKey(metas[indexFocus].Id)) {
                dicHistory[metas[indexFocus].Id] += 1;
            } else {
                dicHistory[metas[indexFocus].Id] = 1;
            }
            await FileUtil.SaveHistoryAsync(Id, dicHistory);

            return metas[indexFocus];
        }

        public Meta GetFocus() {
            if (metas.Count == 0) {
                return null;
            }

            int index = 0;
            if (indexFocus >= metas.Count - 1) {
                index = metas.Count - 1;
            } else if (indexFocus >= 0) {
                index = indexFocus;
            }
            return metas[index];
        }

        public async Task<Meta> Yesterday() {
            if (metas.Count == 0) {
                indexFocus = 0;
                return null;
            }
            if (indexFocus >= metas.Count - 1) {
                indexFocus = metas.Count - 1;
            } else if (indexFocus >= 0) {
                indexFocus++;
            } else {
                indexFocus = 0;
            }

            // 更新沉底机制数据
            if (dicHistory.ContainsKey(metas[indexFocus].Id)) {
                dicHistory[metas[indexFocus].Id] += 1;
            } else {
                dicHistory[metas[indexFocus].Id] = 1;
            }
            await FileUtil.SaveHistoryAsync(Id, dicHistory);

            return metas[indexFocus];
        }

        public Meta GetYesterday() {
            if (metas.Count == 0) {
                return null;
            }
            int index = 0;
            if (indexFocus >= metas.Count - 1) {
                index = metas.Count - 1;
            } else if (indexFocus >= 0) {
                index = indexFocus + 1;
            }
            return metas[index];
        }

        public Meta Tomorrow() {
            if (metas.Count == 0) {
                indexFocus = 0;
                return null;
            }
            if (indexFocus >= metas.Count) {
                indexFocus = metas.Count - 1;
            } else if (indexFocus > 0) {
                indexFocus--;
            } else {
                indexFocus = 0;
            }
            return metas[indexFocus];
        }

        public Meta GetTomorrow() {
            if (metas.Count == 0) {
                return null;
            }
            int index = 0;
            if (indexFocus >= metas.Count) {
                index = metas.Count - 1;
            } else if (indexFocus > 0) {
                index = indexFocus - 1;
            }
            return metas[index];
        }

        public Meta Index(int index) {
            if (metas.Count == 0) {
                indexFocus = 0;
                return null;
            }
            if (index >= metas.Count) {
                indexFocus = metas.Count - 1;
            } else if (index >= 0) {
                indexFocus = index;
            } else {
                indexFocus = 0;
            }
            return metas[indexFocus];
        }

        public Meta GetIndex(int index) {
            if (metas.Count == 0) {
                return null;
            }
            if (index >= metas.Count) {
                index = metas.Count - 1;
            } else if (index < 0) {
                index = 0;
            }
            return metas[index];
        }

        public Meta Target(DateTime date) {
            for (int i = 0; i < metas.Count; i++) {
                if (date.ToString("yyyyMMddHHmm").Equals(metas[i].Date.ToString("yyyyMMddHHmm"))) {
                    indexFocus = i;
                    return metas[i];
                }
            }
            return null;
        }

        public List<Meta> GetMetas(int count) {
            List<Meta> metasNew = new List<Meta>();
            for (int i = 0; i < count && i < metas.Count; ++i) {
                metasNew.Add(metas[i]);
            }
            return metasNew;
        }

        public virtual async Task<Meta> CacheAsync(Meta meta, bool calFacePos, CancellationToken token) {
            LogUtil.D("Cache() " + meta?.Id);
            int index = -1;
            for (int i = 0; i < metas.Count; i++) { // 定位索引以便缓存多个
                if (meta != null && metas[i].Id.Equals(meta.Id)) {
                    index = i;
                    break;
                }
            }
            if (index < 0) {
                return null;
            }
            IReadOnlyList<DownloadOperation> historyDownloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            LogUtil.D("Cache() current downloads " + historyDownloads.Count);
            for (int i = index; i < Math.Min(metas.Count, index + POOL_CACHE); i++) { // 缓存多个
                Meta m = metas[i];
                if (m.CacheUhd != null) { // 无需缓存
                    continue;
                }
                string cacheName = string.Format("{0}-{1}{2}", Id, m.Id, m.Format);
                StorageFile cacheFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(cacheName, CreationCollisionOption.OpenIfExists);
                BasicProperties fileProperties = await cacheFile.GetBasicPropertiesAsync();
                if (fileProperties.Size > 0) { // 已缓存过
                    m.CacheUhd = cacheFile;
                    LogUtil.D("Cache() cached from disk: " + cacheFile.Path);
                } else if (m.Uhd != null) {
                    Uri uriUhd = new Uri(m.Uhd);
                    if (uriUhd.IsFile) { // 开始复制
                        StorageFile srcFile = await StorageFile.GetFileFromPathAsync(m.Uhd);
                        await srcFile.CopyAndReplaceAsync(cacheFile);
                        meta.CacheUhd = cacheFile;
                    } else if (m.Do == null) { // 开始缓存
                        LogUtil.D("Cache() cache from network: " + m.Uhd);
                        foreach (DownloadOperation o in historyDownloads) { // 从历史中恢复任务
                            if (m.Uhd.Equals(o.RequestedUri)) {
                                m.Do = o;
                                break;
                            }
                        }
                        if (m.Do == null) { // 新建下载任务
                            //try {
                            m.Do = downloader.CreateDownload(uriUhd, cacheFile);
                            _ = m.Do.StartAsync();
                            //} catch (Exception e) {
                            //    LogUtil.E("Cache() " + e.Message);
                            //}
                        }
                        if (activeDownloads.Count >= 5) { // 暂停缓存池之外的任务
                            DownloadOperation o = activeDownloads.Dequeue();
                            if (o.Progress.Status == BackgroundTransferStatus.Running) {
                                o.Pause();
                            }
                        }
                        activeDownloads.Enqueue(m.Do);
                    }
                }
            }
            // 等待当前任务下载完成
            if (meta.CacheUhd == null && meta.Do != null) {
                LogUtil.D("Cache() wait for cache: " + meta.Do.Guid);
                try {
                    if (meta.Do.Progress.Status == BackgroundTransferStatus.PausedByApplication) {
                        meta.Do.Resume();
                    }
                    _ = await meta.Do.AttachAsync().AsTask(token);
                    LogUtil.D("Cache() " + meta.Do.Progress.Status + " " + meta.Id);
                    if (meta.Do.Progress.Status == BackgroundTransferStatus.Completed) {
                        meta.CacheUhd = meta.Do.ResultFile as StorageFile;
                    }
                } catch (Exception e) {
                    meta.Do = null; // 置空，下次重新下载
                    // 情况1：链接404
                    // System.Exception: 未找到(404)。
                    // 情况2：任务被取消，此时该下载不再存在于 BackgroundDownloader.GetCurrentDownloadsAsync()
                    // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                    LogUtil.E("Cache() " + e.Message);
                }
            }
            // 获取图片尺寸（耗时短）
            if (meta.Dimen.Width == 0 && meta.CacheUhd != null) {
                try {
                    using (IRandomAccessStream stream = await meta.CacheUhd.OpenAsync(FileAccessMode.Read)) {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                        meta.Dimen = new Windows.Foundation.Size(decoder.PixelWidth, decoder.PixelHeight);
                    }
                } catch (Exception e) {
                    LogUtil.E("Cache() " + e.Message);
                }
            }
            if (token.IsCancellationRequested) {
                return meta;
            }
            // 检测人脸位置（耗时较长）
            if (calFacePos && meta.FacePos == null && meta.CacheUhd != null && FaceDetector.IsSupported) {
                long start = DateTime.Now.Ticks;
                meta.FacePos = new List<Windows.Foundation.Point>();
                SoftwareBitmap bitmap = null;
                try {
                    using (IRandomAccessStream stream = await meta.CacheUhd.OpenAsync(FileAccessMode.Read)) {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                        // TODO: 该行会随机抛出异常 System.Exception: 图像无法识别
                        bitmap = await decoder.GetSoftwareBitmapAsync(decoder.BitmapPixelFormat,
                            BitmapAlphaMode.Premultiplied, new BitmapTransform(),
                            ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
                        if (bitmap.BitmapPixelFormat != BitmapPixelFormat.Gray8) {
                            bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Gray8);
                        }
                        if (token.IsCancellationRequested) {
                            return meta;
                        }
                        FaceDetector detector = await FaceDetector.CreateAsync();
                        IList<DetectedFace> faces = await detector.DetectFacesAsync(bitmap);
                        foreach (DetectedFace face in faces) {
                            meta.FacePos.Add(new Windows.Foundation.Point {
                                X = face.FaceBox.X + face.FaceBox.Width / 2.0f,
                                Y = face.FaceBox.Y + face.FaceBox.Height / 2.0f
                            });
                        }
                        LogUtil.D("detect face cost: " + (int)((DateTime.Now.Ticks - start) / 10000));
                    }
                } catch (Exception ex) {
                    LogUtil.E("Cache() " + ex.Message);
                } finally {
                    bitmap?.Dispose();
                }
            }
            return meta;
        }

        public async Task<StorageFile> DownloadAsync(Meta meta, string provider) {
            if (meta?.CacheUhd == null) {
                return null;
            }
            string appName = AppInfo.Current.DisplayInfo.DisplayName;
            try {
                StorageFolder folder = await KnownFolders.PicturesLibrary.CreateFolderAsync(appName,
                    CreationCollisionOption.OpenIfExists);
                string name = string.Format("{0}_{1}_{2}{3}", appName, provider, meta.Id, meta.Format);
                name = FileUtil.MakeValidFileName(name, "");
                return await meta.CacheUhd.CopyAsync(folder, name, NameCollisionOption.ReplaceExisting);
            } catch (Exception e) {
                LogUtil.E("DownloadAsync() " + e.Message);
            }
            return null;
        }
    }
}
