using System.Collections.Generic;

namespace XMLParser.Models;

public sealed record StudentModel(
    string FullName,
    string Faculty,
    string Department,
    string Specialty,
    string EventWindow,
    string ParliamentType,
    IReadOnlyDictionary<string, string> Attributes
);