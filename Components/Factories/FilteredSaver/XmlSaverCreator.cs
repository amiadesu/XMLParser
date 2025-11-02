using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace XMLParser.Factories.FilteredSaver;

public sealed class XmlSaveCreator : FilteredSaveCreator
{
    protected override IFilteredSaver CreateSaver() => new XmlSaver();

    private sealed class XmlSaver : IFilteredSaver
    {
        public Task SaveAsync(Stream output, IEnumerable<XmlElement> nodes, XDocument? sourceDoc, CancellationToken ct = default)
        {
            var doc = new XDocument(new XElement("students", nodes.Select(n => XElement.Parse(n.OuterXml))));
            doc.Save(output);
            return Task.CompletedTask;
        }
    }
}