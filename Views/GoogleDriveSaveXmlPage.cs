using System;

namespace XMLParser.Views
{
    public sealed class GoogleDriveSaveXmlPage : GoogleDriveSavePage
    {
        public GoogleDriveSaveXmlPage(string fileData, string fileName)
            : base(fileData, EnsureExtension(fileName), ".xml")
        {
            Title = "Save XML to Google Drive";
        }

        private static string EnsureExtension(string name)
        {
            if (!name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                name += ".xml";
            return name;
        }
    }
}
