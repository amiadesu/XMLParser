namespace XMLParser.Utils;

public static class CharChecker
{
    public static bool IsNotZeroDigit(char c)
    {
        return c >= '1' && c <= '9';
    }
    
    public static bool IsLatin(char c)
    {
        return IsLowerLatin(c) || IsUpperLatin(c);
    }

    public static bool IsLowerLatin(char c)
    {
        return c >= 'a' && c <= 'z';
    }

    public static bool IsUpperLatin(char c)
    {
        return c >= 'A' && c <= 'Z';
    }
}