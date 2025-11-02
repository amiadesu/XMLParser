using System.Xml;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using XMLParser.Models;
using System;

namespace XMLParser.Strategies;

public sealed class DomSearchStrategy : IXmlSearchStrategy
{
    public async Task<IList<StudentModel>> SearchAsync(Stream xmlStream, string keyword,
        IReadOnlyDictionary<string, string> attributeFilters, CancellationToken ct = default)
    {
        if (xmlStream.CanSeek)
        {
            xmlStream.Seek(0, SeekOrigin.Begin);
        }

        var list = new List<StudentModel>();
        var doc = new XmlDocument();
        doc.Load(xmlStream);
        foreach (XmlElement student in doc.GetElementsByTagName("student"))
        {
            if (!Match(student, attributeFilters)) continue;

            var sm = await ReadStudent(student, keyword, ct);
            if (sm != null)
            {
                list.Add(sm);
            }
        }
        return list;
    }

    public Task<Dictionary<string, HashSet<string>>> InspectAttributesAsync(Stream xmlStream, CancellationToken ct = default)
    {
        var dict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var doc = new XmlDocument();
        doc.Load(xmlStream);
        foreach (XmlElement student in doc.GetElementsByTagName("student"))
        {
            foreach (XmlAttribute a in student.Attributes)
            {
                if (!dict.TryGetValue(a.Name, out var set)) dict[a.Name] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                set.Add(a.Value);
            }
        }
        return Task.FromResult(dict);
    }
    
    private Task<StudentModel?> ReadStudent(XmlElement student, string keyword, CancellationToken ct = default)
    {
        string text = string.Join(" ", student.ChildNodes.Cast<XmlNode>().Select(n => n.InnerText));
        if (!string.IsNullOrWhiteSpace(keyword) && !text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<StudentModel?>(null);
        }
        var attrs = student.Attributes.Cast<XmlAttribute>().ToDictionary(a => a.Name, a => a.Value);
        var sm = new StudentModel(
            student["fullname"]?.InnerText ?? "",
            student["faculty"]?.InnerText ?? "",
            student["department"]?.InnerText ?? "",
            student["specialty"]?.InnerText ?? "",
            student["eventWindow"]?.InnerText ?? "",
            student["parliamentType"]?.InnerText ?? "",
            attrs
        );
        return Task.FromResult<StudentModel?>(sm);
    }

    private static bool Match(XmlElement el, IReadOnlyDictionary<string, string> filter)
    {
        if (filter.Count == 0) return true;
        foreach (var kv in filter)
        {
            if (el.GetAttribute(kv.Key) is not string v || !string.Equals(v, kv.Value, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
