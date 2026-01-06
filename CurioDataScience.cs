using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.PoEMemory.FilesInMemory;
using System;
using System.IO;
using System.Numerics;
using System.Text;
using SharpDX;

namespace CurioDataScience;

public class Curio_Data_Science : BaseSettingsPlugin<CurioDataScienceSettings>
{
    private readonly SharpDX.Color defaultColor = SharpDX.Color.Cyan;
    private readonly SharpDX.Color replicaColor = SharpDX.Color.Orange;
    private readonly SharpDX.Color trinketColor = SharpDX.Color.Gold;
    private readonly SharpDX.Color currencyColor = SharpDX.Color.Yellow;
    private readonly SharpDX.Color scarabColor = SharpDX.Color.LimeGreen;
    private int textPositionY = 300; // TODO Set up a settings file that I can adjust it in game
    private DateTime lastExport = DateTime.MinValue;
    private readonly TimeSpan exportCooldown = TimeSpan.FromSeconds(5);
    private readonly HashSet<uint> exportedRewardIds = new();

    public override bool Initialise()
    {
        return true;
    }

    private const int MAX_ENCHANTED = 1;
    private const int MAX_EXPLICIT = 6;

    private record HeistRewardInfo(string LabelText, System.Numerics.Vector2 Pos, string DisplayName, string BaseName, string ClassName, ItemRarity? Rarity, List<string> EnchantedStats, List<string> ModTranslations, List<object> ItemMods, List<string> HumanStats, List<string> EnchantedModDisplayNames, List<string> EnchantedModTranslations, List<List<float>> EnchantedModValues, List<string> ExplicitModsDisplayNames, List<string> ExplicitModsTranslations, List<List<float>> ExplicitModsValues, int? StackSize, uint? ItemOnGroundId, uint EntityId, string Path);

    private List<HeistRewardInfo> GetHeistRewards()
    {
        var labels = GameController.IngameState.IngameUi.ItemsOnGroundLabelsVisible;
        var result = new List<HeistRewardInfo>();
        foreach (var labelOnGround in labels)
        {
            var item = labelOnGround.ItemOnGround;
            if (item != null &&
                item.TryGetComponent<HeistRewardDisplay>(out var heistReward) &&
                heistReward.RewardItem is { IsValid: true } rewardEntity)
            {
                var uiLabel = labelOnGround.Label?.Text;
                var baseItem = GameController.Files.BaseItemTypes.Translate(rewardEntity.Path);
                if (string.Equals(baseItem?.ClassName, "HeistObjective", StringComparison.OrdinalIgnoreCase))
                    continue;

                var baseName = baseItem?.BaseName;
                var pos = new System.Numerics.Vector2(50, textPositionY + result.Count * 32);
                var parsed = ParseHeistReward(rewardEntity, uiLabel, baseName, pos, item?.Id);
                result.Add(parsed);
            }
        }
        return result;
    }

    private HeistRewardInfo ParseHeistReward(Entity rewardEntity, string uiLabel, string baseName, System.Numerics.Vector2 pos, uint? itemOnGroundId)
    {
        string className = null;
        ItemRarity? rarity = null;
        var enchanted = new List<string>();
        var humanStats = new List<string>();
        var modTranslations = new List<string>();
        var itemMods = new List<object>();
        string displayName = uiLabel;
        var enchantedModDisplayNames = new List<string>();
        var enchantedModTranslations = new List<string>();
        var enchantedModValues = new List<List<float>>();
        var explicitModsDisplayNames = new List<string>();
        var explicitModsTranslations = new List<string>();
        var explicitModValues = new List<List<float>>();
        int? stackSize = null;

        try
        {
            var baseItem = GameController.Files.BaseItemTypes.Translate(rewardEntity.Path);
            className = baseItem?.ClassName;

            if (rewardEntity.TryGetComponent<Mods>(out var mods))
            {
                rarity = mods.ItemRarity;

                if (mods.ItemRarity == ItemRarity.Unique && !string.IsNullOrWhiteSpace(mods.UniqueName))
                {
                    displayName = mods.UniqueName;
                }
                else if (mods.ItemRarity == ItemRarity.Rare)
                {
                    displayName = baseName ?? uiLabel ?? "Unknown Reward";
                }

                if (mods.EnchantedStats != null)
                    enchanted.AddRange(mods.EnchantedStats);
                if (mods.HumanStats != null)
                    humanStats.AddRange(mods.HumanStats);

                var modsType = mods.GetType();

                if (string.Equals(className, "Trinket", StringComparison.OrdinalIgnoreCase) && mods.HumanStats != null && mods.HumanStats.Count > 0)
                {
                    modTranslations.AddRange(mods.HumanStats);
                }
                else
                {
                    var propExplicit = modsType.GetProperty("ExplicitMods");
                    if (propExplicit != null && propExplicit.GetValue(mods) is System.Collections.IEnumerable em)
                    {
                        foreach (var m in em)
                        {
                            if (m == null) continue;
                            itemMods.Add(m);
                            var translation = m?.GetType().GetProperty("DisplayName")?.GetValue(m) as string;
                            if (!string.IsNullOrWhiteSpace(translation))
                            {
                                modTranslations.Add(translation);
                                continue;
                            }

                            var raw = m?.GetType().GetProperty("RawName")?.GetValue(m) as string;
                            if (!string.IsNullOrWhiteSpace(raw)) modTranslations.Add(raw);
                            else modTranslations.Add(m.ToString());
                        }
                    }
                    else
                    {
                        var propItemMods = modsType.GetProperty("ItemMods");
                        if (propItemMods != null && propItemMods.GetValue(mods) is System.Collections.IEnumerable imCol)
                        {
                            foreach (var im in imCol)
                            {
                                if (im == null) continue;
                                itemMods.Add(im);
                                var translation = im?.GetType().GetProperty("DisplayName")?.GetValue(im) as string;
                                if (!string.IsNullOrWhiteSpace(translation))
                                {
                                    modTranslations.Add(translation);
                                    continue;
                                }

                                var raw = im?.GetType().GetProperty("RawName")?.GetValue(im) as string;
                                if (!string.IsNullOrWhiteSpace(raw)) modTranslations.Add(raw);
                                else modTranslations.Add(im.ToString());
                            }
                        }
                    }
                }
            }

            try
            {
                if (rarity == ItemRarity.Rare)
                {
                    var propEnchantedMods = mods?.GetType().GetProperty("EnchantedMods");
                    if (propEnchantedMods != null && propEnchantedMods.GetValue(mods) is System.Collections.IEnumerable enchantCol)
                    {
                        foreach (var em in enchantCol)
                        {
                            var dn = em?.GetType().GetProperty("DisplayName")?.GetValue(em) as string;
                            var tr = em?.GetType().GetProperty("Translation")?.GetValue(em) as string;
                            var eVals = new List<float>();
                            var vpropE = em?.GetType().GetProperty("Values");
                            if (vpropE != null)
                            {
                                var vobjE = vpropE.GetValue(em);
                                if (vobjE is System.Collections.IEnumerable venE)
                                {
                                    foreach (var v in venE)
                                    {
                                        if (v == null) continue;
                                        try { eVals.Add(Convert.ToSingle(v)); } catch { }
                                    }
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(dn)) enchantedModDisplayNames.Add(dn);
                            if (!string.IsNullOrWhiteSpace(tr)) enchantedModTranslations.Add(tr);
                            enchantedModValues.Add(eVals);
                        }
                    }

                    foreach (var im in itemMods)
                    {
                        var valsList = new List<float>();
                        var vprop = im?.GetType().GetProperty("Values");
                        if (vprop != null)
                        {
                            var vobj = vprop.GetValue(im);
                            if (vobj is System.Collections.IEnumerable ven)
                            {
                                foreach (var v in ven)
                                {
                                    if (v == null) continue;
                                    try { valsList.Add(Convert.ToSingle(v)); } catch { }
                                }
                            }
                        }
                        explicitModValues.Add(valsList);

                        var dname = im?.GetType().GetProperty("DisplayName")?.GetValue(im) as string;
                        var tname = im?.GetType().GetProperty("Translation")?.GetValue(im) as string;
                        if (string.IsNullOrWhiteSpace(dname)) dname = im?.GetType().GetProperty("RawName")?.GetValue(im) as string;
                        explicitModsDisplayNames.Add(dname ?? im?.ToString() ?? "");
                        explicitModsTranslations.Add(tname ?? "");
                    }
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = uiLabel ?? baseName ?? rewardEntity.Path?.Split('/')?.LastOrDefault() ?? "Unknown Reward";
            try
            {
                if (string.Equals(className, "stackablecurrency", StringComparison.OrdinalIgnoreCase) || string.Equals(className, "mapfragment", StringComparison.OrdinalIgnoreCase))
                {
                    if (rewardEntity.TryGetComponent<Stack>(out var stack))
                    {
                        displayName = $"{stack.Size}x {displayName}";
                        stackSize = (int)stack.Size;
                    }
                    else
                    {
                        displayName = $"1x {displayName}";
                        stackSize = 1;
                    }
                }
            }
            catch {  }
        }
        catch (Exception e)
        {
            LogError(e.ToString());
        }

        return new HeistRewardInfo(
            uiLabel ?? "",
            pos,
            displayName?.Trim() ?? "Unknown Reward",
            baseName,
            className,
            rarity,
            enchanted,
            modTranslations,
            itemMods,
            humanStats,
            enchantedModDisplayNames,
            enchantedModTranslations,
            enchantedModValues,
            explicitModsDisplayNames,
            explicitModsTranslations,
            explicitModValues,
            stackSize,
            itemOnGroundId,
            rewardEntity.Id,
            rewardEntity.Path);

    }

    public override void Render()
    {
        base.Render();
        try
        {
            var rewards = GetHeistRewards();
            if (rewards.Count == 0)
            {
                if (exportedRewardIds.Count > 0) exportedRewardIds.Clear();
                return;
            }

            var colors = new[] { defaultColor, replicaColor, trinketColor, currencyColor, scarabColor };
            var startX = 50f;
            var yCursor = (float)textPositionY;
            var maxRewardsToShow = Math.Min(5, rewards.Count);

            for (int i = 0; i < maxRewardsToShow; i++)
            {
                var info = rewards[i];
                var color = colors[i % colors.Length];
                var labelToShow = info.DisplayName ?? info.LabelText ?? "";
                var labelSize = Graphics.MeasureText(labelToShow);
                Graphics.DrawText(labelToShow, new System.Numerics.Vector2(startX, yCursor), color);
                yCursor += labelSize.Y + 4;

                if (info.Rarity != ItemRarity.Unique)
                {
                    var anyModsShown = false;
                    if (info.EnchantedModDisplayNames != null && info.EnchantedModDisplayNames.Count > 0)
                    {
                        foreach (var em in info.EnchantedModDisplayNames)
                        {
                            var emText = "* " + em;
                            var emSize = Graphics.MeasureText(emText);
                            Graphics.DrawText(emText, new System.Numerics.Vector2(startX + 8, yCursor), SharpDX.Color.LightSkyBlue);
                            yCursor += emSize.Y + 2;
                            anyModsShown = true;
                        }
                    }

                    if (info.EnchantedStats != null && info.EnchantedStats.Count > 0)
                    {
                        foreach (var s in info.EnchantedStats)
                        {
                            var sText = "* " + s;
                            var sSize = Graphics.MeasureText(sText);
                            Graphics.DrawText(sText, new System.Numerics.Vector2(startX + 8, yCursor), SharpDX.Color.LightSkyBlue);
                            yCursor += sSize.Y + 2;
                            anyModsShown = true;
                        }
                    }
                    if (info.ModTranslations != null && info.ModTranslations.Count > 0)
                    {
                        var modsToShow = Math.Min(10, info.ModTranslations.Count);
                        for (int m = 0; m < modsToShow; m++)
                        {
                            var modText = "- " + info.ModTranslations[m];
                            var modSize = Graphics.MeasureText(modText);
                            Graphics.DrawText(modText, new System.Numerics.Vector2(startX + 8, yCursor), SharpDX.Color.White);
                            yCursor += modSize.Y + 2;
                            anyModsShown = true;
                        }

                        if (info.ModTranslations.Count > modsToShow)
                        {
                            var more = $"... (+{info.ModTranslations.Count - modsToShow} more)";
                            Graphics.DrawText(more, new System.Numerics.Vector2(startX + 8, yCursor), SharpDX.Color.Gray);
                            yCursor += Graphics.MeasureText(more).Y + 2;
                        }
                    }

                    if (!anyModsShown && info.Rarity.HasValue)
                    {
                        var rarityText = $"[{info.Rarity}]";
                        Graphics.DrawText(rarityText, new System.Numerics.Vector2(startX + 8, yCursor), SharpDX.Color.Gray);
                        yCursor += Graphics.MeasureText(rarityText).Y + 4;
                    }
                }
                else if (info.Rarity.HasValue)
                {
                    var rarityText = $"[{info.Rarity}]";
                    Graphics.DrawText(rarityText, new System.Numerics.Vector2(startX + 8, yCursor), SharpDX.Color.Gray);
                    yCursor += Graphics.MeasureText(rarityText).Y + 4;
                }
                yCursor += 8;
            }
            try
            {
                if (rewards.Count > 0 && DateTime.Now - lastExport > exportCooldown)
                {
                    AppendRewardsToCsv(rewards);
                    lastExport = DateTime.Now;
                }
                if (rewards.Count == 0 && exportedRewardIds.Count > 0)
                {
                    exportedRewardIds.Clear();
                }
            }
            catch (Exception ex)
            {
                LogError("CSV export failed: " + ex);
            }
        }
        catch (System.Exception e)
        {
            LogError(e.ToString());
        }
    }

    private void AppendRewardsToCsv(List<HeistRewardInfo> rewards)
    {
        var folder = Path.GetDirectoryName(typeof(Curio_Data_Science).Assembly.Location) ?? ".";
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
                if (r.ItemOnGroundId.HasValue && exportedRewardIds.Contains(r.ItemOnGroundId.Value))
                    continue;
                if (string.Equals(r.ClassName, "stackablecurrency", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.ClassName, "mapfragment", StringComparison.OrdinalIgnoreCase))
                {
                    var emptyModColumns = Enumerable.Range(0, MAX_ENCHANTED * 3 + MAX_EXPLICIT * 4).Select(_ => QuoteCsv("")).ToList();
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
                    if (r.ItemOnGroundId.HasValue) exportedRewardIds.Add(r.ItemOnGroundId.Value);
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
                    string d = "", t = "", v = "", mm = "";
                    if (r.ExplicitModsDisplayNames != null && i < r.ExplicitModsDisplayNames.Count) d = r.ExplicitModsDisplayNames[i];
                    if (r.ExplicitModsTranslations != null && i < r.ExplicitModsTranslations.Count) t = r.ExplicitModsTranslations[i];
                    if (r.ExplicitModsValues != null && i < r.ExplicitModsValues.Count) v = string.Join(";", r.ExplicitModsValues[i].Select(x => x.ToString()));
                    cols.Add(QuoteCsv(d)); cols.Add(QuoteCsv(t)); cols.Add(QuoteCsv(v)); cols.Add(QuoteCsv(mm));
                }

                cols.Add(QuoteCsv(string.Join(" | ", r.ModTranslations ?? new List<string>())));
                sw.WriteLine(string.Join(",", cols));
                if (r.ItemOnGroundId.HasValue) exportedRewardIds.Add(r.ItemOnGroundId.Value);
                exportedRewardIds.Add(r.EntityId);
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
