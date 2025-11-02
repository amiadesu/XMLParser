using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace XMLParser.Factories.FilteredSaver;

public interface IFilteredSaver
{
    Task SaveAsync(Stream output, IEnumerable<XmlElement> nodes, XDocument? sourceDoc = null, CancellationToken ct = default);
}