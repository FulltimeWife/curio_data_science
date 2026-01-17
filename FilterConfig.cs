using System.Collections.Generic;

namespace CurioDataScience
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
                    Id = "exalted-orb",
                    Name = "Exalted Orbs",
                    Enabled = true,
                    Condition = new BaseNameCondition("Exalted Orb")
                },
                new FilterRule
                {
                    Id = "vaal-orb",
                    Name = "Vaal Orbs",
                    Enabled = true,
                    Condition = new BaseNameCondition("Vaal Orb")
                },
                new FilterRule
                {
                    Id = "gemcutter-prism",
                    Name = "Gemcutter's Prisms",
                    Enabled = true,
                    Condition = new BaseNameCondition("Gemcutter's Prism")
                },
                new FilterRule
                {
                    Id = "scouring-orb",
                    Name = "Scouring Orbs",
                    Enabled = true,
                    Condition = new BaseNameCondition("Orb of Scouring")
                },
                new FilterRule
                {
                    Id = "regret-orb",
                    Name = "Regret Orbs",
                    Enabled = true,
                    Condition = new BaseNameCondition("Orb of Regret")
                },
                new FilterRule
                {
                    Id = "chaos-orb",
                    Name = "Chaos Orbs",
                    Enabled = true,
                    Condition = new BaseNameCondition("Chaos Orb")
                },
                new FilterRule
                {
                    Id = "regal-orb",
                    Name = "Regal Orbs",
                    Enabled = true,
                    Condition = new BaseNameCondition("Regal Orb")
                },
                new FilterRule
                {
                    Id = "tempering-orb",
                    Name = "Tempering Orbs",
                    Enabled = true,
                    Condition = new BaseNameCondition("Tempering Orb")
                },
                new FilterRule
                {
                    Id = "tailoring-orb",
                    Name = "Tailoring Orbs",
                    Enabled = true,
                    Condition = new BaseNameCondition("Tailoring Orb")
                },
                new FilterRule
                {
                    Id = "transfer-attuned-spirit-shield",
                    Name = "Transfer Attuned Spirit Shield",
                    Enabled = true,
                    Condition = new BaseNameCondition("Transfer-attuned Spirit Shield")
                },
                new FilterRule
                {
                    Id = "focused-amulet",
                    Name = "Focused Amulet",
                    Enabled = true,
                    Condition = new BaseNameCondition("Focused Amulet")
                },   
                new FilterRule
                {
                    Id = "simplex-amulet",
                    Name = "Simplex Amulet",
                    Enabled = true,
                    Condition = new BaseNameCondition("Simplex Amulet")
                },
                new FilterRule
                {
                    Id = "astrolabe-amulet",
                    Name = "Astrolabe Amulet",
                    Enabled = true,
                    Condition = new BaseNameCondition("Astrolabe Amulet")
                },
                new FilterRule
                {
                    Id = "cogwork-ring",
                    Name = "Cogwork Ring",
                    Enabled = true,
                    Condition = new BaseNameCondition("Cogwork Ring")
                },
                new FilterRule
                {
                    Id = "composite-ring",
                    Name = "Composite Ring",
                    Enabled = true,
                    Condition = new BaseNameCondition("Composite Ring")
                },
                new FilterRule
                {
                    Id = "geodesic-ring",
                    Name = "Geodesic Ring",
                    Enabled = true,
                    Condition = new BaseNameCondition("Geodesic Ring")
                },
                new FilterRule
                {
                    Id = "helical-ring",
                    Name = "Helical Ring",
                    Enabled = true,
                    Condition = new BaseNameCondition("Helical Ring")
                },
                new FilterRule
                {
                    Id = "manifold-ring",
                    Name = "Manifold Ring",
                    Enabled = true,
                    Condition = new BaseNameCondition("Manifold Ring")
                },
                new FilterRule
                {
                    Id = "ratcheting-ring",
                    Name = "Ratcheting Ring",
                    Enabled = true,
                    Condition = new BaseNameCondition("Ratcheting Ring")
                },
                new FilterRule
                {
                    Id = "replica-cortex",
                    Name = "Replica Cortex",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Cortex")
                },
                new FilterRule
                {
                    Id = "replica-bated-breath",
                    Name = "Replica Bated Breath",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Bated Breath")
                },
                new FilterRule
                {
                    Id = "replica-eternity-shroud",
                    Name = "Replica Eternity Shroud",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Eternity Shroud")
                },
                new FilterRule
                {
                    Id = "replica-shroud-of-the-lightless",
                    Name = "Replica Shroud of the Lightless",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Shroud of the Lightless")
                },
                new FilterRule
                {
                    Id = "replica-kongor's-undying-rage",
                    Name = "Replica Kongor's Undying Rage",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Kongor's Undying Rage")
                },
                new FilterRule
                {
                    Id = "replica-duskdawn",
                    Name = "Replica Duskdawn",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Duskdawn")
                },
                new FilterRule
                {
                    Id = "replica-tukohama's-fortress",
                    Name = "Replica Tukohama's Fortress",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Tukohama's Fortress")
                },
                new FilterRule
                {
                    Id = "replica-headhunter",
                    Name = "Replica Headhunter",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Headhunter")
                },
                new FilterRule
                {
                    Id = "replica-farrul's-fur",
                    Name = "Replica Farrul's Fur",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Farrul's Fur")
                },
                new FilterRule
                {
                    Id = "replica-nebulis",
                    Name = "Replica Nebulis",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Nebulis")
                },
                new FilterRule
                {
                    Id = "replica-atziri's-acuity",
                    Name = "Replica Atziri's Acuity",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Atziri's Acuity")
                },
                new FilterRule
                {
                    Id = "replica-alberon's-warpath",
                    Name = "Replica Alberon's Warpath",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Alberon's Warpath")
                },
                new FilterRule
                {
                    Id = "replica-maloney's-mechanism",
                    Name = "Replica Maloney's Mechanism",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Maloney's Mechanism")
                },
                new FilterRule
                {
                    Id = "replica-trypanon",
                    Name = "Replica Trypanon",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Trypanon")
                },
                new FilterRule
                {
                    Id = "replica-hyrri's-ire",
                    Name = "Replica Hyrri's Ire",
                    Enabled = true,
                    Condition = new DisplayNameCondition("Replica Hyrri's Ire")
                },
                new FilterRule
                {
                    Id = "trinket-divine-orb",
                    Name = "Trinket Divine Orb",
                    Enabled = true,
                    Condition = new HumanStatContainsCondition("Trinket", "Divine Orb")
                },
                new FilterRule
                {
                    Id = "trinket-jewellery",
                    Name = "Trinket Jewellery",
                    Enabled = true,
                    Condition = new HumanStatContainsCondition("Trinket", "Jewellery")
                },
            };
        }
    }
}