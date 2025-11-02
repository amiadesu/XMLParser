using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace XMLParser.Factories.FilteredSaver;

public abstract class FilteredSaveCreator
{
    public async Task SaveAsync(Stream output, IEnumerable<XmlElement> nodes, XDocument? sourceDoc = null, CancellationToken ct = default)
    {
        var saver = CreateSaver();
        await saver.SaveAsync(output, nodes, sourceDoc, ct);
    }
    protected abstract IFilteredSaver CreateSaver();
}