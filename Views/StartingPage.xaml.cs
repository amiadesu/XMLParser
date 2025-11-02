using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XMLParser.Constants;
using XMLParser.FileSystem;
using XMLParser.Services.GoogleDrive;

namespace XMLParser.Views
{
    public partial class StartingPage : ContentPage
    {
        private readonly IGoogleDriveService _driveService = new GoogleDriveService();
        private readonly XmlFileService _fileService = new XmlFileService();

        private bool _initialized;
        private bool _driveAuthorized;

        private bool _xmlDriveMode;
        private bool _xslDriveMode;

        private string? _xmlPath;
        private string? _xmlName;
        private string? _xslPath;

        public StartingPage() => InitializeComponent();

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_initialized) return;
            _initialized = true;

            SetLoading(true);
            await InitDriveAsync();
            UpdateTabsUi();
            await ReloadXmlHistoryAsync();
            await ReloadXslHistoryAsync();
            UpdateContinueState();
            SetLoading(false);
        }

        private async Task InitDriveAsync()
        {
            try
            {
                await _driveService.Init();
                _driveAuthorized = _driveService.IsSignedIn;
            }
            catch
            {
                _driveAuthorized = false;
            }
        }

        private async void OnAuthorizeClicked(object sender, EventArgs e)
        {
            SetLoading(true);
            try
            {
                await _driveService.SignIn();
                _driveAuthorized = _driveService.IsSignedIn;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Google Drive", ex.Message, "OK");
            }
            UpdateTabsUi();
            await ReloadXmlHistoryAsync();
            await ReloadXslHistoryAsync();
            SetLoading(false);
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            SetLoading(true);
            try
            {
                await _driveService.SignOut();
                _driveAuthorized = false;

                _xmlDriveMode = false;
                _xslDriveMode = false;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Google Drive", ex.Message, "OK");
            }
            UpdateTabsUi();
            await ReloadXmlHistoryAsync();
            await ReloadXslHistoryAsync();
            SetLoading(false);
        }

        private async void OnHelpClicked(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PushAsync(new HelpPage());
        }

        private void UpdateTabsUi()
        {
            AuthorizeButton.IsVisible = !_driveAuthorized;
            LogoutButton.IsVisible = _driveAuthorized;
            UserEmailLabel.Text = _driveAuthorized ? _driveService.Email : "";

            XmlDriveButton.IsEnabled = _driveAuthorized;
            XslDriveButton.IsEnabled = _driveAuthorized;

            XmlLocalButton.BackgroundColor = Color.FromArgb("#7C3AED");
            XmlDriveButton.BackgroundColor = Color.FromArgb("#7C3AED");
            XmlLocalButton.Opacity = _xmlDriveMode ? 1.0 : 0.6;
            XmlDriveButton.Opacity = _xmlDriveMode ? 0.6 : 1.0;

            XslLocalButton.BackgroundColor = Color.FromArgb("#7C3AED");
            XslDriveButton.BackgroundColor = Color.FromArgb("#7C3AED");
            XslLocalButton.Opacity = _xslDriveMode ? 1.0 : 0.6;
            XslDriveButton.Opacity = _xslDriveMode ? 0.6 : 1.0;
        }

        private void OnXmlLocalClicked(object sender, EventArgs e)
        {
            _xmlDriveMode = false;
            UpdateTabsUi();
            _ = ReloadXmlHistoryAsync();
        }

        private void OnXmlDriveClicked(object sender, EventArgs e)
        {
            if (!_driveAuthorized)
            {
                DisplayAlert("Google Drive", "Please authorize first.", "OK");
                return;
            }
            _xmlDriveMode = true;
            UpdateTabsUi();
            _ = ReloadXmlHistoryAsync();
        }

        private async void OnPickXmlClicked(object sender, EventArgs e)
        {
            SetLoading(true);
            try
            {
                if (_xmlDriveMode && _driveAuthorized)
                    await PickFromDriveAsync("xml");
                else
                    await PickLocalAsync("xml");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            UpdateContinueState();
            SetLoading(false);
        }

        private async Task ReloadXmlHistoryAsync()
        {
            XmlRecentContainer.Children.Clear();
            XmlRecentContainer.Children.Add(new Label { Text = "XML History", FontAttributes = FontAttributes.Bold });

            var key = _xmlDriveMode ? "xml-drive" : "xml-local";
            var entries = await FileHistoryService.LoadEntriesAsync(key);
            foreach (var (name, path) in entries)
            {
                var btn = Utils.UIGenerator.GenerateRecentFileButton(name, ref XmlRecentContainer);
                btn.Clicked += (s, e) =>
                {
                    _xmlPath = path;
                    XmlSelectedLabel.Text = _xmlDriveMode ? $"Selected (Drive): {name}" : $"Selected: {name}";
                    UpdateContinueState();
                };
            }
        }

        private void OnXslLocalClicked(object sender, EventArgs e)
        {
            _xslDriveMode = false;
            UpdateTabsUi();
            _ = ReloadXslHistoryAsync();
        }

        private void OnXslDriveClicked(object sender, EventArgs e)
        {
            if (!_driveAuthorized)
            {
                DisplayAlert("Google Drive", "Please authorize first.", "OK");
                return;
            }
            _xslDriveMode = true;
            UpdateTabsUi();
            _ = ReloadXslHistoryAsync();
        }

        private async void OnPickXslClicked(object sender, EventArgs e)
        {
            SetLoading(true);
            try
            {
                if (_xslDriveMode && _driveAuthorized)
                    await PickFromDriveAsync("xsl");
                else
                    await PickLocalAsync("xsl");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            UpdateContinueState();
            SetLoading(false);
        }

        private async Task ReloadXslHistoryAsync()
        {
            XslRecentContainer.Children.Clear();
            XslRecentContainer.Children.Add(new Label { Text = "XSL History", FontAttributes = FontAttributes.Bold });

            var key = _xslDriveMode ? "xsl-drive" : "xsl-local";
            var entries = await FileHistoryService.LoadEntriesAsync(key);
            foreach (var (name, path) in entries)
            {
                var btn = Utils.UIGenerator.GenerateRecentFileButton(name, ref XslRecentContainer);
                btn.Clicked += (s, e) =>
                {
                    _xslPath = path;
                    XslSelectedLabel.Text = _xslDriveMode ? $"Selected (Drive): {name}" : $"Selected: {name}";
                    UpdateContinueState();
                };
            }
        }

        private async Task PickLocalAsync(string kind)
        {
            var ext = kind == "xml" ? ".xml" : ".xsl";
            var (file, error) = await XmlFileService.PickFile($"Select {kind.ToUpper()} file", ext);
            if (file == null)
            {
                if (!string.IsNullOrEmpty(error))
                    await DisplayAlert("Error", error, "OK");
                return;
            }

            if (kind == "xml")
            {
                _xmlPath = file.FullPath;
                _xmlName = Path.GetFileName(_xmlName);
                XmlSelectedLabel.Text = $"Selected: {file.FileName}";
                await FileHistoryService.AddEntryAsync("xml-local", file.FileName, _xmlPath);
                await ReloadXmlHistoryAsync();
            }
            else
            {
                _xslPath = file.FullPath;
                XslSelectedLabel.Text = $"Selected: {file.FileName}";
                await FileHistoryService.AddEntryAsync("xsl-local", file.FileName, _xslPath);
                await ReloadXslHistoryAsync();
            }
        }

        private async Task PickFromDriveAsync(string kind)
        {
            if (!_driveAuthorized)
            {
                await DisplayAlert("Google Drive", "Please authorize first.", "OK");
                return;
            }

            var filter = kind == "xml" ? new[] { ".xml" } : new[] { ".xsl" };
            var files = await _driveService.GetFiles(filter);
            if (files == null || files.Count == 0)
            {
                await DisplayAlert("Google Drive", $"No {kind.ToUpper()} files found.", "OK");
                return;
            }

            var names = files.Select(f => f.Name).ToArray();
            var chosen = await DisplayActionSheet($"Choose {kind.ToUpper()} file", "Cancel", null, names);
            if (string.IsNullOrEmpty(chosen) || chosen == "Cancel") return;

            var file = files.First(f => f.Name == chosen);
            if (kind == "xml")
            {
                _xmlPath = file.Id;
                _xmlName = file.Name;
                XmlSelectedLabel.Text = $"Selected (Drive): {file.Name}";
                await FileHistoryService.AddEntryAsync("xml-drive", file.Name, file.Id);
                await ReloadXmlHistoryAsync();
            }
            else
            {
                _xslPath = file.Id;
                XslSelectedLabel.Text = $"Selected (Drive): {file.Name}";
                await FileHistoryService.AddEntryAsync("xsl-drive", file.Name, file.Id);
                await ReloadXslHistoryAsync();
            }
        }

        private void UpdateContinueState()
        {
            ContinueButton.IsEnabled = !string.IsNullOrEmpty(_xmlPath) && !string.IsNullOrEmpty(_xslPath);
            ContinueButton.Opacity = ContinueButton.IsEnabled ? 1.0 : 0.6;
        }

        private async void OnContinueClicked(object sender, EventArgs e)
        {
            string xmlData;
            if (_xmlDriveMode)
            {
                xmlData = await _fileService.LoadFromGoogleDrive(_xmlPath!, _driveService);
            }
            else
            {
                xmlData = _fileService.LoadFromPath(_xmlPath!);
            }
            string xslData;
            if (_xslDriveMode)
            {
                xslData = await _fileService.LoadFromGoogleDrive(_xslPath!, _driveService);
            }
            else
            {
                xslData = _fileService.LoadFromPath(_xslPath!);
            }

            await Shell.Current.Navigation.PushAsync(new XmlWorkPage(xmlData, xslData, _xmlName ?? Literals.defaultXmlFileName));
        }

        private async void OnClearHistoryClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Confirm", "Are you sure you want to clear the file history?", "Yes", "No");
            if (!confirm) return;

            SetLoading(true);
            await FileHistoryService.ClearHistoryAsync();
            await ReloadXmlHistoryAsync();
            await ReloadXslHistoryAsync();
            SetLoading(false);
        }

        private void SetLoading(bool isLoading)
        {
            LoadingIndicator.IsVisible = isLoading;
            LoadingIndicator.IsRunning = isLoading;
        }
    }
}
