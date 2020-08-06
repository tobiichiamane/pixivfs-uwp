using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;

using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace PixivFSUWP.Converters
{
    public class StringToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string str)
            {
                var ftask = StorageFile.GetFileFromPathAsync(str).AsTask();
                ftask.Wait();
                var file = ftask.GetAwaiter().GetResult();
                if (file != null)
                {
                    var fstask = file.OpenAsync(FileAccessMode.Read).AsTask();
                    fstask.Wait();
                    var fs = fstask.GetAwaiter().GetResult();
                    var bitmap = new BitmapImage();
                    bitmap.SetSource(fs);
                    return bitmap;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => null;
    }
}
