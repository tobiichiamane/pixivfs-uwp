using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Markup;

namespace PixivFSUWP.Data
{
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public class LangExtension : MarkupExtension
    {
        public LangExtension()
        {

        }
        public LangExtension(string id) : this()
        {
            ID = id;
        }

        public string ID { get; set; }
        protected override object ProvideValue()
        {
            return string.IsNullOrEmpty(ID) ? null : (object)GetResourceString(ID);
        }

        public static string GetResourceString(string ID) => Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString(ID);
    }
}
