using System;

namespace CurioDataScience.Item
{
    public interface IItemCondition
    {
        bool Matches(HeistRewardInfo item);
    }

    public class BaseNameCondition : IItemCondition
    {
        private readonly string _baseName;
        
        public BaseNameCondition(string baseName)
        {
            _baseName = baseName;
        }
        
        public bool Matches(HeistRewardInfo item)
        {
            return string.Equals(item.BaseName, _baseName, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class EnchantmentCondition : IItemCondition
    {
        private readonly string _baseName;
        private readonly string _enchantment;
        
        public EnchantmentCondition(string baseName, string enchantment)
        {
            _baseName = baseName;
            _enchantment = enchantment;
        }
        
        public bool Matches(HeistRewardInfo item)
        {
            return string.Equals(item.BaseName, _baseName, StringComparison.OrdinalIgnoreCase) &&
                   item.EnchantedStats?.Contains(_enchantment) == true;
        }
    }

    public class DisplayNameCondition : IItemCondition
    {
        private readonly string _displayName;
        
        public DisplayNameCondition(string displayName)
        {
            _displayName = displayName;
        }
        
        public bool Matches(HeistRewardInfo item)
        {
            return string.Equals(item.DisplayName, _displayName, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class CompositeCondition : IItemCondition
    {
        private readonly Func<HeistRewardInfo, bool> _condition;
        
        public CompositeCondition(Func<HeistRewardInfo, bool> condition)
        {
            _condition = condition;
        }
        
        public bool Matches(HeistRewardInfo item) => _condition(item);
    }
}