using XMLParser.Constants;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace XMLParser.Utils;

public static class UIGenerator
{
    public static Label GenerateColumnHeader(string name, ref StackLayout columnLayout)
    {
        var header = new Label
        {
            Text = name,
            WidthRequest = Literals.cellWidth,
            HeightRequest = Literals.cellHeight,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = ColorConstants.columnLabelColor,
            BackgroundColor = ColorConstants.columnLabelBackgroundColor,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        columnLayout.Children.Add(header);

        return header;
    }

    public static Label GenerateRowHeader(string name, ref StackLayout rowLayout)
    {
        var header = new Label
        {
            Text = name,
            WidthRequest = Literals.cellWidth / 2.5,
            HeightRequest = Literals.cellHeight,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = ColorConstants.rowLabelColor,
            BackgroundColor = ColorConstants.rowLabelBackgroundColor,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        rowLayout.Children.Add(header);

        return header;
    }

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