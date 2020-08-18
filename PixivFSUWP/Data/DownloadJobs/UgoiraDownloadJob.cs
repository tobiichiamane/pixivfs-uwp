using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Provider;

namespace PixivFSUWP.Data.DownloadJobs
{
    [Serializable]
    public class UgoiraDownloadJob : DownloadJobPlus
    {
        private readonly PixivCS.Objects.UgoiraMetadata res;
        public UgoiraDownloadJob(string Title, string Uri, StorageFile File, PixivCS.Objects.UgoiraMetadata res) : base(Title, Uri, File)
        {
            this.res = res;
            if (string.IsNullOrEmpty(Uri))
                _ = SetStatus(DownloadJobStatus.Failed);
        }

        protected override async Task<FileUpdateStatus> WriteToFile(Stream zipfile)
        {
            var framefiles = new List<string>();
            var framedelays = new Dictionary<string, int>();
            var frameimgs = new Dictionary<string, SoftwareBitmap>();
            try
            {
                Ugoira ugoira = null;
                if (res.UgoiraMetadataUgoiraMetadata != null)
                {
                    var framesarray = res.UgoiraMetadataUgoiraMetadata.Frames;
                    foreach (var frame in framesarray)
                    {
                        var filePath = frame.File;
                        framefiles.Add(filePath);
                        framedelays.Add(filePath, (int)frame.Delay);

                    }
                    using (var ziparc = new ZipArchive(zipfile, ZipArchiveMode.Read))
                    {
                        foreach (var entry in ziparc.Entries)
                        {
                            var file = entry.FullName;
                            using (var memStream = new MemoryStream())
                            {
                                await entry.Open().CopyToAsync(memStream);
                                memStream.Position = 0;
                                var decoder = await BitmapDecoder.CreateAsync(memStream.AsRandomAccessStream());
                                frameimgs.Add(file, await decoder.GetSoftwareBitmapAsync());
                            }
                        }
                    }
                    ugoira = new Ugoira();
                    foreach (var i in framefiles)
                        ugoira.Frames.Add(new Ugoira.Frame() { Image = frameimgs[i], Delay = framedelays[i] });
                }
                using (var stream = await File.OpenAsync(FileAccessMode.ReadWrite))
                using (var renderer = new Lumia.Imaging.GifRenderer())
                {
                    renderer.Duration = ugoira.Frames[0].Delay;
                    renderer.Size = new Windows.Foundation.Size(ugoira.Frames[0].Image.PixelWidth, ugoira.Frames[0].Image.PixelHeight);
                    var sources = new List<Lumia.Imaging.IImageProvider>();
                    foreach (var img in ugoira.Frames)
                        sources.Add(new Lumia.Imaging.SoftwareBitmapImageSource(img.Image));
                    renderer.Sources = sources;
                    var asyncOperationWithProgress = stream.WriteAsync(await renderer.RenderAsync());
                    asyncOperationWithProgress.Progress = (_, progress) => Progress = (int)progress;
                    await asyncOperationWithProgress;
                }
                return FileUpdateStatus.Complete;
            }
            catch
            {
                return FileUpdateStatus.Failed;
            }
            finally
            {
                framefiles.Clear();
                framedelays.Clear();
                frameimgs.Clear();
            }
        }
    }
}