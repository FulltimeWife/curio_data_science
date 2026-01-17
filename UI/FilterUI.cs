using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using ExileCore;
using CurioDataScience.Item;
using CurioDataScience.Config;

namespace CurioDataScience.UI
{
    public class FilterUI
    {
        private readonly ItemFilter _itemFilter;
        private readonly EventManager _eventManager;
        
        private System.Numerics.Vector2 _position = new System.Numerics.Vector2(400, 100);
        private System.Numerics.Vector2 _size = new System.Numerics.Vector2(400, 500);
        
        private List<FilterRule> _displayRules;
        private bool _isDragging = false;
        private System.Numerics.Vector2 _dragOffset;
        
        public FilterUI(ItemFilter itemFilter, EventManager eventManager)
        {
            _itemFilter = itemFilter;
            _eventManager = eventManager;
            _displayRules = FilterConfig.GetDefaultRules();
        }
        
        public void Render(Graphics graphics)
        {
            DrawWindow(graphics);
            DrawFilterRules(graphics);
        }
        
        private void DrawWindow(Graphics graphics)
        {
            graphics.DrawBox(_position, _size, Color.DarkSlateGray);
            
            var titlePos = new System.Numerics.Vector2(_position.X + 10, _position.Y + 5);
            graphics.DrawText("Filter Configuration", titlePos, Color.White);
            
            var closeButtonPos = new System.Numerics.Vector2(_position.X + _size.X - 25, _position.Y + 5);
            graphics.DrawText("X", closeButtonPos, Color.Red);
        }
        
        private void DrawFilterRules(Graphics graphics)
        {
            var yPos = _position.Y + 30;
            var xPos = _position.X + 10;
            
            graphics.DrawText("Active Filters:", new System.Numerics.Vector2(xPos, yPos), Color.Yellow);
            yPos += 20;
            
            foreach (var rule in _displayRules)
            {
                var ruleColor = rule.Enabled ? Color.LightGreen : Color.LightGray;
                
                var checkboxPos = new System.Numerics.Vector2(xPos, yPos);
                graphics.DrawText(rule.Enabled ? "[✓]" : "[ ]", checkboxPos, ruleColor);
                
                var namePos = new System.Numerics.Vector2(xPos + 30, yPos);
                graphics.DrawText(rule.Name, namePos, ruleColor);
                
                var conditionPos = new System.Numerics.Vector2(xPos + 200, yPos);
                var conditionText = GetConditionTypeText(rule.Condition);
                graphics.DrawText(conditionText, conditionPos, Color.LightBlue);
                
                yPos += 20;
            }
            
            yPos += 10;
            var enabledCount = _displayRules.Count(r => r.Enabled);
            graphics.DrawText($"Enabled: {enabledCount}/{_displayRules.Count}", 
                new System.Numerics.Vector2(xPos, yPos), Color.White);
        }
        
        private string GetConditionTypeText(IItemCondition condition)
        {
            return condition switch
            {
                BaseNameCondition => "Base Name",
                DisplayNameCondition => "Display Name",
                EnchantmentCondition => "Enchantment",
                CompositeCondition => "Composite",
                _ => "Custom"
            };
        }
        
        public void Update()
        {
        }
        
        public void UpdateFilterRules(List<FilterRule> rules)
        {
            _displayRules = rules;
        }
        
        public bool IsMouseOverWindow(System.Numerics.Vector2 mousePos)
        {
            return mousePos.X >= _position.X && mousePos.X <= _position.X + _size.X &&
                   mousePos.Y >= _position.Y && mousePos.Y <= _position.Y + _size.Y;
        }
        
        public void OnMouseDown(System.Numerics.Vector2 mousePos)
        {
            if (IsMouseOverWindow(mousePos))
            {
                var relativeY = mousePos.Y - _position.Y - 50;
                var ruleIndex = (int)(relativeY / 20);
                
                if (ruleIndex >= 0 && ruleIndex < _displayRules.Count)
                {
                    _displayRules[ruleIndex].Enabled = !_displayRules[ruleIndex].Enabled;
                    _eventManager.PublishFilterChanged(_displayRules);
                }
                
                if (mousePos.X >= _position.X + _size.X - 25 && 
                    mousePos.Y <= _position.Y + 25)
                {
                    _eventManager.Publish("CloseFilterUI", null);
                }
            }
        }
    }
}