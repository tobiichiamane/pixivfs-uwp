using Windows.UI.Xaml.Markup;

namespace PixivFSUWP.Data
{
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public class LangExtension : MarkupExtension
    {
        public string ID { get; set; }

        public LangExtension() { }
        public LangExtension(string id) : this() => ID = id;
        protected override object ProvideValue() => string.IsNullOrEmpty(ID) ? null : (object)OverAll.GetResourceString(ID);
    }
}
