using System.Collections.Generic;
using System.Linq;
using CurioDataScience.Config;

namespace CurioDataScience.Item
{
    public class ItemFilter
    {
        private readonly List<FilterRule> _rules;
        
        public ItemFilter()
        {
            _rules = FilterConfig.GetDefaultRules();
        }
        
        public ItemFilter(List<FilterRule> rules)
        {
            _rules = rules;
        }
        
        public bool ShouldDisplayItem(HeistRewardInfo item)
        {
            return _rules
                .Where(rule => rule.Enabled)
                .Any(rule => rule.Condition.Matches(item));
        }
        
        public List<FilterRule> GetMatchingRules(HeistRewardInfo item)
        {
            return _rules
                .Where(rule => rule.Enabled && rule.Condition.Matches(item))
                .ToList();
        }
        
        public void AddRule(FilterRule rule)
        {
            _rules.Add(rule);
        }
        
        public void UpdateRule(string id, bool enabled)
        {
            var rule = _rules.FirstOrDefault(r => r.Id == id);
            if (rule != null)
            {
                rule.Enabled = enabled;
            }
        }
        
        public void RemoveRule(string id)
        {
            _rules.RemoveAll(r => r.Id == id);
        }
    }
}