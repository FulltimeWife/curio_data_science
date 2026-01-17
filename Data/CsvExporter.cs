using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CurioDataScience.Item;

namespace CurioDataScience.Data
{
    public class CsvExporter
    {
        private const int MAX_ENCHANTED = 1;
        private const int MAX_EXPLICIT = 6;
        
        public void ExportRewardsToCsv(List<HeistRewardInfo> rewards, string pluginAssemblyLocation)
        {
            var folder = Path.GetDirectoryName(pluginAssemblyLocation) ?? ".";
            var filePath = Path.Combine(folder, "heist_rewards.csv");

            bool writeHeader = !File.Exists(filePath);

            using (var sw = new StreamWriter(filePath, append: true, encoding: Encoding.UTF8))
            {
                if (writeHeader)
                {
                    var headerCols = new List<string> {
                        "Timestamp",
                        "DisplayName",
                        "BaseName",
                        "ClassName",
                        "Rarity",
                        "StackSize",
                    };

                    for (int i = 1; i <= MAX_ENCHANTED; i++)
                    {
                        headerCols.Add($"Enchanted{i}_Display");
                        headerCols.Add($"Enchanted{i}_Translation");
                        headerCols.Add($"Enchanted{i}_Values");
                    }
                    for (int i = 1; i <= MAX_EXPLICIT; i++)
                    {
                        headerCols.Add($"Explicit{i}_Display");
                        headerCols.Add($"Explicit{i}_Translation");
                        headerCols.Add($"Explicit{i}_Values");
                    }

                    headerCols.Add("AllModTranslations");

                    sw.WriteLine(string.Join(",", headerCols.Select(h => QuoteCsv(h))));
                }

                foreach (var r in rewards)
                {
                    if (string.Equals(r.ClassName, "stackablecurrency", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r.ClassName, "mapfragment", StringComparison.OrdinalIgnoreCase))
                    {
                        var emptyModColumns = Enumerable.Range(0, MAX_ENCHANTED * 3 + MAX_EXPLICIT * 3).Select(_ => QuoteCsv("")).ToList();
                        var baseCols = new List<string> {
                            QuoteCsv(DateTime.Now.ToString("o")),
                            QuoteCsv(r.DisplayName),
                            QuoteCsv(r.BaseName),
                            QuoteCsv(r.ClassName),
                            QuoteCsv(r.Rarity?.ToString() ?? ""),
                            QuoteCsv(r.StackSize?.ToString() ?? ""),
                        };
                        baseCols.AddRange(emptyModColumns);
                        baseCols.Add(QuoteCsv(string.Join(" | ", r.ModTranslations ?? new List<string>())));
                        sw.WriteLine(string.Join(",", baseCols));
                        continue;
                    }
                    
                    var cols = new List<string> {
                        QuoteCsv(DateTime.Now.ToString("o")),
                        QuoteCsv(r.DisplayName),
                        QuoteCsv(r.BaseName),
                        QuoteCsv(r.ClassName),
                        QuoteCsv(r.Rarity?.ToString() ?? ""),
                        QuoteCsv(r.StackSize?.ToString() ?? ""),
                    };

                    for (int i = 0; i < MAX_ENCHANTED; i++)
                    {
                        string d = "", t = "", v = "";
                        if (r.EnchantedModDisplayNames != null && i < r.EnchantedModDisplayNames.Count) d = r.EnchantedModDisplayNames[i];
                        if (r.EnchantedModTranslations != null && i < r.EnchantedModTranslations.Count) t = r.EnchantedModTranslations[i];
                        if (r.EnchantedModValues != null && i < r.EnchantedModValues.Count) v = string.Join(";", r.EnchantedModValues[i].Select(x => x.ToString()));
                        cols.Add(QuoteCsv(d)); cols.Add(QuoteCsv(t)); cols.Add(QuoteCsv(v));
                    }

                    for (int i = 0; i < MAX_EXPLICIT; i++)
                    {
                        string d = "", t = "", v = "";
                        if (r.ExplicitModsDisplayNames != null && i < r.ExplicitModsDisplayNames.Count) d = r.ExplicitModsDisplayNames[i];
                        if (r.ExplicitModsTranslations != null && i < r.ExplicitModsTranslations.Count) t = r.ExplicitModsTranslations[i];
                        if (r.ExplicitModsValues != null && i < r.ExplicitModsValues.Count) v = string.Join(";", r.ExplicitModsValues[i].Select(x => x.ToString()));
                        cols.Add(QuoteCsv(d)); cols.Add(QuoteCsv(t)); cols.Add(QuoteCsv(v));
                    }

                    cols.Add(QuoteCsv(string.Join(" | ", r.ModTranslations ?? new List<string>())));
                    sw.WriteLine(string.Join(",", cols));
                }
            }
        }
        
        private static string QuoteCsv(string input)
        {
            if (input == null) return "";
            var outp = input.Replace("\"", "\"\"");
            return $"\"{outp}\"";
        }
    }
}