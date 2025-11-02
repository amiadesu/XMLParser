using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using XMLParser.Models;
using System;

namespace XMLParser.Strategies;

public sealed class LinqSearchStrategy : IXmlSearchStrategy
{
    public Task<IList<StudentModel>> SearchAsync(Stream xmlStream, string keyword,
        IReadOnlyDictionary<string, string> attributeFilters, CancellationToken ct = default)
    {
        if (xmlStream.CanSeek)
        {
            xmlStream.Seek(0, SeekOrigin.Begin);
        }
        
        var doc = XDocument.Load(xmlStream);
        var q = from s in doc.Descendants("student")
                let attrs = s.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value)
                where Matches(attrs, attributeFilters)
                let blob = string.Join(" ", new[]
                {
                    (string?)s.Element("fullname"),
                    (string?)s.Element("faculty"),
                    (string?)s.Element("department"),
                    (string?)s.Element("specialty"),
                    (string?)s.Element("eventWindow"),
                    (string?)s.Element("parliamentType")
                }.Where(x => !string.IsNullOrWhiteSpace(x)))
                where string.IsNullOrWhiteSpace(keyword) || blob.Contains(keyword!, StringComparison.OrdinalIgnoreCase)
                select new StudentModel(
                    (string?)s.Element("fullname") ?? "",
                    (string?)s.Element("faculty") ?? "",
                    (string?)s.Element("department") ?? "",
                    (string?)s.Element("specialty") ?? "",
                    (string?)s.Element("eventWindow") ?? "",
                    (string?)s.Element("parliamentType") ?? "",
                    attrs
                );
        return Task.FromResult<IList<StudentModel>>(q.ToList());
    }

    public Task<Dictionary<string, HashSet<string>>> InspectAttributesAsync(Stream xmlStream, CancellationToken ct = default)
    {
        var doc = XDocument.Load(xmlStream);
        var dict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in doc.Descendants("student"))
        {
            foreach (var a in s.Attributes())
            {
                if (!dict.TryGetValue(a.Name.LocalName, out var set)) dict[a.Name.LocalName] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                set.Add(a.Value);
            }
        }
        return Task.FromResult(dict);
    }

    private static bool Matches(Dictionary<string, string> attrs, IReadOnlyDictionary<string, string> filter)
    {
        if (filter.Count == 0) return true;
        foreach (var kv in filter)
            if (!attrs.TryGetValue(kv.Key, out var v) || !string.Equals(v, kv.Value, StringComparison.OrdinalIgnoreCase))
                return false;
        return true;
    }
}