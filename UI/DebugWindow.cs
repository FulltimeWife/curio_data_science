using System;
using System.Linq;
using SharpDX;
using ExileCore;
using CurioDataScience.Data;
using CurioDataScience.Item;

namespace CurioDataScience.UI
{
    public class DebugWindow
    {
        private System.Numerics.Vector2 _position = new System.Numerics.Vector2(800, 100);
        private System.Numerics.Vector2 _size = new System.Numerics.Vector2(400, 300);
        
        private BufferManager _bufferManager;
        private DateTime _lastUpdate = DateTime.MinValue;
        
        public void Render(Graphics graphics, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            
            graphics.DrawBox(_position, _size, new Color(0, 0, 0, 200));
            
            graphics.DrawText("Debug Information", 
                new System.Numerics.Vector2(_position.X + 10, _position.Y + 5), 
                Color.Yellow);
            
            var yPos = _position.Y + 30;
            DrawDebugInfo(graphics, ref yPos);
        }
        
        private void DrawDebugInfo(Graphics graphics, ref float yPos)
        {
            if (_bufferManager == null) return;
            
            var xPos = _position.X + 10;
            
            graphics.DrawText($"Last Update: {DateTime.Now:HH:mm:ss.fff}", 
                new System.Numerics.Vector2(xPos, yPos), Color.White);
            yPos += 16;
            
            graphics.DrawText($"Total Items Processed: {_bufferManager.GetTotalBuffered()}", 
                new System.Numerics.Vector2(xPos, yPos), Color.LightGray);
            yPos += 16;
            
            var completed = _bufferManager.GetCompletedCount();
            graphics.DrawText($"Completed Items: {completed}", 
                new System.Numerics.Vector2(xPos, yPos), 
                completed > 0 ? Color.LightGreen : Color.LightGray);
            yPos += 16;
            
            var visible = _bufferManager.GetVisibleCount();
            graphics.DrawText($"Visible Items: {visible}", 
                new System.Numerics.Vector2(xPos, yPos), Color.LightBlue);
            yPos += 16;
            
            var recentItems = _bufferManager.GetRecentCompleted(5);
            if (recentItems.Count > 0)
            {
                yPos += 10;
                graphics.DrawText("Recently Completed:", 
                    new System.Numerics.Vector2(xPos, yPos), Color.Yellow);
                yPos += 16;
                
                foreach (var item in recentItems)
                {
                    var timeAgo = (DateTime.Now - item.LastSeen).TotalSeconds;
                    var text = $"{item.Reward.DisplayName} ({timeAgo:F1}s ago)";
                    graphics.DrawText(text, 
                        new System.Numerics.Vector2(xPos + 10, yPos), Color.LightGreen);
                    yPos += 14;
                }
            }
        }
        
        public void Update()
        {
            if ((DateTime.Now - _lastUpdate).TotalSeconds >= 0.5)
            {
                _lastUpdate = DateTime.Now;
            }
        }
        
        public void UpdateBufferInfo(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
        }
        
        public static void LogMsg(string message, int time = 1)
        {
            DebugWindow.LogMsg(message, time);
        }
        
        public static void LogError(string message, int time = 10)
        {
            DebugWindow.LogError(message, time);
        }
    }
}