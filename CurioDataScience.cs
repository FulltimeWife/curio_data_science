﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;

namespace CurioDataScience
{
    public class Curio_Data_Science : BaseSettingsPlugin<CurioDataScienceSettings>
    {
        private readonly SharpDX.Color defaultColor = SharpDX.Color.Cyan;
        private readonly SharpDX.Color replicaColor = SharpDX.Color.Orange;
        private readonly SharpDX.Color trinketColor = SharpDX.Color.Gold;
        private readonly SharpDX.Color currencyColor = SharpDX.Color.Yellow;
        private readonly SharpDX.Color scarabColor = SharpDX.Color.LimeGreen;
        
        private int textPositionY = 300;
        
        private readonly List<BufferedReward> bufferedRewards = new();
        private const int BUFFER_SIZE = 5;
        private DateTime lastBufferCheck = DateTime.MinValue;
        private readonly TimeSpan bufferCheckInterval = TimeSpan.FromSeconds(1);
        private readonly TimeSpan completionTimeout = TimeSpan.FromSeconds(10);
        private readonly HashSet<uint> exportedEntityIds = new();
        private DateTime lastBufferUpdate = DateTime.MinValue;
        private readonly TimeSpan bufferUpdateCooldown = TimeSpan.FromMilliseconds(500);

        private const int MAX_ENCHANTED = 1;
        private const int MAX_EXPLICIT = 6;

        private List<HeistRewardInfo> lastVisibleRewards = new();

        private ItemFilter itemFilter = new ItemFilter();

        private record HeistRewardInfo(
            string LabelText, 
            System.Numerics.Vector2 Pos, 
            string DisplayName, 
            string BaseName, 
            string ClassName, 
            ItemRarity? Rarity, 
            List<string> EnchantedStats, 
            List<string> ModTranslations, 
            List<object> ItemMods, 
            List<string> HumanStats, 
            List<string> EnchantedModDisplayNames, 
            List<string> EnchantedModTranslations, 
            List<List<float>> EnchantedModValues, 
            List<string> ExplicitModsDisplayNames, 
            List<string> ExplicitModsTranslations, 
            List<List<float>> ExplicitModsValues, 
            int? StackSize, 
            uint? ItemOnGroundId, 
            uint EntityId, 
            string Path);

        private class BufferedReward
        {
            public HeistRewardInfo Reward { get; set; }
            public RewardState State { get; set; }
            public DateTime FirstSeen { get; set; }
            public DateTime LastSeen { get; set; }
            public DateTime LastVisible { get; set; }
            public int VisibilityCount { get; set; }
            public bool IsExported { get; set; }
            public string ContentHash { get; set; }
            public uint? ItemOnGroundId { get; set; }
            public string CompositeKey => $"{ContentHash}|{ItemOnGroundId}";
        }

        private enum RewardState
        {
            New,
            Visible,
            Completed,
            Exported
        }

        public override bool Initialise()
        {
            LogMessage("Heist Data Science initialized with CONTENT-BASED buffer system");
            LogMessage($"Buffer size: {BUFFER_SIZE} items");
            LogMessage("Using content hashing to prevent EntityID duplication");
            return true;
        }

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
                    if (string.Equals(className, "stackablecurrency", StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(className, "mapfragment", StringComparison.OrdinalIgnoreCase))
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

        private string CreateContentHash(HeistRewardInfo reward)
        {
            var parts = new List<string>
            {
                reward.DisplayName?.Trim()?.ToLowerInvariant() ?? "",
                reward.BaseName?.Trim()?.ToLowerInvariant() ?? "",
                reward.ClassName?.Trim()?.ToLowerInvariant() ?? "",
                reward.Rarity?.ToString() ?? "",
                reward.StackSize?.ToString() ?? "1"
            };
            
            if (reward.ModTranslations != null && reward.ModTranslations.Count > 0)
            {
                parts.Add($"mods:{string.Join(",", reward.ModTranslations.Take(2))}");
            }
            
            return string.Join("|", parts);
        }

        private void UpdateBufferState(List<HeistRewardInfo> currentRewards)
        {
            lastVisibleRewards = currentRewards.ToList();
            
            if ((DateTime.Now - lastBufferUpdate) < bufferUpdateCooldown)
                return;
            
            lastBufferUpdate = DateTime.Now;
            
            var updatedAny = false;
            
            // Clean up old exported items first
            bufferedRewards.RemoveAll(b => 
                b.State == RewardState.Exported && 
                (DateTime.Now - b.LastSeen).TotalMinutes > 5);
            
            foreach (var buffered in bufferedRewards.ToList())
            {
                bool isCurrentlyVisible = false;
                HeistRewardInfo matchingReward = null;
                
                foreach (var reward in currentRewards)
                {
                    var hash = CreateContentHash(reward);
                    var compositeKey = $"{hash}|{reward.ItemOnGroundId}";
                    
                    if (compositeKey == buffered.CompositeKey)
                    {
                        isCurrentlyVisible = true;
                        matchingReward = reward;
                        break;
                    }
                }
                
                if (isCurrentlyVisible && matchingReward != null)
                {
                    buffered.Reward = matchingReward;
                    
                    if (buffered.State != RewardState.Visible)
                    {
                        buffered.State = RewardState.Visible;
                        updatedAny = true;
                    }
                    buffered.LastSeen = DateTime.Now;
                    buffered.LastVisible = DateTime.Now;
                    buffered.VisibilityCount++;
                }
                else if (buffered.State == RewardState.Visible)
                {
                    var timeSinceLastVisible = DateTime.Now - buffered.LastVisible;
                    
                    if (timeSinceLastVisible.TotalSeconds >= 2.0)
                    {
                        var totalVisibleTime = DateTime.Now - buffered.FirstSeen;
                        
                        if (totalVisibleTime.TotalSeconds >= 5.0)
                        {
                            buffered.State = RewardState.Completed;
                            buffered.LastSeen = DateTime.Now;
                            updatedAny = true;
                            LogMessage($"Reward completed: {buffered.Reward.DisplayName} (visible for {totalVisibleTime.TotalSeconds:F1}s)");
                            if (buffered.Reward.Rarity == ItemRarity.Rare) {
                                LogMessage($"{buffered.Reward.ModTranslations?.Count ?? 0} mods: {string.Join(" | ", buffered.Reward.ModTranslations ?? new List<string>())}");
                                LogMessage($"{buffered.Reward.EnchantedStats?.Count ?? 0} enchanted stats: {string.Join(" | ", buffered.Reward.EnchantedStats ?? new List<string>())}");
                                LogMessage($"{buffered.Reward.HumanStats?.Count ?? 0} human stats: {string.Join(" | ", buffered.Reward.HumanStats ?? new List<string>())}");
                            }
                        }
                        else
                        {
                            bufferedRewards.Remove(buffered);
                            LogMessage($"Removed brief item: {buffered.Reward.DisplayName} (only {totalVisibleTime.TotalSeconds:F1}s)");
                            updatedAny = true;
                        }
                    }
                }
                else if (buffered.State == RewardState.New)
                {
                    var timeAsNew = DateTime.Now - buffered.FirstSeen;
                    if (timeAsNew.TotalSeconds > 5.0)
                    {
                        bufferedRewards.Remove(buffered);
                        LogMessage($"Cleaned up New item that never became Visible: {buffered.Reward.DisplayName}");
                        updatedAny = true;
                    }
                }
            }
            
            foreach (var reward in currentRewards)
            {
                var hash = CreateContentHash(reward);
                var compositeKey = $"{hash}|{reward.ItemOnGroundId}";
                
                bool alreadyExists = bufferedRewards.Any(b => b.CompositeKey == compositeKey);
                if (alreadyExists)
                {
                    var existing = bufferedRewards.First(b => b.CompositeKey == compositeKey);
                    existing.Reward = reward;
                    existing.LastSeen = DateTime.Now;
                    if (existing.State != RewardState.Visible)
                    {
                        existing.State = RewardState.Visible;
                        updatedAny = true;
                    }
                    continue;
                }

                if (string.IsNullOrEmpty(reward.DisplayName))
                {
                    LogMessage($"Skipping suspicious item: {reward.DisplayName ?? "Unknown"}");
                    continue;
                }
                
                bufferedRewards.Add(new BufferedReward
                {
                    Reward = reward,
                    ContentHash = hash,
                    ItemOnGroundId = reward.ItemOnGroundId,
                    State = RewardState.New,
                    FirstSeen = DateTime.Now,
                    LastSeen = DateTime.Now,
                    LastVisible = DateTime.Now,
                    VisibilityCount = 1,
                    IsExported = false
                });
                
                updatedAny = true;
                LogMessage($"New reward buffered: {reward.DisplayName} (ID: {reward.ItemOnGroundId}, Hash: {hash})");
            }
            
            if (updatedAny)
            {
                CheckForBufferFlush();
            }
        }

        private void CheckForBufferFlush()
        {
            var completedItems = bufferedRewards
                .Where(b => b.State == RewardState.Completed)
                .ToList();
            
            var completedCount = completedItems.Count;
            
            if (completedCount >= BUFFER_SIZE)
            {
                LogMessage($"Buffer condition met: {completedCount} completed items (need {BUFFER_SIZE})");
                FlushCompletedItems();
                return;
            }
            
            if (DateTime.Now - lastBufferCheck > bufferCheckInterval)
            {
                var visibleCount = bufferedRewards.Count(b => b.State == RewardState.Visible);
                var newCount = bufferedRewards.Count(b => b.State == RewardState.New);
                
                if (completedCount > 0 && visibleCount == 0 && newCount == 0)
                {
                    var oldestCompletion = completedItems
                        .OrderBy(b => b.LastSeen)
                        .FirstOrDefault();
                    
                    if (oldestCompletion != null && 
                        (DateTime.Now - oldestCompletion.LastSeen) > completionTimeout)
                    {
                        LogMessage($"Timeout flush: {completedCount} items completed for >{completionTimeout.TotalSeconds}s");
                        FlushCompletedItems();
                    }
                }
                
                lastBufferCheck = DateTime.Now;
            }
        }

        private void FlushCompletedItems()
        {
            var toExport = bufferedRewards
                .Where(b => b.State == RewardState.Completed && !b.IsExported)
                .Select(b => b.Reward)
                .ToList();
            
            if (toExport.Count == 0)
            {
                LogMessage("Flush called but no items to export");
                return;
            }
            
            LogMessage($"Preparing to export {toExport.Count} items to CSV");
            
            try
            {
                ExportRewardsToCsv(toExport);
                
                foreach (var buffered in bufferedRewards.Where(b => b.State == RewardState.Completed))
                {
                    buffered.State = RewardState.Exported;
                    buffered.IsExported = true;
                    
                    exportedEntityIds.Add(buffered.Reward.EntityId);
                    if (buffered.Reward.ItemOnGroundId.HasValue)
                    {
                        exportedEntityIds.Add(buffered.Reward.ItemOnGroundId.Value);
                    }
                }
                
                bufferedRewards.Clear();
                exportedEntityIds.Clear();
                
                LogMessage($"Successfully exported {toExport.Count} items to CSV. Buffer cleared for next set.");
            }
            catch (Exception ex)
            {
                LogError("Export failed: " + ex);
            }
        }

        private void ExportRewardsToCsv(List<HeistRewardInfo> rewards)
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
                }
            }
        }

        public override void Render()
        {
            base.Render();
            try
            {
                var currentRewards = GetHeistRewards();
                
                UpdateBufferState(currentRewards);
                
                ShowBufferStatus();
                
                DisplayCurrentRewards(currentRewards);
            }
            catch (System.Exception e)
            {
                LogError(e.ToString());
            }
        }

        private void ShowBufferStatus()
        {
            var startX = 50f;
            var yCursor = (float)textPositionY;
            
            var completedCount = bufferedRewards.Count(b => b.State == RewardState.Completed);
            var visibleCount = bufferedRewards.Count(b => b.State == RewardState.Visible);
            var newCount = bufferedRewards.Count(b => b.State == RewardState.New);
            var exportedCount = bufferedRewards.Count(b => b.State == RewardState.Exported);
            var totalBuffered = bufferedRewards.Count;
            
            Graphics.DrawText($"Buffer Status", new System.Numerics.Vector2(startX, yCursor), SharpDX.Color.White);
            yCursor += 20;
            
            Graphics.DrawText($"Completed: {completedCount}/{BUFFER_SIZE} to flush", 
                new System.Numerics.Vector2(startX, yCursor), 
                completedCount >= BUFFER_SIZE ? SharpDX.Color.LimeGreen : SharpDX.Color.Yellow);
            yCursor += 20;
            
            Graphics.DrawText($"Visible: {visibleCount} | New: {newCount} | Total: {totalBuffered} | Exported: {exportedCount}", 
                new System.Numerics.Vector2(startX, yCursor), SharpDX.Color.LightGray);
            yCursor += 20;
            
            if (completedCount > 0 && completedCount < BUFFER_SIZE)
            {
                var oldestCompleted = bufferedRewards
                    .Where(b => b.State == RewardState.Completed)
                    .OrderBy(b => b.LastSeen)
                    .FirstOrDefault();
                
                if (oldestCompleted != null)
                {
                    var timeSinceCompletion = DateTime.Now - oldestCompleted.LastSeen;
                    var timeUntilTimeout = completionTimeout - timeSinceCompletion;
                    
                    if (timeUntilTimeout.TotalSeconds > 0)
                    {
                        Graphics.DrawText($"Timeout in: {timeUntilTimeout.TotalSeconds:F0}s", 
                            new System.Numerics.Vector2(startX, yCursor), SharpDX.Color.LightBlue);
                        yCursor += 20;
                    }
                }
            }
            
            yCursor += 10;
            
            var recentCompleted = bufferedRewards
                .Where(b => b.State == RewardState.Completed)
                .OrderByDescending(b => b.LastSeen)
                .Take(3)
                .ToList();
            
            if (recentCompleted.Count > 0)
            {
                Graphics.DrawText("Recently completed:", new System.Numerics.Vector2(startX, yCursor), SharpDX.Color.White);
                yCursor += 20;
                
                foreach (var buffered in recentCompleted)
                {
                    var name = buffered.Reward.DisplayName.Length > 20 
                        ? buffered.Reward.DisplayName.Substring(0, 20) + "..." 
                        : buffered.Reward.DisplayName;
                    
                    Graphics.DrawText($"• {name}", new System.Numerics.Vector2(startX + 10, yCursor), SharpDX.Color.LightGreen);
                    yCursor += 16;
                }
            }
        }

        private void DisplayCurrentRewards(List<HeistRewardInfo> rewards)
        {
            if (rewards.Count == 0) return;
            
            var colors = new[] { defaultColor, replicaColor, trinketColor, currencyColor, scarabColor };
            var startX = 50f;
            var yCursor = 400f;
            
            var filteredItems = new List<(string displayText, HeistRewardInfo reward)>();
            foreach (var reward in rewards)
            {
                var itemData = new ItemData(
                    reward.DisplayName, 
                    reward.BaseName, 
                    reward.EnchantedStats, 
                    reward.HumanStats,
                    reward.ModTranslations,
                    reward.EnchantedModValues);
                
                string matchedFilter = itemFilter.GetMatchingFilterName(itemData);
                
                if (!string.IsNullOrEmpty(matchedFilter))
                {
                    string displayText = FormatDisplayText(matchedFilter, reward);
                    filteredItems.Add((displayText, reward));
                }
            }
            
            Graphics.DrawText($"Active Filters ({filteredItems.Count}/{rewards.Count}):", 
                new System.Numerics.Vector2(startX, yCursor), SharpDX.Color.White);
            yCursor += 20;
            
            var maxToShow = Math.Min(5, filteredItems.Count);
            for (int i = 0; i < maxToShow; i++)
            {
                var (displayText, info) = filteredItems[i];
                var color = colors[i % colors.Length];
                
                Graphics.DrawText($"{i+1}. {displayText}", 
                    new System.Numerics.Vector2(startX, yCursor), color);
                yCursor += 16;
            }
        }

        private string FormatDisplayText(string filterName, HeistRewardInfo reward)
        {
            bool isCurrency = string.Equals(reward.ClassName, "stackablecurrency", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(reward.ClassName, "mapfragment", StringComparison.OrdinalIgnoreCase);
            
            if (isCurrency && reward.StackSize.HasValue && reward.StackSize.Value > 1)
            {
                return $"{filterName} (x{reward.StackSize})";
            }
            
            return filterName;
        }
        
        private static string QuoteCsv(string input)
        {
            if (input == null) return "";
            var outp = input.Replace("\"", "\"\"");
            return $"\"{outp}\"";
        }
        
        private void LogMessage(string message) => 
            DebugWindow.LogMsg($"[Heist Data Science] {message}", 1);
        
        private void LogError(string error) => 
            DebugWindow.LogError($"[Heist Data Science] {error}", 2);
    }
}