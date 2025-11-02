using System.Globalization;
using XMLParser.Constants;

namespace XMLParser.Utils;

public static class DataProcessor
{
    public static double StringToDouble(string number)
    {
        return double.Parse(number, CultureInfo.InvariantCulture);
    }

    public static string FormatResource(string resource, params (string key, object value)[] parameters)
    {
        string result = resource;
        foreach (var (key, value) in parameters)
        {
            result = result.Replace($"{{{key}}}", value?.ToString() ?? "");
        }
        return result;
    }
}