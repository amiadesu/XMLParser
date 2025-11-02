using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace XMLParser.Factories.FilteredSaver;

public sealed class HtmlSaveCreator : FilteredSaveCreator
{
    private readonly string _xsltPath;
    public HtmlSaveCreator(string xsltPath) => _xsltPath = xsltPath;
    protected override IFilteredSaver CreateSaver() => new HtmlSaver(_xsltPath);

    private sealed class HtmlSaver : IFilteredSaver
    {
        private readonly string _xsltPath;
        public HtmlSaver(string xsltPath) => _xsltPath = xsltPath;
        public Task SaveAsync(Stream output, IEnumerable<XmlElement> nodes, XDocument? sourceDoc, CancellationToken ct = default)
        {
            var doc = new XDocument(new XElement("students", nodes.Select(n => XElement.Parse(n.OuterXml))));
            var tmp = new MemoryStream();
            doc.Save(tmp);
            tmp.Position = 0;

            var xslt = new XslCompiledTransform();
            xslt.Load(_xsltPath);
            using var reader = XmlReader.Create(tmp);
            using var writer = new StreamWriter(output, leaveOpen: true);
            using var xw = XmlWriter.Create(writer, xslt.OutputSettings);
            xslt.Transform(reader, xw);
            writer.Flush();
            return Task.CompletedTask;
        }
    }
}