using System.Collections.Generic;
using CurioDataScience.Item;

namespace CurioDataScience.Config
{
    public class FilterRule
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public IItemCondition Condition { get; set; }
    }

    public static class FilterConfig
    {
        public static List<FilterRule> GetDefaultRules()
        {
            return new List<FilterRule>
            {
                new FilterRule
                {
                    Id = "divine-orb",
                    Name = "Divine Orbs",
                    Enabled = true,
                    Condition = new BaseNameCondition("Divine Orb")
                },
                new FilterRule
                {
                    Id = "vaal-regalia-enchanted",
                    Name = "Enchanted Vaal Regalia",
                    Enabled = true,
                    Condition = new CompositeCondition(item =>
                        string.Equals(item.BaseName, "Vaal Regalia", StringComparison.OrdinalIgnoreCase) &&
                        item.EnchantedStats?.Contains("Enchantment Defence Modifier Effect") == true)
                },
                new FilterRule
                {
                    Id = "replica-cortex",
                    Name = "Replica Cortex",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Cortex")
                }
            };
        }
        
        public static void AddCustomRule(List<FilterRule> rules, string name, IItemCondition condition)
        {
            rules.Add(new FilterRule
            {
                Id = $"custom-{rules.Count + 1}",
                Name = name,
                Enabled = true,
                Condition = condition
            });
        }
    }
}