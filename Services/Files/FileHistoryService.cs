using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XMLParser.Constants;
using MauiFileSystem = Microsoft.Maui.Storage.FileSystem;

namespace XMLParser.FileSystem;

public static class FileHistoryService
{
    private static readonly string HistoryFileName = Literals.defaultHistoryFileName;

    private static string GetHistoryFilePath()
    {
        return Path.Combine(MauiFileSystem.AppDataDirectory, HistoryFileName);
    }

    public static async Task AddEntryAsync(string fileName, string filePath)
    {
        var entries = await LoadEntriesAsync();

        entries.RemoveAll(e => e.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)
                             && e.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

        entries.Insert(0, (fileName, filePath));

        await SaveEntriesAsync(entries);
    }

    public static async Task<List<(string FileName, string FilePath)>> LoadEntriesAsync()
    {
        string path = GetHistoryFilePath();
        if (!File.Exists(path))
            return new List<(string, string)>();

        var lines = await File.ReadAllLinesAsync(path);

        var entries = new List<(string, string)>();
        foreach (var line in lines)
        {
            var parts = line.Split(new[] { " - " }, 2, StringSplitOptions.None);
            if (parts.Length == 2)
                entries.Add((parts[0], parts[1]));
        }

        return entries;
    }

    public static async Task SaveEntriesAsync(List<(string FileName, string FilePath)> entries)
    {
        string path = GetHistoryFilePath();

        var lines = entries.Select(e => $"{e.FileName} - {e.FilePath}").ToList();

        await File.WriteAllLinesAsync(path, lines);
    }

    public static async Task ClearHistoryAsync()
    {
        string path = GetHistoryFilePath();
        if (File.Exists(path))
            File.Delete(path);

        await Task.CompletedTask;
    }
}
