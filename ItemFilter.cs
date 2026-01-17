using System.Collections.Generic;
using System.Linq;
using ExileCore;

namespace CurioDataScience
{
    public class ItemFilter
    {
        private readonly List<FilterRule> _rules;
        
        public ItemFilter()
        {
            _rules = FilterConfig.GetDefaultRules();
        }
        
        public bool ShouldDisplayItem(ItemData itemData)
        {
            return _rules
                .Where(rule => rule.Enabled)
                .Any(rule => rule.Condition.Matches(itemData));
        }
        
        public string GetMatchingFilterName(ItemData itemData)
        {
            var matchingRule = _rules
                .Where(rule => rule.Enabled)
                .FirstOrDefault(rule => rule.Condition.Matches(itemData));
            
            return matchingRule?.Name;
        }
    }
}