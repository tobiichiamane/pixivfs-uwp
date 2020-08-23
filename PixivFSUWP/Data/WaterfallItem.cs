using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace PixivFSUWP.Data
{
    public class WaterfallItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ImageUri { get; set; }
        public int Stars { get; set; }
        public int Pages { get; set; }
        public bool IsBookmarked { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Type { get; set; }
        public static WaterfallItem FromObject(PixivCS.Objects.UserPreviewIllust Source)
        {
            var toret = new WaterfallItem();
            toret.Id = (int)Source.Id;
            toret.Title = Source.Title;
            toret.Author = Source.User.Name;
            toret.ImageUri = Source.ImageUrls.Medium?.ToString() ?? "";
            toret.Stars = (int)Source.TotalBookmarks;
            toret.Pages = (int)Source.PageCount;
            toret.IsBookmarked = Source.IsBookmarked;
            toret.Width = (int)Source.Width;
            toret.Height = (int)Source.Height;
            toret.Type = Source.Type;
            return toret;
        }
    }
}
