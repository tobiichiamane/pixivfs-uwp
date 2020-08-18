using System;
using System.IO;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Provider;

namespace PixivFSUWP.Data.DownloadJobs
{
    // 直接传StorageFile对象的...可以避免用FileSavePicker选中的文件传过来没有权限的问题
    // 对FileSavePicker选择的文件直接修改不需要对应的文件系统访问权限
    [Serializable]
    public class DownloadJobPlus : DownloadJob
    {
        protected readonly StorageFile File;

        public DownloadJobPlus(string Title, string Uri, StorageFile File) : base(Title, Uri, File.Path) => this.File = File;

        protected override async Task<FileUpdateStatus> WriteToFile(Stream memStream)
        {
            CachedFileManager.DeferUpdates(File);
            using (var fileStream = await File.OpenAsync(FileAccessMode.ReadWrite))
                await memStream.CopyToAsync(fileStream.AsStream());
            return await CachedFileManager.CompleteUpdatesAsync(File);
        }
    }
}