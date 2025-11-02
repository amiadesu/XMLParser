using Microsoft.Maui.Controls;
using System;
using XMLParser.FileSystem;
using System.Threading.Tasks;
using XMLParser.Utils;
using XMLParser.Resources.Localization;

namespace XMLParser.Views;

public partial class StartingPage : ContentPage
{
    private bool _initialized = false;

    public StartingPage()
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
        await GenerateRecentFileObjects();
        SetLoading(false);
    }

    private async void OnOpenSpreadsheetClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        await Shell.Current.Navigation.PushAsync(new SpreadsheetPage());
        SetLoading(false);
    }

    private async void OnHelpPageClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        await Shell.Current.Navigation.PushAsync(new HelpPage());
        SetLoading(false);
    }

    private async void OnOpenFilesClicked(object sender, EventArgs e)
    {
        var result = await XmlFileService.PickTable(
            DataProcessor.FormatResource(
                AppResources.PickTable
            )
        );
        if (result.result is not null)
        {
            await OpenFile(result.result.FileName, result.result.FullPath);
        }
        else if (!string.IsNullOrEmpty(result.errorMessage))
        {
            await DisplayAlert(
                DataProcessor.FormatResource(
                    AppResources.Error
                ),
                result.errorMessage,
                DataProcessor.FormatResource(
                    AppResources.OK
                )
            );
        }
    }

    private async void OnClearRecentFilesClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        await FileHistoryService.ClearHistoryAsync();
        await GenerateRecentFileObjects();
        SetLoading(false);
    }

    private async Task OpenFile(string fileName, string filePath)
    {
        SetLoading(true);
        await FileHistoryService.AddEntryAsync(fileName, filePath);
        await Shell.Current.Navigation.PushAsync(new SpreadsheetPage(filePath, fileName));
        SetLoading(false);
    }

    private async Task GenerateRecentFileObjects()
    {
        var entries = await FileHistoryService.LoadEntriesAsync();
        if (entries.Count == 0)
        {
            NoRecentFilesLabel.IsVisible = true;
            RecentItemsContainer.IsVisible = false;
        }
        else
        {
            NoRecentFilesLabel.IsVisible = false;
            RecentItemsContainer.IsVisible = true;

            foreach (var entry in entries)
            {
                var button = UIGenerator.GenerateRecentFileButton(entry.FileName, ref RecentItemsContainer);

                button.Clicked += (async (sender, e) => await OpenFile(entry.FileName, entry.FilePath));
            }
        }
    }

    private async void OnViewGoogleDriveFilesClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        await Shell.Current.Navigation.PushAsync(new GoogleDriveFilesPage());
        SetLoading(false);
    }
    
    private void SetLoading(bool isLoading)
    {
        LoadingIndicator.IsRunning = isLoading;
        LoadingIndicator.IsVisible = isLoading;
    }
}