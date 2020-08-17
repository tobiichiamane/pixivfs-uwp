using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PixivFSUWP.Data.DownloadJobs;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PixivFSUWP.Converters
{
    // 用于 [暂停] 按钮
    public class DownloadJobStatus_Pause_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch ((DownloadJobStatus)value)
            {
                case DownloadJobStatus.Created:
                case DownloadJobStatus.Ready:
                case DownloadJobStatus.Running:
                    return Visibility.Visible;
                default:
                case DownloadJobStatus.Finished:
                case DownloadJobStatus.Cancel:
                case DownloadJobStatus.Failed:
                case DownloadJobStatus.Pause:
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
    // 用于 [继续] 键和重试键的转换器
    public class DownloadJobStatus_Continue_Retry_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch ((DownloadJobStatus)value)
            {
                default:
                case DownloadJobStatus.Created:
                case DownloadJobStatus.Ready:
                case DownloadJobStatus.Finished:
                case DownloadJobStatus.Running:
                    return Visibility.Collapsed;

                case DownloadJobStatus.Cancel:
                case DownloadJobStatus.Failed:
                case DownloadJobStatus.Pause:
                    return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
