using System;
using System.Collections.Generic;
using System.Linq;

namespace CurioDataScience
{
    public interface IItemCondition
    {
        bool Matches(ItemData item);
    }

    public class BaseNameCondition : IItemCondition
    {
        private readonly string _baseName;
        
        public BaseNameCondition(string baseName)
        {
            _baseName = baseName;
        }
        
        public bool Matches(ItemData item)
        {
            return string.Equals(item.BaseName, _baseName, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class DisplayNameCondition : IItemCondition
    {
        private readonly string _displayName;
        
        public DisplayNameCondition(string displayName)
        {
            _displayName = displayName;
        }
        
        public bool Matches(ItemData item)
        {
            return string.Equals(item.DisplayName, _displayName, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class EnchantmentCondition : IItemCondition
    {
        private readonly string _baseName;
        private readonly string _enchantmentText;
        private readonly float? _minValue;
        private readonly float? _maxValue;
        
        public EnchantmentCondition(string baseName, string enchantmentText)
        {
            _baseName = baseName;
            _enchantmentText = enchantmentText;
        }
        
        public EnchantmentCondition(string baseName, string enchantmentText, float exactValue)
        {
            _baseName = baseName;
            _enchantmentText = enchantmentText;
            _minValue = exactValue;
            _maxValue = exactValue;
        }
        
        public bool Matches(ItemData item)
        {
            if (!string.Equals(item.BaseName, _baseName, StringComparison.OrdinalIgnoreCase))
                return false;
            
            if (item.EnchantedStats != null)
            {
                for (int i = 0; i < item.EnchantedStats.Count; i++)
                {
                    var stat = item.EnchantedStats[i];
                    if (stat.IndexOf(_enchantmentText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!_minValue.HasValue && !_maxValue.HasValue)
                            return true;
                        
                        if (item.EnchantedModValues != null && i < item.EnchantedModValues.Count)
                        {
                            var values = item.EnchantedModValues[i];
                            foreach (var value in values)
                            {
                                bool valueMatches = true;
                                
                                if (_minValue.HasValue && value < _minValue.Value)
                                    valueMatches = false;
                                
                                if (_maxValue.HasValue && value > _maxValue.Value)
                                    valueMatches = false;
                                
                                if (valueMatches)
                                    return true;
                            }
                        }
                    }
                }
            }
            
            return false;
        }
    }

    // UPDATED: Single HumanStatContainsCondition with both constructors
    public class HumanStatContainsCondition : IItemCondition
    {
        private readonly string _searchText;
        private readonly string _baseName;
        
        public HumanStatContainsCondition(string searchText)
        {
            _searchText = searchText;
            _baseName = "";
        }
        
        public HumanStatContainsCondition(string baseName, string searchText)
        {
            _baseName = baseName;
            _searchText = searchText;
        }
        
        public bool Matches(ItemData item)
        {
            if (!string.IsNullOrEmpty(_baseName) && 
                !string.Equals(item.BaseName, _baseName, StringComparison.OrdinalIgnoreCase))
                return false;
            
            if (item.HumanStats != null)
            {
                return item.HumanStats.Any(stat => 
                    stat.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            
            return false;
        }
    }

    // Optional: Condition that checks ModTranslations (for trinkets)
    public class ModTranslationContainsCondition : IItemCondition
    {
        private readonly string _searchText;
        private readonly string _baseName;
        
        public ModTranslationContainsCondition(string searchText)
        {
            _searchText = searchText;
            _baseName = "";
        }
        
        public ModTranslationContainsCondition(string baseName, string searchText)
        {
            _baseName = baseName;
            _searchText = searchText;
        }
        
        public bool Matches(ItemData item)
        {
            if (!string.IsNullOrEmpty(_baseName) && 
                !string.Equals(item.BaseName, _baseName, StringComparison.OrdinalIgnoreCase))
                return false;
            
            if (item.ModTranslations != null)
            {
                return item.ModTranslations.Any(stat => 
                    stat.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            
            return false;
        }
    }

    public class ItemData
    {
        public string DisplayName { get; set; }
        public string BaseName { get; set; }
        public List<string> EnchantedStats { get; set; }
        public List<string> HumanStats { get; set; }
        public List<string> ModTranslations { get; set; }
        public List<List<float>> EnchantedModValues { get; set; }
        
        public ItemData(string displayName, string baseName, 
            List<string> enchantedStats, List<string> humanStats,
            List<string> modTranslations,
            List<List<float>> enchantedModValues = null)
        {
            DisplayName = displayName;
            BaseName = baseName;
            EnchantedStats = enchantedStats ?? new List<string>();
            HumanStats = humanStats ?? new List<string>();
            ModTranslations = modTranslations ?? new List<string>();
            EnchantedModValues = enchantedModValues ?? new List<List<float>>();
        }
    }
}