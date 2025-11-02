using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace XMLParser.Utils;

public static class UIGenerator
{
    public static Button GenerateRecentFileButton(string fileName, ref VerticalStackLayout verticalStackLayout)
    {
        var button = new Button
        {
            Text = fileName,
            BackgroundColor = Colors.Gray,
            HeightRequest = 40
        };

        verticalStackLayout.Children.Add(button);

        return button;
    }
}