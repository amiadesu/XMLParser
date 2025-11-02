using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using XMLParser.Services;
using XMLParser.FileSystem;
using XMLParser.Utils;
using XMLParser.Resources.Localization;
using XMLParser.Constants;
using Microsoft.Maui.ApplicationModel;

namespace XMLParser.Views;

public partial class SpreadsheetPage : ContentPage
{
    private bool _initialized = false;
    readonly XmlFileService _tableFileService = new();
    private readonly string _fileName = Literals.defaultFileName;

    private string _fileData = "";

    public SpreadsheetPage()
    {
        InitializeComponent();
    }

    public SpreadsheetPage(string fileData, string tableFileName)
    {
        InitializeComponent();

        _fileData = fileData;
        _fileName = tableFileName;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized)
            return;

        _initialized = true;

        DisplayedText.Text = _fileData;

        this.Title = _fileName;

        if (Application.Current is not null && Application.Current.Windows.Count > 0)
        {
            Application.Current.Windows[0].Title = DataProcessor.FormatResource(
                AppResources.ApplicationNameEditingFile,
                ("FileName", _fileName)
            );
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (Application.Current is not null && Application.Current.Windows.Count > 0)
        {
            Application.Current.Windows[0].Title = DataProcessor.FormatResource(
                AppResources.ApplicationName,
                ("FileName", _fileName)
            );
        }
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        bool result = await OpenConfirmation();
        if (result)
        {
            SetLoading(true);
            await Shell.Current.Navigation.PushAsync(new StartingPage());
            SetLoading(false);
        }
            
    }

    private async void OnHelpClicked(object sender, EventArgs e)
    {
        bool result = await OpenConfirmation();
        if (result)
        {
            SetLoading(true);
            await Shell.Current.Navigation.PushAsync(new HelpPage());
            SetLoading(false);
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        string result = await _tableFileService.SaveLocally(_fileData, _fileName);
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

    private async void OnGoogleDriveSaveClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        await Shell.Current.Navigation.PushAsync(new GoogleDriveSavePage(_fileData, _fileName));
        SetLoading(false);
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

    private async Task<bool> OpenConfirmation()
    {
        bool result = await DisplayAlert(
            DataProcessor.FormatResource(
                AppResources.ActionConfirmation
            ),
            DataProcessor.FormatResource(
                AppResources.CurrentDataWillBeLost
            ),
            DataProcessor.FormatResource(
                AppResources.Yes
            ),
            DataProcessor.FormatResource(
                AppResources.No
            )
        );

        return result;
    }

    private void SetLoading(bool isLoading)
    {
        LoadingIndicator.IsRunning = isLoading;
        LoadingIndicator.IsVisible = isLoading;
    }
}