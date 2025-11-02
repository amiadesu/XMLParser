using XMLParser.Services.GoogleDrive;
using XMLParser.FileSystem;
using XMLParser.Utils;
using XMLParser.Resources.Localization;
using System;
using Microsoft.Maui.Controls;

namespace XMLParser.Views
{
    public sealed class GoogleDriveSaveHtmlPage : GoogleDriveSavePage
    {
        public GoogleDriveSaveHtmlPage(string fileData, string fileName)
            : base(fileData, EnsureExtension(fileName))
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
