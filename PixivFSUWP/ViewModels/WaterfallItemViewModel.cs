using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PixivFSUWP.Data;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;
using System.ComponentModel;

namespace PixivFSUWP.ViewModels
{
    public class WaterfallItemViewModel : INotifyPropertyChanged
    {
        public int ItemId { get; private set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ImageUri { get; set; }
        public int Stars { get; set; }
        public int Pages { get; set; }
        public bool IsBookmarked { get; set; }
        public BitmapImage ImageSource { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public bool IsManga { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadImageAsync()
        {
            ImageSource = await Data.OverAll.LoadImageAsync(ImageUri);
        }

        public string StarsString => "★" + Stars.ToString();

        public static WaterfallItemViewModel FromItem(WaterfallItem Item)
            => new WaterfallItemViewModel()
            {
                ItemId = Item.Id,
                Title = Item.Title,
                Author = Item.Author,
                ImageUri = Item.ImageUri,
                IsBookmarked = Item.IsBookmarked,
                Stars = Item.Stars,
                Pages = Item.Pages,
                Width = Item.Width,
                Height = Item.Height,
                IsManga = Item.Type.Equals("manga")
            };

        public void NotifyChange(string PropertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
    }
}
