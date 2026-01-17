using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using CurioDataScience.Core;
using CurioDataScience.Item;
using CurioDataScience.Data;

namespace CurioDataScience.UI
{
    public class RenderHelper
    {
        public void ShowBufferStatus(Graphics graphics, BufferManager bufferManager)
        {
            var startX = 50f;
            var yCursor = (float)Constants.TextPositionY;
            
            var completedCount = bufferManager.GetCompletedCount();
            var visibleCount = bufferManager.GetVisibleCount();
            var newCount = bufferManager.GetNewCount();
            var exportedCount = bufferManager.GetExportedCount();
            var totalBuffered = bufferManager.GetTotalBuffered();
            
            graphics.DrawText($"Buffer Status", new System.Numerics.Vector2(startX, yCursor), Color.White);
            yCursor += 20;
            
            graphics.DrawText($"Completed: {completedCount}/{Constants.BufferSize} to flush", 
                new System.Numerics.Vector2(startX, yCursor), 
                completedCount >= Constants.BufferSize ? Color.LimeGreen : Color.Yellow);
            yCursor += 20;
            
            graphics.DrawText($"Visible: {visibleCount} | New: {newCount} | Total: {totalBuffered} | Exported: {exportedCount}", 
                new System.Numerics.Vector2(startX, yCursor), Color.LightGray);
            yCursor += 20;
            
            if (completedCount > 0 && completedCount < Constants.BufferSize)
            {
                var timeUntilTimeout = bufferManager.GetTimeUntilTimeout();
                
                if (timeUntilTimeout.TotalSeconds > 0)
                {
                    graphics.DrawText($"Timeout in: {timeUntilTimeout.TotalSeconds:F0}s", 
                        new System.Numerics.Vector2(startX, yCursor), Color.LightBlue);
                    yCursor += 20;
                }
            }
            
            yCursor += 10;
            
            var recentCompleted = bufferManager.GetRecentCompleted(3);
            
            if (recentCompleted.Count > 0)
            {
                graphics.DrawText("Recently completed:", new System.Numerics.Vector2(startX, yCursor), Color.White);
                yCursor += 20;
                
                foreach (var buffered in recentCompleted)
                {
                    var name = buffered.Reward.DisplayName.Length > 20 
                        ? buffered.Reward.DisplayName.Substring(0, 20) + "..." 
                        : buffered.Reward.DisplayName;
                    
                    graphics.DrawText($"• {name}", new System.Numerics.Vector2(startX + 10, yCursor), Color.LightGreen);
                    yCursor += 16;
                }
            }
        }
        
        public void DisplayCurrentRewards(Graphics graphics, List<HeistRewardInfo> rewards, ItemFilter filter)
        {
            if (rewards.Count == 0) return;
            
            var colors = new[] { 
                Constants.DefaultColor, 
                Constants.ReplicaColor, 
                Constants.TrinketColor, 
                Constants.CurrencyColor, 
                Constants.ScarabColor 
            };
            
            var startX = 50f;
            var yCursor = 400f;
            
            var filteredRewards = rewards.Where(r => filter.ShouldDisplayItem(r)).ToList();
            
            graphics.DrawText($"Currently Visible ({filteredRewards.Count}/{rewards.Count}):", 
                new System.Numerics.Vector2(startX, yCursor), Color.White);
            yCursor += 20;
            
            var maxToShow = Math.Min(5, filteredRewards.Count);
            for (int i = 0; i < maxToShow; i++)
            {
                var info = filteredRewards[i];
                var color = colors[i % colors.Length];
                
                var matchingRules = filter.GetMatchingRules(info);
                var ruleText = matchingRules.Count > 0 ? $" [{string.Join(", ", matchingRules.Select(r => r.Name))}]" : "";
                
                graphics.DrawText($"{i+1}. {info.DisplayName}{ruleText}", 
                    new System.Numerics.Vector2(startX, yCursor), color);
                yCursor += 16;
            }
            
            if (filteredRewards.Count < rewards.Count)
            {
                yCursor += 10;
                graphics.DrawText($"({rewards.Count - filteredRewards.Count} items hidden by filter)", 
                    new System.Numerics.Vector2(startX, yCursor), Color.Gray);
            }
        }
    }
}