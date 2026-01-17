using System;
using System.Collections.Generic;
using ExileCore.Shared.Enums;

namespace CurioDataScience.Item
{
    public record HeistRewardInfo(
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
}