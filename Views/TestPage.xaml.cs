using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using XMLParser.Models;
using XMLParser.Strategies;

namespace XMLParser.Views
{
    public partial class TestPage : ContentPage
    {
        public TestPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
        }
    }

    public sealed class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        private readonly IXmlSearchStrategy _strategy = new LinqSearchStrategy();

        private byte[]? _xmlBytes;

        public ObservableCollection<StudentModel> Results { get; } = new();

        public ObservableCollection<string> ResultLines { get; } = new();

        public ObservableCollection<string> AttrKeys { get; } = new();
        public ObservableCollection<FilterItem> ActiveFilters { get; } = new();
        public Dictionary<string, string> SelectedAttrFilters { get; } = new();

        private string _keyword = string.Empty;
        public string Keyword { get => _keyword; set { _keyword = value; OnPropertyChanged(); } }

        private string _selectedAttrKey = string.Empty;
        public string SelectedAttrKey { get => _selectedAttrKey; set { _selectedAttrKey = value; OnPropertyChanged(); } }

        private string _newAttrValue = string.Empty;
        public string NewAttrValue { get => _newAttrValue; set { _newAttrValue = value; OnPropertyChanged(); } }

        public ICommand LoadSampleCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand AddFilterCommand { get; }
        public ICommand RemoveFilterCommand { get; }

        public MainViewModel()
        {
            LoadSampleCommand = new Command(async () => await LoadSampleAsync());
            SearchCommand = new Command(async () => await SearchAsync(), () => _xmlBytes != null);
            ClearCommand = new Command(Clear);
            AddFilterCommand = new Command(AddFilter);
            RemoveFilterCommand = new Command<string>(RemoveFilter);
        }

        private async Task LoadSampleAsync()
        {
            const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<students>
  <student id=""1"" group=""A"" year=""3"">
    <fullname>Іваненко Іван</fullname>
    <faculty>ФКНК</faculty>
    <department>Кібернетика</department>
  </student>
  <student id=""2"" group=""B"">
    <fullname>Петренко Марія</fullname>
    <faculty>ФКНК</faculty>
    <department>Інформатика</department>
  </student>
  <student id=""3"" group=""A"" dorm=""7"">
    <fullname>Сидоренко Олег</fullname>
    <faculty>ФІТ</faculty>
    <department>Програмна інженерія</department>
  </student>
</students>";

            _xmlBytes = Encoding.UTF8.GetBytes(xml);

            using var ms = new MemoryStream(_xmlBytes);
            var attrs = await _strategy.InspectAttributesAsync(ms);
            AttrKeys.Clear();
            if (attrs != null)
                foreach (var k in attrs.Keys.OrderBy(k => k)) AttrKeys.Add(k);

            await SearchAsync();
            (SearchCommand as Command)?.ChangeCanExecute();
        }

        private async Task SearchAsync()
        {
            if (_xmlBytes is null) return;

            Results.Clear();
            ResultLines.Clear();

            using var ms = new MemoryStream(_xmlBytes);
            var items = await _strategy.SearchAsync(ms, Keyword, SelectedAttrFilters);
            foreach (var model in items)
            {
                Results.Add(model);
                ResultLines.Add(FormatModel(model));
            }
        }

        private string FormatModel(StudentModel m)
        {
            var parts = new List<string>();
            void add(string? v) { if (!string.IsNullOrWhiteSpace(v)) parts.Add(v!); }

            add(m.FullName);
            add(m.Faculty);
            add(m.Department);
            add(m.Specialty);
            add(m.EventWindow);
            add(m.ParliamentType);

            var left = parts.Count > 0 ? string.Join(" • ", parts) : "(no text fields)";
            
            string right = m.Attributes is { Count: > 0 }
                ? " [" + string.Join("; ", m.Attributes.Select(kv => $"{kv.Key}={kv.Value}")) + "]"
                : string.Empty;

            return left + right;
        }

        private void Clear()
        {
            Keyword = string.Empty;
            NewAttrValue = string.Empty;
            SelectedAttrKey = string.Empty;

            SelectedAttrFilters.Clear();
            ActiveFilters.Clear();

            Results.Clear();
            ResultLines.Clear();
        }

        private void AddFilter()
        {
            if (string.IsNullOrWhiteSpace(SelectedAttrKey) || string.IsNullOrWhiteSpace(NewAttrValue))
                return;

            SelectedAttrFilters[SelectedAttrKey] = NewAttrValue;

            var existing = ActiveFilters.FirstOrDefault(f => f.Key == SelectedAttrKey);
            if (existing is not null) ActiveFilters.Remove(existing);
            ActiveFilters.Add(new FilterItem(SelectedAttrKey, NewAttrValue));

            _ = SearchAsync();
        }

        private void RemoveFilter(string key)
        {
            if (!SelectedAttrFilters.Remove(key)) return;
            var chip = ActiveFilters.FirstOrDefault(f => f.Key == key);
            if (chip != null) ActiveFilters.Remove(chip);
            _ = SearchAsync();
        }
    }

    public record FilterItem(string Key, string Value)
    {
        public string Display => $"{Key} = {Value}";
    }
}
