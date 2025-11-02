namespace XMLParser.Constants;

public static class Literals
{
    public const string errorMessage = "#ERROR";
    public const string refErrorMessage = "#REF";
    public const string defaultFileName = "result.xml";
    public const string defaultHistoryFileName = "file_history.txt";
    public static readonly string[] supportedExtensions = { ".xml", ".xsl" };

    public const int cellWidth = 100;
    public const int cellHeight = 25;

    // 1e-13 because we store 12 digits after the dot in our spreadsheet
    public const double comparisonTolerance = 1e-13;

    public const long authBufferSeconds = 10;
    public const string authURL = "https://accounts.google.com/o/oauth2/v2/auth";
    public const int authFlowPort = 42138;
}