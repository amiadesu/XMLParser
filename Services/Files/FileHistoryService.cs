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
    private static readonly string HistoryDirectory = Path.Combine(MauiFileSystem.AppDataDirectory, "history");

    private static string GetHistoryFilePath(string key)
    {
        Directory.CreateDirectory(HistoryDirectory);
        return Path.Combine(HistoryDirectory, $"{key}.txt");
    }

    public static async Task AddEntryAsync(string key, string fileName, string filePath)
    {
        var entries = await LoadEntriesAsync(key);

        entries.RemoveAll(e => e.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)
                             && e.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

        entries.Insert(0, (fileName, filePath));

        await SaveEntriesAsync(key, entries);
    }

    public static async Task<List<(string FileName, string FilePath)>> LoadEntriesAsync(string key)
    {
        string path = GetHistoryFilePath(key);
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

    private static async Task SaveEntriesAsync(string key, List<(string FileName, string FilePath)> entries)
    {
        string path = GetHistoryFilePath(key);
        var lines = entries.Select(e => $"{e.FileName} - {e.FilePath}");
        await File.WriteAllLinesAsync(path, lines);
    }

    public static async Task ClearHistoryAsync()
    {
        if (Directory.Exists(HistoryDirectory))
        {
            foreach (var file in Directory.GetFiles(HistoryDirectory))
                File.Delete(file);
        }
        await Task.CompletedTask;
    }
}
