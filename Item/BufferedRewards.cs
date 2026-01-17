using System;

namespace CurioDataScience.Item
{
    public class BufferedReward
    {
        public HeistRewardInfo Reward { get; set; }
        public RewardState State { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public DateTime LastVisible { get; set; }
        public int VisibilityCount { get; set; }
        public bool IsExported { get; set; }
        public string ContentHash { get; set; }
    }
}