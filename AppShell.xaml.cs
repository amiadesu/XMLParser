using Microsoft.Maui.Controls;
using XMLParser.Views;

namespace XMLParser;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(StartingPage), typeof(StartingPage));
		Routing.RegisterRoute(nameof(SpreadsheetPage), typeof(SpreadsheetPage));
	}
}

