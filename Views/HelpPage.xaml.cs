using Microsoft.Maui.Controls;
using System;
using XMLParser.FileSystem;
using XMLParser.Utils;
using XMLParser.Resources.Localization;

namespace XMLParser.Views;

public partial class HelpPage : ContentPage 
{
    public HelpPage()
    {
        InitializeComponent();
    }

    private async void OnHomePageClicked(object sender, EventArgs e)
    {
        SetLoading(true);
        await Shell.Current.Navigation.PushAsync(new StartingPage());
        SetLoading(false);
    }

    private void SetLoading(bool isLoading)
    {
        LoadingIndicator.IsRunning = isLoading;
        LoadingIndicator.IsVisible = isLoading;
    }
}