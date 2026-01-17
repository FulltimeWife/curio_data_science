using System;
using System.Collections.Generic;
using SharpDX;
using ExileCore;
using CurioDataScience.Item;
using CurioDataScience.Data;
using CurioDataScience.Config;
using CurioDataScience.Enums;
using CurioDataScience.Core;

namespace CurioDataScience.UI
{
    public class UIManager
    {
        private readonly Graphics _graphics;
        private readonly EventManager _eventManager;
        private readonly ItemFilter _itemFilter;
        private readonly BufferManager _bufferManager;
        private readonly RenderHelper _renderHelper;
        private readonly FilterUI _filterUI;
        private readonly DebugWindow _debugWindow;
        
        private bool _showDebugWindow = false;
        private bool _showFilterUI = false;
        
        public UIManager(Graphics graphics, EventManager eventManager, ItemFilter itemFilter, 
                        BufferManager bufferManager, RenderHelper renderHelper)
        {
            _graphics = graphics;
            _eventManager = eventManager;
            _itemFilter = itemFilter;
            _bufferManager = bufferManager;
            _renderHelper = renderHelper;
            
            _filterUI = new FilterUI(_itemFilter, _eventManager);
            _debugWindow = new DebugWindow();
            
            SetupEventHandlers();
        }
        
        private void SetupEventHandlers()
        {
            _eventManager.SubscribeToBufferUpdated(OnBufferUpdated);
            _eventManager.SubscribeToFilterChanged(OnFilterChanged);
        }
        
        public void Render()
        {
            try
            {
                DrawMainUI();
                
                if (_showDebugWindow)
                {
                    _debugWindow.Render(_graphics, _bufferManager);
                }
                
                if (_showFilterUI)
                {
                    _filterUI.Render(_graphics);
                }
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"UI Manager render error: {ex}");
            }
        }
        
        private void DrawMainUI()
        {
            _renderHelper.ShowBufferStatus(_graphics, _bufferManager);
            
            var currentItems = _bufferManager.LastVisibleRewards;
            _renderHelper.DisplayCurrentRewards(_graphics, currentItems, _itemFilter);
            
            DrawControlsOverlay();
        }
        
        private void DrawControlsOverlay()
        {
            var controlsPos = new System.Numerics.Vector2(50, 100);
            
            _graphics.DrawText("Heist Data Science Controls", controlsPos, Color.White);
            controlsPos.Y += 20;
            
            _graphics.DrawText("F1: Toggle Debug Window", controlsPos, Color.LightGray);
            controlsPos.Y += 16;
            
            _graphics.DrawText("F2: Toggle Filter UI", controlsPos, Color.LightGray);
            controlsPos.Y += 16;
            
            _graphics.DrawText("F3: Export Now", controlsPos, Color.LightGray);
            controlsPos.Y += 16;
            
            _graphics.DrawText("F4: Clear Buffer", controlsPos, Color.LightGray);
        }
        
        public void OnKeyDown(Keys key)
        {
            switch (key)
            {
                case Keys.F1:
                    _showDebugWindow = !_showDebugWindow;
                    break;
                    
                case Keys.F2:
                    _showFilterUI = !_showFilterUI;
                    break;
                    
                case Keys.F3:
                    _eventManager.Publish("ForceExport", null);
                    break;
                    
                case Keys.F4:
                    _eventManager.Publish("ClearBuffer", null);
                    break;
            }
        }
        
        private void OnBufferUpdated(BufferManager bufferManager)
        {
            _debugWindow.UpdateBufferInfo(bufferManager);
        }
        
        private void OnFilterChanged(List<FilterRule> rules)
        {
            _filterUI.UpdateFilterRules(rules);
        }
        
        public void Update()
        {
            _filterUI.Update();
            _debugWindow.Update();
        }
    }
}