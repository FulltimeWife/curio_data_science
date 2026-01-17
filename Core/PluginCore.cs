using System;
using System.Collections.Generic;
using System.Linq;
using CurioDataScience.Enums;
using CurioDataScience.Data;
using CurioDataScience.Item;
using CurioDataScience.UI;
using CurioDataScience.Config;
using ExileCore;

namespace CurioDataScience.Core
{
    public class PluginCore
    {
        private readonly GameController _gameController;
        private readonly EventManager _eventManager;
        private readonly ItemProcessor _itemProcessor;
        private readonly BufferManager _bufferManager;
        private readonly CsvExporter _csvExporter;
        private readonly RenderHelper _renderHelper;
        private readonly ItemFilter _itemFilter;
        private readonly UIManager _uiManager;
        
        private DateTime _lastBufferCheck = DateTime.MinValue;
        private readonly TimeSpan _bufferCheckInterval = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _completionTimeout = TimeSpan.FromSeconds(10);
        
        public PluginCore(GameController gameController, Graphics graphics)
        {
            _gameController = gameController;
            _eventManager = new EventManager();
            
            _itemProcessor = new ItemProcessor(gameController);
            _bufferManager = new BufferManager();
            _csvExporter = new CsvExporter();
            _renderHelper = new RenderHelper();
            _itemFilter = new ItemFilter();
            
            _uiManager = new UIManager(graphics, _eventManager, _itemFilter, _bufferManager, _renderHelper);
            
            SetupEventHandlers();
        }
        
        private void SetupEventHandlers()
        {
            _eventManager.SubscribeToItemFound(OnItemFound);
            
            _eventManager.SubscribeToFilterChanged(OnFilterChanged);
            
            _eventManager.Subscribe("ForceExport", data => ForceExport());
            
            _eventManager.Subscribe("ClearBuffer", data => ClearBuffer());
            
            _eventManager.Subscribe("CloseFilterUI", data => DebugWindow.LogMsg("Filter UI closed", 2));
        }
        
        public void Update()
        {
            try
            {
                var currentRewards = _itemProcessor.GetHeistRewards();
                
                foreach (var reward in currentRewards)
                {
                    _eventManager.PublishItemFound(reward);
                }
                
                _bufferManager.UpdateBufferState(currentRewards, _itemProcessor);
                _uiManager.Update();
                
                CheckForBufferFlush();
            }
            catch (Exception e)
            {
                DebugWindow.LogError($"[Heist Data Science] Update error: {e}");
            }
        }
        
        public void Render()
        {
            try
            {
                _uiManager.Render();
            }
            catch (Exception e)
            {
                DebugWindow.LogError($"[Heist Data Science] Render error: {e}");
            }
        }
        
        public void OnKeyDown(Keys key)
        {
            _uiManager.OnKeyDown(key);
        }
        
        private void OnItemFound(Item.HeistRewardInfo item)
        {
            if (_itemFilter.ShouldDisplayItem(item))
            {
                DebugWindow.LogMsg($"Filter matched: {item.DisplayName}", 2);
            }
        }
        
        private void OnFilterChanged(List<FilterRule> rules)
        {
            DebugWindow.LogMsg($"Filters updated: {rules.Count(r => r.Enabled)} enabled", 2);
        }
        
        private void CheckForBufferFlush()
        {
            var completedCount = _bufferManager.GetCompletedCount();
            
            if (completedCount >= Constants.BufferSize)
            {
                FlushCompletedItems();
                return;
            }
            
            if (DateTime.Now - _lastBufferCheck > _bufferCheckInterval)
            {
                var visibleCount = _bufferManager.GetVisibleCount();
                var newCount = _bufferManager.GetNewCount();
                
                if (completedCount > 0 && visibleCount == 0 && newCount == 0)
                {
                    var oldestCompletion = _bufferManager.GetOldestCompleted();
                    
                    if (oldestCompletion != null && 
                        (DateTime.Now - oldestCompletion.LastSeen) > _completionTimeout)
                    {
                        FlushCompletedItems();
                    }
                }
                
                _lastBufferCheck = DateTime.Now;
            }
        }
        
        private void FlushCompletedItems()
        {
            var toExport = _bufferManager.GetCompletedItemsForExport();
            
            if (toExport.Count == 0)
                return;
            
            try
            {
                _csvExporter.ExportRewardsToCsv(toExport, GetType().Assembly.Location);
                _bufferManager.MarkItemsAsExported(toExport);
                
                _eventManager.Publish("ExportComplete", toExport.Count);
                DebugWindow.LogMsg($"Exported {toExport.Count} items to CSV", 5);
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"[Heist Data Science] Export failed: {ex}");
            }
        }
        
        private void ForceExport()
        {
            var items = _bufferManager.GetCompletedItemsForExport();
            if (items.Count > 0)
            {
                FlushCompletedItems();
            }
            else
            {
                DebugWindow.LogMsg("No completed items to export", 2);
            }
        }
        
        private void ClearBuffer()
        {
            _bufferManager.Clear();
            DebugWindow.LogMsg("Buffer cleared", 2);
        }
    }
}