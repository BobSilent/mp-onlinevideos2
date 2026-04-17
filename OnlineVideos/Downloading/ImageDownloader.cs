using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineVideos.Downloading
{
    public static class ImageDownloader
    {
        [Serializable]
        public struct ResizeOptions
        {
            public static ResizeOptions Default
            {
                get
                {
                    return new ResizeOptions
                    {
                        MaxSize = 500,
                        Compositing = CompositingQuality.AssumeLinear,
                        Interpolation = InterpolationMode.High,
                        Smoothing = SmoothingMode.HighQuality
                    };
                }
            }
            public int MaxSize;
            public CompositingQuality Compositing;
            public InterpolationMode Interpolation;
            public SmoothingMode Smoothing;
        }

        // Cancels in-progress GetImages / DownloadImages calls.
        private static CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>Cancels any in-progress image download batch.</summary>
        public static void StopDownloads()
        {
            _cts.Cancel();
        }

        // Shared client for thumbnail downloads — reuses TCP connections across parallel download groups.
        private static readonly HttpClient _thumbClient = new HttpClient(
            new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true
            })
        {
            DefaultRequestHeaders =
            {
                UserAgent = { ProductInfoHeaderValue.Parse(OnlineVideoSettings.Instance.UserAgent) },
                Accept = { new MediaTypeWithQualityHeaderValue("*/*") }
            },
            Timeout = TimeSpan.FromSeconds(5)
        };

        /// <summary>
        /// Downloads images from the <see cref="SearchResultItem.Thumb"/> on thread-pool threads
        /// and sets the path of the downloaded image to the <see cref="SearchResultItem.ThumbnailImage"/>.
        /// Up to 5 images are downloaded concurrently. Cancel by calling <see cref="StopDownloads"/>.
        /// </summary>
        /// <typeparam name="T">must be a <see cref="SearchResultItem"/></typeparam>
        /// <param name="items">list of <see cref="SearchResultItem"/>s to download images for</param>
        public static void GetImages<T>(IList<T> items) where T : SearchResultItem
        {
            // Replace the token so a fresh call always starts uncanelled,
            // even if a previous StopDownload was issued.
            var oldCts = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
            oldCts.Dispose();
            var token = _cts.Token;

            Task.Run(() =>
            {
                try
                {
                    Parallel.ForEach(
                        items,
                        new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = token },
                        item => DownloadImageForItem(item, token));
                }
                catch (OperationCanceledException) { /* intentional stop */ }
            });
        }

        /// <summary>
        /// Downloads images for all items in <paramref name="items"/> on the calling thread.
        /// Called directly by <see cref="LatestVideosManager"/> for its own sequenced loop.
        /// </summary>
        public static void DownloadImages<T>(List<T> items, CancellationToken cancellationToken = default) where T : SearchResultItem
        {
            foreach (T item in items)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                DownloadImageForItem(item, cancellationToken);
            }
        }

        /// <summary>Downloads and assigns a thumbnail for a single item.</summary>
        private static void DownloadImageForItem<T>(T item, CancellationToken cancellationToken) where T : SearchResultItem
        {
            if (cancellationToken.IsCancellationRequested || string.IsNullOrEmpty(item.Thumb))
            {
                return;
            }

            foreach (string url in item.Thumb.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                string imageLocation = string.Empty;

                if (Uri.TryCreate(url, UriKind.Absolute, out Uri temp))
                {
                    if (temp.IsFile)
                    {
                        if (File.Exists(url))
                        {
                            imageLocation = url;
                        }
                    }
                    else
                    {
                        string thumbFile = string.IsNullOrEmpty(item.ThumbnailImage) ? Helpers.FileUtils.GetThumbFile(url) : item.ThumbnailImage;
                        if (File.Exists(thumbFile))
                        {
                            imageLocation = thumbFile;
                        }
                        else if (DownloadAndCheckImage(url, thumbFile, item.ImageForcedAspectRatio))
                        {
                            imageLocation = thumbFile;
                        }
                    }
                }

                // stop with the first valid image
                if (imageLocation != string.Empty)
                {
                    item.ThumbnailImage = imageLocation;
                    break;
                }
            }
        }

        public static bool DownloadAndCheckImage(string url, string file, float? forcedAspectRatio = null)
        {
            try
            {
                if (forcedAspectRatio != null && forcedAspectRatio.Value == 0.0f)
                {
                    forcedAspectRatio = null; // don't use 0.0 but null
                }

                using (var response = _thumbClient.GetAsync(url).GetAwaiter().GetResult())
                using (var responseStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                {
                    System.Drawing.Image image = Image.FromStream(responseStream, true, true);
                    // resample if needed
                    float imageAspectRatio = image.Width / (float)image.Height;
                    if (image.Width > OnlineVideoSettings.Instance.ThumbsResizeOptions.MaxSize || image.Height > OnlineVideoSettings.Instance.ThumbsResizeOptions.MaxSize
                        || (forcedAspectRatio != null && Math.Abs(forcedAspectRatio.Value - imageAspectRatio) > 0.1))
                    {
                        int iWidth = Math.Min(image.Width, OnlineVideoSettings.Instance.ThumbsResizeOptions.MaxSize);
                        int iHeight = Math.Min(image.Height, OnlineVideoSettings.Instance.ThumbsResizeOptions.MaxSize);

                        if (forcedAspectRatio != null && Math.Abs(forcedAspectRatio.Value - imageAspectRatio) > 0.1)
                        {
                            imageAspectRatio = forcedAspectRatio.Value;
                        }

                        if (image.Width > image.Height)
                        {
                            iHeight = (int)Math.Floor(iWidth / imageAspectRatio);
                        }
                        else
                        {
                            iWidth = (int)Math.Floor(imageAspectRatio * iHeight);
                        }

                        Bitmap tmp = new Bitmap(iWidth, iHeight, image.PixelFormat);
                        using (Graphics g = Graphics.FromImage(tmp))
                        {
                            g.CompositingQuality = OnlineVideoSettings.Instance.ThumbsResizeOptions.Compositing;
                            g.InterpolationMode = OnlineVideoSettings.Instance.ThumbsResizeOptions.Interpolation;
                            g.SmoothingMode = OnlineVideoSettings.Instance.ThumbsResizeOptions.Smoothing;
                            g.DrawImage(image, new Rectangle(0, 0, iWidth, iHeight));
                            image.Dispose();
                            image = tmp;
                        }
                    }
                    if (image.RawFormat.Guid == System.Drawing.Imaging.ImageFormat.Gif.Guid && file.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                    {
                        image.Save(file, System.Drawing.Imaging.ImageFormat.Gif);
                    }
                    else if (image.RawFormat.Guid == System.Drawing.Imaging.ImageFormat.Png.Guid && file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        image.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    else
                    {
                        image.Save(file, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                    image.Dispose();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Info("Invalid Image: '{0}' {1}", url, ex.Message);
                return false;
            }
        }

        public static void DeleteOldThumbs(int maxAge, Func<byte, bool> progressCallback)
        {
            int thumbsDeleted = 0;
            try
            {
                DateTime keepdate = DateTime.Now.AddDays(-maxAge);
                string cacheDir = Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"Cache\");

                // Count first so we can report meaningful progress without loading all FileInfo into memory.
                int total = 0;
                foreach (var _ in Directory.EnumerateFiles(cacheDir))
                {
                    total++;
                }

                Log.Info("Checking {0} thumbnails for age.", total);

                int processed = 0;
                foreach (string path in Directory.EnumerateFiles(cacheDir))
                {
                    var f = new FileInfo(path);
                    if (f.LastWriteTime <= keepdate)
                    {
                        f.Delete();
                        thumbsDeleted++;
                    }
                    processed++;
                    if (!progressCallback(total > 0 ? (byte)((float)processed / total * 100) : (byte)0))
                    {
                        break;
                    }
                }
            }
            catch (Exception threadException)
            {
                Log.Error(threadException);
            }
            finally
            {
                Log.Info("Deleted {0} thumbnails.", thumbsDeleted);
                progressCallback(byte.MaxValue);
            }
        }
    }
}
