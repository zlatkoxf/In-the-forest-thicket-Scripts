using System;
using System.Collections.Generic;
using UnityEngine;

public static class Localization
{
    public enum Lang { EN, RU }

    private static Dictionary<string, string> cache = new Dictionary<string, string>();

    public static string Get(string key) => cache.TryGetValue(key, out var v) ? v : $"[{key}]";
    public static string Get(string key, params object[] args) => cache.TryGetValue(key, out var v) ? string.Format(v, args) : $"[{key}]";

    public static void LoadLanguage(Lang lang)
    {
        cache.Clear();
        // Загружаем CSV из папки Assets/Resources/Localization/localization.csv
        TextAsset csvFile = Resources.Load<TextAsset>("Localization/localization");
        if (csvFile == null) { Debug.LogError("[Loc] Файл Resources/Localization/localization.csv не найден!"); return; }

        string[] lines = csvFile.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return;

        string[] headers = SplitCsvLine(lines[0]);
        string targetLangStr = lang.ToString();
        int langIdx = Array.FindIndex(headers, 1, h => h.Equals(targetLangStr, StringComparison.OrdinalIgnoreCase));

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = SplitCsvLine(lines[i]);
            if (row.Length > langIdx && !string.IsNullOrEmpty(row[0]))
                cache[row[0]] = row[langIdx].Replace("\\n", "\n");
        }
    }

    private static string[] SplitCsvLine(string line)
    {
        List<string> result = new List<string>();
        int start = 0; bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"') inQuotes = !inQuotes;
            else if (line[i] == ',' && !inQuotes) {
                result.Add(line.Substring(start, i - start).Trim(' ', '"'));
                start = i + 1;
            }
        }
        result.Add(line.Substring(start).Trim(' ', '"'));
        return result.ToArray();
    }
}
