using System;

namespace XMLParser.Views
{
    public sealed class GoogleDriveSaveHtmlPage : GoogleDriveSavePage
    {
        public GoogleDriveSaveHtmlPage(string fileData, string fileName)
            : base(fileData, EnsureExtension(fileName), ".html")
        {
            Title = "Save HTML to Google Drive";
        }

        private static string EnsureExtension(string name)
        {
            if (!name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                name += ".html";
            return name;
        }
    }
}
