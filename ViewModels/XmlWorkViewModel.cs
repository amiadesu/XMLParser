using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Xsl;
using System.Xml.XPath;
using XMLParser.Strategies;
using XMLParser.Models;
using XMLParser.Services.Serialization;
using Microsoft.Maui.ApplicationModel;
using System.Xml;
using XMLParser.Components.Logging;

namespace XMLParser.Views
{
    public class XmlWorkViewModel : BindableObject
    {
        private readonly string _xmlData;
        private readonly string _xslData;

        private readonly IXmlSearchStrategy _linq = new LinqSearchStrategy();
        private readonly IXmlSearchStrategy _dom = new DomSearchStrategy();
        private readonly IXmlSearchStrategy _sax = new SaxSearchStrategy();
        private IXmlSearchStrategy _strategy;

        private const double DebounceSeconds = 0.5;
        private CancellationTokenSource? _debounceCts;

        public string OriginalXml { get; private set; } = string.Empty;
        public string FilteredXml { get; private set; } = string.Empty;
        public string VisualText { get; private set; } = string.Empty;
        public bool IsVisualTextVisible => !string.IsNullOrEmpty(VisualText) && ResultLines.Count == 0;
        public bool IsResultsVisible => ResultLines.Count > 0;
        private byte[] _xmlBytes = Array.Empty<byte>();

        public ObservableCollection<string> ResultLines { get; } = new();
        public ObservableCollection<string> AttrKeys { get; } = new();
        public ObservableCollection<FilterItem> ActiveFilters { get; } = new();
        public ObservableCollection<string> Strategies { get; } = new() { "LINQ", "DOM", "SAX" };

        private string _keyword = string.Empty;
        public string Keyword
        {
            get => _keyword;
            set
            {
                if (_keyword == value) return;
                _keyword = value;
                OnPropertyChanged();
                DebounceSearch();
            }
        }

        private string _selectedAttrKey = string.Empty;
        public string SelectedAttrKey
        {
            get => _selectedAttrKey;
            set
            {
                if (_selectedAttrKey == value) return;
                _selectedAttrKey = value;
                OnPropertyChanged();
                NewAttrValue = string.Empty;
            }
        }

        private string _newAttrValue = string.Empty;
        public string NewAttrValue
        {
            get => _newAttrValue;
            set { _newAttrValue = value; OnPropertyChanged(); }
        }

        private string _selectedStrategy = "LINQ";
        public string SelectedStrategy
        {
            get => _selectedStrategy;
            set
            {
                if (_selectedStrategy == value) return;
                _selectedStrategy = value;
                _strategy = value switch
                {
                    "DOM" => _dom,
                    "SAX" => _sax,
                    _ => _linq
                };
                OnPropertyChanged();
                _ = ReinspectAttrsAndSearchAsync();
            }
        }

        public Dictionary<string, string> SelectedAttrFilters { get; } = new();

        public ICommand SearchCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand AddFilterCommand { get; }
        public ICommand RemoveFilterCommand { get; }

        public XmlWorkViewModel(string xmlData, string xslData)
        {
            _xmlData = xmlData;
            _xslData = xslData;

            _strategy = _linq;

            SearchCommand = new Command(async () => await SearchAsync());
            ClearCommand = new Command(Clear);
            AddFilterCommand = new Command(AddFilter);
            RemoveFilterCommand = new Command<string>(RemoveFilter);

            _ = InitializeAsync();
        }

        private void DebounceSearch()
        {
            _debounceCts?.Cancel();
            var localCts = new CancellationTokenSource();
            _debounceCts = localCts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(DebounceSeconds), localCts.Token);
                    if (!localCts.Token.IsCancellationRequested)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            try
                            {
                                await SearchAsync();
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.Error("Не вдалося застосувати пошук за ключовим словом", ex);
                            }
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    // Not an error, just cancellation
                }
            });
        }

        private async Task InitializeAsync()
        {
            _xmlBytes = Encoding.UTF8.GetBytes(_xmlData);
            OriginalXml = _xmlData;
            FilteredXml = OriginalXml;

            await ReinspectAttrsAsync();
            await SearchAsync();
        }

        private async Task ReinspectAttrsAndSearchAsync()
        {
            await ReinspectAttrsAsync();
            await SearchAsync();
        }

        private async Task ReinspectAttrsAsync()
        {
            using var ms = new MemoryStream(_xmlBytes);
            var attrs = await _strategy.InspectAttributesAsync(ms);
            AttrKeys.Clear();
            foreach (var k in attrs.Keys)
                AttrKeys.Add(k);

            if (!string.IsNullOrEmpty(SelectedAttrKey) && !AttrKeys.Contains(SelectedAttrKey))
                SelectedAttrKey = "";
        }

        public async Task SearchAsync()
        {
            if (_xmlBytes.Length == 0) return;

            LogFilters();

            using var ms = new MemoryStream(_xmlBytes);
            var items = await _strategy.SearchAsync(ms, Keyword, SelectedAttrFilters);

            UpdateVisuals(items);

            UpdateFilteredXml(items);
        }

        private void UpdateVisuals(IList<StudentModel>? items)
        {
            ResultLines.Clear();

            if (items == null || items.Count == 0)
            {
                VisualText = "Немає результатів.";
                FireVisualUpdates();
                return;
            }

            var sb = new StringBuilder();
            foreach (var item in items)
            {
                var card = BuildPrettyBlock(item);
                ResultLines.Add(card);
                sb.AppendLine(card).AppendLine();
            }
            VisualText = sb.ToString().TrimEnd();

            FireVisualUpdates();
        }

        private void FireVisualUpdates()
        {
            OnPropertyChanged(nameof(VisualText));
            OnPropertyChanged(nameof(IsVisualTextVisible));
            OnPropertyChanged(nameof(IsResultsVisible));
        }

        private void UpdateFilteredXml(IList<StudentModel>? items)
        {
            if (items == null || items.Count == 0)
            {
                FilteredXml = "<students/>";
                OnPropertyChanged(nameof(FilteredXml));
                return;
            }

            var built = StudentXmlSerializer.Serialize(items);
            FilteredXml = string.IsNullOrWhiteSpace(built) ? "<students/>" : built.Trim();

            OnPropertyChanged(nameof(FilteredXml));
        }

        private static string BuildPrettyBlock(dynamic item)
        {
            var title = item?.ToString() ?? "(item)";
            var attrs = (IDictionary<string, string>?)item?.Attributes ?? new Dictionary<string, string>();
            var sb = new StringBuilder();
            sb.AppendLine(title);
            foreach (var kv in attrs)
                sb.AppendLine($"  @{kv.Key} = {kv.Value}");
            return sb.ToString().TrimEnd();
        }

        private void AddFilter()
        {
            if (string.IsNullOrWhiteSpace(SelectedAttrKey) || string.IsNullOrWhiteSpace(NewAttrValue))
                return;

            SelectedAttrFilters[SelectedAttrKey] = NewAttrValue;

            var existing = ActiveFilters.FirstOrDefault(f => f.Key == SelectedAttrKey);
            if (existing != null) ActiveFilters.Remove(existing);

            ActiveFilters.Add(new FilterItem(SelectedAttrKey, NewAttrValue));
            _ = SearchAsync();
        }

        private void RemoveFilter(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            if (SelectedAttrFilters.Remove(key))
            {
                var chip = ActiveFilters.FirstOrDefault(f => f.Key == key);
                if (chip != null) ActiveFilters.Remove(chip);
                _ = SearchAsync();
            }
        }

        private void Clear()
        {
            Keyword = "";
            SelectedAttrKey = "";
            NewAttrValue = "";
            ActiveFilters.Clear();
            SelectedAttrFilters.Clear();

            FilteredXml = OriginalXml;
            _ = SearchAsync();
        }

        public string TransformToHtml()
        {
            try
            {
                var xmlBytes = Encoding.UTF8.GetBytes(FilteredXml);
                using var reader = new MemoryStream(xmlBytes);
                var xmlDoc = new XPathDocument(reader);

                var transform = new XslCompiledTransform();
                using (var xsltReader = XmlReader.Create(new StringReader(_xslData)))
                {
                    transform.Load(xsltReader);
                }

                using var sw = new StringWriter();
                transform.Transform(xmlDoc, null, sw);
                return sw.ToString();
            }
            catch (Exception ex)
            {
                Logger.Instance.Warn("Помилка при створенні .html документа", ex);
                return $"<!-- Transformation error: {ex.Message} -->";
            }
        }

        private void LogFilters()
        {
            var logMessageBuilder = new StringBuilder();
            logMessageBuilder.Append($"Фільтр за ключовим словом '{Keyword}' та {SelectedAttrFilters.Count} атрибутами:");
            foreach (var kv in SelectedAttrFilters)
            {
                logMessageBuilder.Append($"\n @{kv.Key}='{kv.Value}'");
            }

            Logger.Instance.Info(logMessageBuilder.ToString());
        }
    }

    public record FilterItem(string Key, string Value)
    {
        public string Display => $"{Key} = {Value}";
    }
}
