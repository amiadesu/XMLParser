using System;
using XMLParser.Constants;

namespace XMLParser.Utils;

public static class DoubleChecker
{
    /// <summary>
    /// Checks if two doubles are equal with a tolerance equal to value stored 
    /// inside <b>XMLParser.Utils.Constants.comparisonTolerance</b> variable.
    /// </summary>
    public static bool Equal(double x, double y)
    {
        return Math.Abs(x - y) < Literals.comparisonTolerance;
    }
}