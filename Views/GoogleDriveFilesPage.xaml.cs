using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using System;
using XMLParser.FileSystem;
using System.Threading.Tasks;
using XMLParser.Utils;
using XMLParser.Resources.Localization;
using XMLParser.Services.GoogleDrive;
using XMLParser.Services;
using System.Diagnostics;
using XMLParser.Constants;

namespace XMLParser.Views;

public partial class GoogleDriveFilesPage : ContentPage
{
    readonly XmlFileService _tableFileService = new();
    readonly GoogleDriveService _googleDriveService = new();
    private bool _initialized = false;
    private bool _authorized => _googleDriveService.IsSignedIn;

    public GoogleDriveFilesPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized)
            return;

        _initialized = true;

        SetLoading(true);
        try
        {
            await _googleDriveService.Init();
            UpdateButtons();

            if (_googleDriveService.IsSignedIn)
            {
                await GenerateGoogleDriveFileObjects();
            }
        }
        catch (Exception e)
        {
            ShowError(
                DataProcessor.FormatResource(
                    AppResources.Error
                ),
                e.Message
            );
        }
        SetLoading(false);
    }

    private async void OnHomePageClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        await Shell.Current.Navigation.PushAsync(new StartingPage());
        SetLoading(false);
    }

    private async Task OpenFile(string fileName, string fileId)
    {
        SetLoading(true);
        try
        {
            var spreadsheet = await _tableFileService.LoadFromGoogleDrive(fileId, _googleDriveService);
            await Shell.Current.Navigation.PushAsync(new SpreadsheetPage(spreadsheet, fileName));
        }
        catch (Exception e)
        {
            Trace.TraceError($"Exception while downloading file: {e}");
            ShowError(
                DataProcessor.FormatResource(
                    AppResources.Error
                ),
                e.Message
            );
        }
        SetLoading(false);
    }

    private async Task GenerateGoogleDriveFileObjects()
    {
        try
        {
            var files = await _googleDriveService.GetFiles(Literals.supportedExtensions);

            if (files.Count == 0)
            {
                NoGoogleDriveFilesLabel.IsVisible = true;
                GoogleDriveItemsContainer.IsVisible = false;
            }
            else
            {
                NoGoogleDriveFilesLabel.IsVisible = false;
                GoogleDriveItemsContainer.IsVisible = true;

                foreach (var file in files)
                {
                    var button = UIGenerator.GenerateRecentFileButton(file.Name, ref GoogleDriveItemsContainer);

                    button.Clicked += (async (sender, e) => await OpenFile(file.Name, file.Id));
                }
            }
        }
        catch (Exception e)
        {
            ShowError(
                DataProcessor.FormatResource(
                    AppResources.Error
                ),
                e.Message
            );
        }
    }

    private async void OnSignInClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        await Authorize();
        SetLoading(false);
    }

    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        GoogleDriveItemsContainer.Clear();
        await GenerateGoogleDriveFileObjects();
        SetLoading(false);
    }

    private async Task Authorize()
    {
        try
        {
            GoogleDriveItemsContainer.Clear();
            if (!_authorized)
            {
                await _googleDriveService.SignIn();
            }
            else
            {
                await _googleDriveService.SignOut();
            }
        }
        catch (Exception e)
        {
            ShowError(
                DataProcessor.FormatResource(
                    AppResources.Error
                ),
                e.Message
            );
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (_authorized)
        {
            SignInButton.Text = DataProcessor.FormatResource(
                AppResources.SignOut,
                ("Email", _googleDriveService.Email ?? "")
            );
            UpdateButton.IsVisible = true;
        }
        else
        {
            SignInButton.Text = DataProcessor.FormatResource(
                AppResources.SignIn
            );
            UpdateButton.IsVisible = false;
        }
    }

    private void ShowError(string title, string message)
    {
        DisplayAlert(
            title,
            message,
            DataProcessor.FormatResource(
                AppResources.OK
        ));
    }

    private void SetLoading(bool isLoading)
    {
        LoadingIndicator.IsRunning = isLoading;
        LoadingIndicator.IsVisible = isLoading;
    }
}