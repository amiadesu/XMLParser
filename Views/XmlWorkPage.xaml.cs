using Microsoft.Maui.Controls;
using System;
using XMLParser.Components.Logging;
using XMLParser.Constants;
using XMLParser.FileSystem;

namespace XMLParser.Views
{
    public partial class XmlWorkPage : ContentPage
    {
        private readonly string _xmlData;
        private readonly string _xslData;
        private readonly string _xmlName;

        private readonly XmlFileService _fileService = new();

        public XmlWorkPage(string xmlData, string xslData, string xmlName = Literals.defaultXmlFileName)
        {
            _xmlData = xmlData;
            _xslData = xslData;

            _xmlName = xmlName;

            InitializeComponent();
            BindingContext = new XmlWorkViewModel(_xmlData, _xslData);
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            SetLoading(true);
            await Shell.Current.Navigation.PushAsync(new StartingPage());
            SetLoading(false);
        }

        private async void OnSaveXmlClicked(object sender, EventArgs e)
        {
            if (BindingContext is not XmlWorkViewModel vm) return;
            SetLoading(true);

            string name = _xmlName;
            string result = await _fileService.SaveLocally(vm.FilteredXml, name);

            Logger.Instance.Info($"Спроба локально зберегти .xml файл {name} з результатом {result}");

            await DisplayAlert("Saved", result, "OK");
            SetLoading(false);
        }

        private async void OnSaveHtmlClicked(object sender, EventArgs e)
        {
            if (BindingContext is not XmlWorkViewModel vm) return;
            SetLoading(true);

            string html = vm.TransformToHtml();
            string htmlName = _xmlName + ".html";
            string result = await _fileService.SaveLocally(html, htmlName);

            Logger.Instance.Info($"Спроба локально зберегти .html файл {htmlName} з результатом {result}");

            await DisplayAlert("Saved", result, "OK");
            SetLoading(false);
        }

        private async void OnSaveXmlDriveClicked(object sender, EventArgs e)
        {
            if (BindingContext is not XmlWorkViewModel vm) return;
            SetLoading(true);

            await Shell.Current.Navigation.PushAsync(
                new GoogleDriveSaveXmlPage(vm.FilteredXml, _xmlName)
            );

            SetLoading(false);
        }

        private async void OnSaveHtmlDriveClicked(object sender, EventArgs e)
        {
            if (BindingContext is not XmlWorkViewModel vm) return;
            SetLoading(true);

            string html = vm.TransformToHtml();
            string htmlName = _xmlName + ".html";
            await Shell.Current.Navigation.PushAsync(new GoogleDriveSaveHtmlPage(html, htmlName));

            SetLoading(false);
        }

        private void SetLoading(bool isLoading)
        {
            LoadingIndicator.IsVisible = isLoading;
            LoadingIndicator.IsRunning = isLoading;
        }
    }
}
