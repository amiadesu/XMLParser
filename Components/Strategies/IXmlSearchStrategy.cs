using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using XMLParser.Models;

namespace XMLParser.Strategies;

public interface IXmlSearchStrategy
{
    Task<IList<StudentModel>> SearchAsync(Stream xmlStream, string keyword,
                                            IReadOnlyDictionary<string, string> attributeFilters,
                                            CancellationToken ct = default);

    Task<Dictionary<string, HashSet<string>>> InspectAttributesAsync(Stream xmlStream, CancellationToken ct = default);
}