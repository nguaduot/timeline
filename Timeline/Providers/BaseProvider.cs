using Timeline.Beans;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Timeline.Utils;
using Windows.Graphics.Imaging;
using System.Linq;
using Windows.Media.FaceAnalysis;
using System.Threading;
using Windows.Storage.Streams;
using Windows.ApplicationModel;

namespace Timeline.Providers {
    public class BaseProvider {
        // 缓存图片量
        private const int POOL_CACHE = 4;

        // 索引顺序为「回顾」顺序
        // 注：并非都按时间降序排列，因图源配置而异
        protected readonly List<Meta> metas = new List<Meta>();

        //protected Dictionary<string, int> dicHistory = new Dictionary<string, int>();
        private readonly BackgroundDownloader downloader = new BackgroundDownloader();

        public string Id { set; get; }

        protected bool AppendMetas(List<Meta> metasAdd) {
            List<string> list = metas.Select(t => t.Id).ToList();
            foreach (Meta meta in metasAdd) {
                if (!list.Contains(meta.Id) && meta.IsValid()) {
                    metas.Add(meta);
                }
            }
            return metas.Count > list.Count;
        }

        //protected void SortMetas(List<Meta> metasAdd) {
        //    AppendMetas(metasAdd);
        //    string idFocus = GetFocus()?.Id;
        //    // 按日期降序排列
        //    metas.Sort((m1, m2) => {
        //        if (m1.SortFactor > m2.SortFactor) {
        //            return -1;
        //        }
        //        if (m1.SortFactor < m2.SortFactor) {
        //            return 1;
        //        }
        //        return m1.Id.CompareTo(m2.Id);
        //    });
        //    // 恢复当前索引
        //    if (indexFocus > 0) {
        //        for (int i = 0; i < metas.Count; i++) {
        //            if (metas[i].Id == idFocus) {
        //                indexFocus = i;
        //                break;
        //            }
        //        }
        //    }
        //}

        //protected void RandomMetas(List<Meta> metasAdd) {
        //    //List<Meta> metasNew = new List<Meta>();
        //    //Random random = new Random();
        //    //foreach (Meta meta in metas) {
        //    //    metasNew.Insert(random.Next(metasNew.Count + 1), meta);
        //    //}
        //    //metas.Clear();
        //    //metas.AddRange(metasNew);
        //    Random random = new Random();
        //    foreach (Meta meta in metasAdd) {
        //        dicHistory.TryGetValue(meta.Id, out int times);
        //        meta.SortFactor = random.NextDouble() + Math.Min(times / 10.0, 0.9);
        //    }
        //    // 升序排列，已阅图降低出现在前排的概率
        //    metasAdd.Sort((m1, m2) => m1.SortFactor.CompareTo(m2.SortFactor));
        //    AppendMetas(metasAdd);
        //}

        public virtual async Task<bool> LoadData(CancellationToken token, Ini ai, BaseIni bi, Go go) {
            //dicHistory.Clear();
            //Dictionary<string, int> dicNew = await FileUtil.GetHistoryAsync(Id);
            //foreach (var item in dicNew) {
            //    dicHistory.Add(item.Key, item.Value);
            //}
            await Task.Delay(100);
            return false;
        }

        public int GetCount() {
            return metas.Count;
        }

        public int GetCountNext(Meta meta) {
            int index = GetIndex(meta);
            return metas.Count > 0 ? metas.Count - (index + 1) : 0;
        }

        public int GetIndex(Meta meta) {
            for (int i = 0; i < metas.Count; ++i) {
                if (metas[i].Id.Equals(meta?.Id)) {
                    return i;
                }
            }
            return -1;
        }

        public Meta GetMeta(int index) {
            index = Math.Max(Math.Min(index, metas.Count - 1), 0);
            return metas.Count > 0 ? metas[index] : null;
        }

        public Meta GetNextMeta(Meta meta) {
            int index = GetIndex(meta);
            index = Math.Min(index + 1, metas.Count - 1);
            return metas.Count >= 0 ? metas[index] : null;
        }

        public Meta GetPrevMeta(Meta meta) {
            int index = GetIndex(meta);
            index = Math.Max(index - 1, 0);
            return metas.Count >= 0 ? metas[index] : null;
        }

        public int GetMaxIndex() {
            return metas.Count - 1;
        }

        public int GetMinNo() {
            return metas.Count > 0 ? metas.Min(x => x.No) : int.MaxValue;
        }

        public DateTime GetMinDate(bool Utc = false) {
            if (Utc) {
                return metas.Count > 0 ? metas.Min(x => x.Date).ToUniversalTime() : DateTime.UtcNow;
            }
            return metas.Count > 0 ? metas.Min(x => x.Date) : DateTime.Now;
        }

        public float GetMinScore() {
            return metas.Count > 0 ? metas.Min(x => x.Score) : int.MaxValue;
        }

        public List<Meta> GetMetas() {
            return metas.ToList();
        }

        public void ClearMetas() {
            metas.Clear();
        }

        public virtual async Task<Meta> CacheAsync(Meta meta, bool calFacePos, CancellationToken token) {
            LogUtil.D("CacheAsync() " + meta?.Uhd);
            if (meta == null) {
                return null;
            }
            // 缓存当前（等待）
            IReadOnlyList<DownloadOperation> downloading = await BackgroundDownloader.GetCurrentDownloadsAsync();
            LogUtil.D("CacheAsync() history " + downloading.Count);
            if (meta.CacheUhd == null) {
                string cacheName = string.Format("{0}-{1}{2}", Id, meta.Id, meta.Format);
                StorageFile cacheFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(cacheName, CreationCollisionOption.OpenIfExists);
                if ((await cacheFile.GetBasicPropertiesAsync()).Size > 0) { // 已缓存过
                    Debug.WriteLine("CacheAsync() cache from disk: " + cacheFile.Path);
                    meta.CacheUhd = cacheFile;
                } else if (meta.Uhd != null) { // 未缓存
                    Uri uriUhd = new Uri(meta.Uhd);
                    if (uriUhd.IsFile) { // 开始复制
                        Debug.WriteLine("CacheAsync() cache from file: " + cacheFile.Path);
                        StorageFile srcFile = await StorageFile.GetFileFromPathAsync(meta.Uhd);
                        await srcFile.CopyAndReplaceAsync(cacheFile);
                        meta.CacheUhd = cacheFile;
                    } else { // 开始缓存
                        if (meta.Do == null) { // 从历史中查找任务
                            foreach (DownloadOperation o in downloading) {
                                if (meta.Uhd.Equals(o.RequestedUri)) {
                                    meta.Do = o;
                                } else if (o.Progress.Status == BackgroundTransferStatus.Running) {
                                    try { // 暂停缓存池之外的任务
                                        o.Pause();
                                        LogUtil.D("CacheAsync() pause: " + meta.Uhd);
                                    } catch (System.InvalidOperationException e) {
                                        LogUtil.E("CacheAsync() " + e.Message);
                                    }
                                }
                            }
                        }
                        try {
                            long start = DateTime.Now.Ticks;
                            if (meta.Do != null) { // 从历史中恢复任务
                                Debug.WriteLine("CacheAsync() cache from history: " + meta.Uhd);
                                if (meta.Do.Progress.Status == BackgroundTransferStatus.PausedByApplication) {
                                    meta.Do.Resume();
                                }
                                await meta.Do.AttachAsync().AsTask(token);
                            } else { // 新建下载任务
                                Debug.WriteLine("CacheAsync() cache from network: " + meta.Uhd);
                                meta.Do = downloader.CreateDownload(uriUhd, cacheFile);
                                await meta.Do.StartAsync().AsTask(token);
                            }
                            LogUtil.D("CacheAsync() " + meta.Uhd + " " + meta.Do.Progress.Status + " " + (int)((DateTime.Now.Ticks - start) / 10000));
                            if (meta.Do.Progress.Status == BackgroundTransferStatus.Completed) {
                                meta.CacheUhd = meta.Do.ResultFile as StorageFile;
                            }
                        } catch (Exception e) {
                            meta.Do = null; // 置空，下次重新下载
                            // 情况1：链接404
                            // System.Exception: 未找到(404)。
                            // 情况2：任务被取消，此时该下载不再存在于 BackgroundDownloader.GetCurrentDownloadsAsync()
                            // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                            LogUtil.E("CacheAsync() " + e.Message);
                        }
                    }
                }
            }
            // 缓存后续（不等待）
            List<Meta> nextMetas = metas.Skip(GetIndex(meta) + 1).Take(POOL_CACHE).ToList();
            foreach (Meta m in nextMetas) {
                if (token.IsCancellationRequested || m.CacheUhd != null) { // 无需缓存
                    continue;
                }
                string cacheName = string.Format("{0}-{1}{2}", Id, m.Id, m.Format);
                StorageFile cacheFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(cacheName, CreationCollisionOption.OpenIfExists);
                if ((await cacheFile.GetBasicPropertiesAsync()).Size > 0) { // 已缓存过
                    m.CacheUhd = cacheFile;
                    Debug.WriteLine("CacheAsync() cache from disk: " + cacheFile.Path);
                } else if (m.Do != null) { // 下载中
                    if (m.Do.Progress.Status == BackgroundTransferStatus.PausedByApplication) {
                        m.Do.Resume();
                    }
                } else if (m.Uhd != null && !new Uri(m.Uhd).IsFile) { // 未下载
                    foreach (DownloadOperation o in downloading) { // 从历史中恢复任务
                        if (m.Uhd.Equals(o.RequestedUri)) {
                            Debug.WriteLine("CacheAsync() cache from history: " + m.Uhd);
                            m.Do = o;
                            if (m.Do.Progress.Status == BackgroundTransferStatus.PausedByApplication) {
                                m.Do.Resume();
                            }
                            break;
                        }
                    }
                    if (m.Do == null) { // 新建下载任务
                        Debug.WriteLine("CacheAsync() cache from network: " + m.Uhd);
                        m.Do = downloader.CreateDownload(new Uri(m.Uhd), cacheFile);
                        _ = m.Do.StartAsync();
                    }
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
                    LogUtil.E("CacheAsync() " + e.Message);
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
                    LogUtil.E("CacheAsync() " + ex.Message);
                } finally {
                    bitmap?.Dispose();
                }
            }
            return meta;
        }

        public async Task<StorageFile> DownloadAsync(StorageFolder folder, Meta meta, string provider) {
            if (meta?.CacheUhd == null) {
                return null;
            }
            string appName = AppInfo.Current.DisplayInfo.DisplayName;
            try {
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
