using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace CurioDataScience.Item
{
    public class ItemProcessor
    {
        private readonly GameController _gameController;
        
        public ItemProcessor(GameController gameController)
        {
            _gameController = gameController;
        }
        
        public List<HeistRewardInfo> GetHeistRewards()
        {
            var labels = _gameController.IngameState.IngameUi.ItemsOnGroundLabelsVisible;
            var result = new List<HeistRewardInfo>();
            
            foreach (var labelOnGround in labels)
            {
                var item = labelOnGround.ItemOnGround;
                if (item != null &&
                    item.TryGetComponent<HeistRewardDisplay>(out var heistReward) &&
                    heistReward.RewardItem is { IsValid: true } rewardEntity)
                {
                    var uiLabel = labelOnGround.Label?.Text;
                    var baseItem = _gameController.Files.BaseItemTypes.Translate(rewardEntity.Path);
                    
                    if (string.Equals(baseItem?.ClassName, "HeistObjective", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var baseName = baseItem?.BaseName;
                    var pos = new System.Numerics.Vector2(50, Core.Constants.TextPositionY + result.Count * 32);
                    var parsed = ParseHeistReward(rewardEntity, uiLabel, baseName, pos, item?.Id);
                    result.Add(parsed);
                }
            }
            
            return result;
        }
        
        public HeistRewardInfo ParseHeistReward(Entity rewardEntity, string uiLabel, string baseName, 
            System.Numerics.Vector2 pos, uint? itemOnGroundId)
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
                var baseItem = _gameController.Files.BaseItemTypes.Translate(rewardEntity.Path);
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
                DebugWindow.LogError(e.ToString());
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
        
        public string CreateContentHash(HeistRewardInfo reward)
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
    }
}