using Microsoft.Maui.Controls;
using System;
using XMLParser.FileSystem;
using XMLParser.Utils;
using XMLParser.Resources.Localization;
using XMLParser.Services;
using XMLParser.Services.GoogleDrive;
using System.Threading.Tasks;

namespace XMLParser.Views;

public partial class GoogleDriveSavePage : ContentPage 
{
    readonly XmlFileService _tableFileService = new();
    private readonly GoogleDriveService _googleDriveService = new();
    private bool _initialized = false;
    private bool _authorized => _googleDriveService.IsSignedIn;

    private string _fileData = "";
    private string _fullFileName = "";
    private string _fileName
    {
        get
        {
            if (_fullFileName.EndsWith(".table"))
                return _fullFileName.Substring(0, _fullFileName.Length - 6); // 6 = length of ".table"
            return _fullFileName;
        }
        set
        {
            if (!value.EndsWith(".table"))
                _fullFileName = value + ".table";
            else
                _fullFileName = value;
        }
    }

    public GoogleDriveSavePage(string fileData, string fileName)
    {
        _fileData = fileData;
        _fileName = fileName;

        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized)
            return;

        _initialized = true;

        FileNameEntry.Text = _fileName;

        SetLoading(true);
        try
        {
            await _googleDriveService.Init();
            UpdateButtons();
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

    private async void OnReturnClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        await Shell.Current.Navigation.PushAsync(new SpreadsheetPage(_fileData, _fullFileName));
        SetLoading(false);
    }

    private async void OnSignInClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        await Authorize();
        SetLoading(false);
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        _fileName = FileNameEntry.Text;
        var result = await _tableFileService.SaveToGoogleDrive(_fileData, _googleDriveService, _fullFileName);
        SetLoading(false);

        await DisplayAlert(
            DataProcessor.FormatResource(
                AppResources.SavingResult
            ),
            result,
            DataProcessor.FormatResource(
                AppResources.OK
            )
        );
    }
    
    private async Task Authorize()
    {
        try
        {
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
            SaveButton.IsVisible = true;
        }
        else
        {
            SignInButton.Text = DataProcessor.FormatResource(
                AppResources.SignIn
            );
            SaveButton.IsVisible = false;
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