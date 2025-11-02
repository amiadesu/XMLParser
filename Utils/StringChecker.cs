using XMLParser.Constants;

namespace XMLParser.Utils;

public static class StringChecker
{
    public static bool IsError(string s)
    {
        return s == Literals.errorMessage || s == Literals.refErrorMessage;
    }
}