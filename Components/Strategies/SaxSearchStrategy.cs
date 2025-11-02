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
        var events = new List<StudentModel>();
        using var reader = XmlReader.Create(xmlStream, new XmlReaderSettings { Async = true, IgnoreWhitespace = true });

        string? fullName = null, faculty = null, department = null, specialty = null, window = null, parType = null;
        Dictionary<string, string> attrs = new();

        while (await reader.ReadAsync())
        {
            if (ct.IsCancellationRequested) break;

            if (reader.NodeType == XmlNodeType.Element && reader.Name == "student")
            {
                attrs.Clear();
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                        attrs[reader.Name] = reader.Value;
                    reader.MoveToElement();
                }

                if (!MatchAttrs(attrs, attributeFilters))
                {
                    await reader.SkipAsync();
                    continue;
                }

                fullName = faculty = department = specialty = window = parType = null;

                using var subTree = reader.ReadSubtree();
                var sub = XDocument.Load(subTree);
                fullName = sub.Root?.Element("fullname")?.Value;
                faculty = sub.Root?.Element("faculty")?.Value;
                department = sub.Root?.Element("department")?.Value;
                specialty = sub.Root?.Element("specialty")?.Value;
                window = sub.Root?.Element("eventWindow")?.Value;
                parType = sub.Root?.Element("parliamentType")?.Value;
                
                var blob = string.Join(" ", new[] { fullName, faculty, department, specialty, window, parType }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (string.IsNullOrWhiteSpace(keyword) || blob.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    events.Add(new StudentModel(fullName ?? "", faculty ?? "", department ?? "", specialty ?? "", window ?? "", parType ?? "", new Dictionary<string, string>(attrs)));
                }
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