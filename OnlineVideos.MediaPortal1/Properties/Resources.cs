// Hand-written replacement for the auto-generated Resources.Designer.cs.
// Bitmaps are loaded directly from embedded manifest resources (Resources\*.png)
// instead of going through a .resx / ResourceManager indirection.

namespace OnlineVideos.MediaPortal1.Properties
{
    using System.Drawing;
    using System.Reflection;

    internal static class Resources
    {
        // Assembly that contains the embedded PNGs.
        private static readonly Assembly _assembly = typeof(Resources).Assembly;

        /// <summary>Loads a PNG embedded as <c>OnlineVideos.MediaPortal1.Resources.{filename}</c>.</summary>
        private static Bitmap Load(string filename)
        {
            string resourceName = "OnlineVideos.MediaPortal1.Resources." + filename;
            using (var stream = _assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new System.InvalidOperationException("Embedded resource not found: " + resourceName);
                return new Bitmap(stream);
            }
        }

        internal static Bitmap Add                   => Load("Add.png");
        internal static Bitmap cache                 => Load("cache.png");
        internal static Bitmap ClearFiltering        => Load("clear-filter.png");
        internal static Bitmap ColumnFilterIndicator => Load("filter-icons3.png");
        internal static Bitmap CreateSite            => Load("CreateSite.png");
        internal static Bitmap delete                => Load("Delete.png");
        internal static Bitmap Down                  => Load("Down.png");
        internal static Bitmap edit                  => Load("edit.png");
        internal static Bitmap Filtering             => Load("filter.png");
        internal static Bitmap flash                 => Load("flash.png");
        internal static Bitmap help                  => Load("help.png");
        internal static Bitmap Import                => Load("Import.png");
        internal static Bitmap ImportGlobal          => Load("ImportGlobal.png");
        internal static Bitmap ImportXml             => Load("ImportXml.png");
        internal static Bitmap key                   => Load("key.png");
        internal static Bitmap Latest                => Load("Latest.png");
        internal static Bitmap NewFolderHS           => Load("NewFolder.png");
        internal static Bitmap NewReport             => Load("NewReport.png");
        internal static Bitmap PublishToWeb          => Load("PublishToWeb.png");
        internal static Bitmap rss                   => Load("rss.png");
        internal static Bitmap Save                  => Load("Save.png");
        internal static Bitmap search                => Load("search.png");
        internal static Bitmap SortAscending         => Load("sort-ascending.png");
        internal static Bitmap SortDescending        => Load("sort-descending.png");
        internal static Bitmap thumbnail             => Load("thumbnail.png");
        internal static Bitmap timeout               => Load("timeout.png");
        internal static Bitmap tv                    => Load("tv.png");
        internal static Bitmap Up                    => Load("Up.png");
    }
}