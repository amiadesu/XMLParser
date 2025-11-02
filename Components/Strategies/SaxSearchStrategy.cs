using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using XMLParser.Models;
using System;

namespace XMLParser.Strategies;

public sealed class SaxSearchStrategy : IXmlSearchStrategy
{
    public async Task<IList<StudentModel>> SearchAsync(Stream xmlStream, string keyword,
        IReadOnlyDictionary<string, string> attributeFilters,
        CancellationToken ct = default)
    {
        if (xmlStream.CanSeek)
        {
            xmlStream.Seek(0, SeekOrigin.Begin);
        }

        var events = new List<StudentModel>();
        using var reader = XmlReader.Create(xmlStream, new XmlReaderSettings { Async = true, IgnoreWhitespace = true });

        while (await reader.ReadAsync())
        {
            if (ct.IsCancellationRequested) break;

            var sm = await ReadStudent(reader, keyword, ct);
            if (sm != null && MatchAttrs(sm.Attributes.ToDictionary(), attributeFilters))
            {
                events.Add(sm);
            }
        }
        return events;
    }

    public async Task<Dictionary<string, HashSet<string>>> InspectAttributesAsync(Stream xmlStream, CancellationToken ct = default)
    {
        var dict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        using var reader = XmlReader.Create(xmlStream, new XmlReaderSettings { Async = true, IgnoreWhitespace = true });
        while (await reader.ReadAsync())
        {
            if (ct.IsCancellationRequested) break;
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "student" && reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (!dict.TryGetValue(reader.Name, out var set))
                        dict[reader.Name] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    set.Add(reader.Value);
                }
                reader.MoveToElement();
            }
        }
        return dict;
    }
    
    private async Task<StudentModel?> ReadStudent(XmlReader reader, string keyword, CancellationToken ct = default)
    {
        if (reader.NodeType != XmlNodeType.Element || reader.Name != "student")
        {
            return null;
        }
                
        Dictionary<string, string> attrs = new();
        if (reader.HasAttributes)
        {
            while (reader.MoveToNextAttribute())
                attrs[reader.Name] = reader.Value;
            reader.MoveToElement();
        }

        string? fullName = null, faculty = null, department = null, specialty = null, window = null, parType = null;

        using var subTree = reader.ReadSubtree();
        var sub = XDocument.Load(subTree);
        fullName = sub.Root?.Element("fullname")?.Value;
        faculty = sub.Root?.Element("faculty")?.Value;
        department = sub.Root?.Element("department")?.Value;
        specialty = sub.Root?.Element("specialty")?.Value;
        window = sub.Root?.Element("eventWindow")?.Value;
        parType = sub.Root?.Element("parliamentType")?.Value;

        var blob = string.Join(" ", new[] { fullName, faculty, department, specialty, window, parType }.Where(s => !string.IsNullOrWhiteSpace(s)));

        if (!string.IsNullOrWhiteSpace(keyword) && !blob.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new StudentModel(fullName ?? "", faculty ?? "", department ?? "", specialty ?? "", window ?? "", parType ?? "", new Dictionary<string, string>(attrs));
    }

    private static bool MatchAttrs(Dictionary<string, string> nodeAttrs, IReadOnlyDictionary<string, string> filter)
    {
        if (filter.Count == 0) return true;
        foreach (var kv in filter)
        {
            if (!nodeAttrs.TryGetValue(kv.Key, out var v) || !string.Equals(v, kv.Value, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}