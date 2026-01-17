using System;
using System.Collections.Generic;
using System.Linq;
using CurioDataScience.Item;

namespace CurioDataScience.Data
{
    public class BufferManager
    {
        private readonly List<BufferedReward> _bufferedRewards = new();
        private readonly HashSet<uint> _exportedEntityIds = new();
        private DateTime _lastBufferCheck = DateTime.MinValue;
        private DateTime _lastBufferUpdate = DateTime.MinValue;
        
        private readonly TimeSpan _bufferCheckInterval = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _completionTimeout = TimeSpan.FromSeconds(10);
        private readonly TimeSpan _bufferUpdateCooldown = TimeSpan.FromMilliseconds(500);
        
        public List<HeistRewardInfo> LastVisibleRewards { get; private set; } = new();
        
        public void UpdateBufferState(List<HeistRewardInfo> currentRewards, ItemProcessor processor)
        {
            LastVisibleRewards = currentRewards.ToList();
            
            if ((DateTime.Now - _lastBufferUpdate) < _bufferUpdateCooldown)
                return;
            
            _lastBufferUpdate = DateTime.Now;
            
            var updatedAny = false;
            
            foreach (var buffered in _bufferedRewards.ToList())
            {
                bool isCurrentlyVisible = false;
                HeistRewardInfo matchingReward = null;
                
                foreach (var reward in currentRewards)
                {
                    var hash = processor.CreateContentHash(reward);
                    if (hash == buffered.ContentHash)
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
                        }
                        else
                        {
                            _bufferedRewards.Remove(buffered);
                            updatedAny = true;
                        }
                    }
                }
                else if (buffered.State == RewardState.New)
                {
                    var timeAsNew = DateTime.Now - buffered.FirstSeen;
                    if (timeAsNew.TotalSeconds > 5.0)
                    {
                        _bufferedRewards.Remove(buffered);
                        updatedAny = true;
                    }
                }
                
                if (buffered.State == RewardState.Exported && 
                    (DateTime.Now - buffered.LastSeen).TotalMinutes > 5)
                {
                    _bufferedRewards.Remove(buffered);
                    updatedAny = true;
                }
            }
            
            foreach (var reward in currentRewards)
            {
                var hash = processor.CreateContentHash(reward);
                
                bool alreadyExists = _bufferedRewards.Any(b => b.ContentHash == hash);
                if (alreadyExists)
                {
                    var existing = _bufferedRewards.First(b => b.ContentHash == hash);
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
                    continue;
                }
                
                _bufferedRewards.Add(new BufferedReward
                {
                    Reward = reward,
                    ContentHash = hash,
                    State = RewardState.New,
                    FirstSeen = DateTime.Now,
                    LastSeen = DateTime.Now,
                    LastVisible = DateTime.Now,
                    VisibilityCount = 1,
                    IsExported = false
                });
                
                updatedAny = true;
            }
            
            if (updatedAny)
            {
                _lastBufferCheck = DateTime.Now;
            }
        }
        
        public List<HeistRewardInfo> GetCompletedItemsForExport()
        {
            return _bufferedRewards
                .Where(b => b.State == RewardState.Completed && !b.IsExported)
                .Select(b => b.Reward)
                .ToList();
        }
        
        public void MarkItemsAsExported(List<HeistRewardInfo> exportedItems)
        {
            foreach (var buffered in _bufferedRewards.Where(b => 
                     b.State == RewardState.Completed && 
                     exportedItems.Contains(b.Reward)))
            {
                buffered.State = RewardState.Exported;
                buffered.IsExported = true;
                _exportedEntityIds.Add(buffered.Reward.EntityId);
                if (buffered.Reward.ItemOnGroundId.HasValue)
                {
                    _exportedEntityIds.Add(buffered.Reward.ItemOnGroundId.Value);
                }
            }
            
            _bufferedRewards.Clear();
            _exportedEntityIds.Clear();
        }
        
        public void Clear()
        {
            _bufferedRewards.Clear();
            _exportedEntityIds.Clear();
        }
        
        public int GetCompletedCount() => 
            _bufferedRewards.Count(b => b.State == RewardState.Completed);
        
        public int GetVisibleCount() => 
            _bufferedRewards.Count(b => b.State == RewardState.Visible);
        
        public int GetNewCount() => 
            _bufferedRewards.Count(b => b.State == RewardState.New);
        
        public int GetExportedCount() => 
            _bufferedRewards.Count(b => b.State == RewardState.Exported);
        
        public int GetTotalBuffered() => _bufferedRewards.Count;
        
        public List<BufferedReward> GetRecentCompleted(int count = 3)
        {
            return _bufferedRewards
                .Where(b => b.State == RewardState.Completed)
                .OrderByDescending(b => b.LastSeen)
                .Take(count)
                .ToList();
        }
        
        public BufferedReward GetOldestCompleted()
        {
            return _bufferedRewards
                .Where(b => b.State == RewardState.Completed)
                .OrderBy(b => b.LastSeen)
                .FirstOrDefault();
        }
        
        public TimeSpan GetTimeUntilTimeout()
        {
            var oldest = GetOldestCompleted();
            if (oldest == null) return TimeSpan.Zero;
            
            var timeSinceCompletion = DateTime.Now - oldest.LastSeen;
            var timeUntilTimeout = _completionTimeout - timeSinceCompletion;
            return timeUntilTimeout > TimeSpan.Zero ? timeUntilTimeout : TimeSpan.Zero;
        }
    }
}