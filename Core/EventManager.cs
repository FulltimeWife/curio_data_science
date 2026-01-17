using System;
using System.Collections.Generic;

namespace CurioDataScience.Core
{
    public class EventManager
    {
        private readonly Dictionary<string, List<Action<object>>> _eventHandlers = new();
        
        public void Subscribe(string eventName, Action<object> handler)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = new List<Action<object>>();
            }
            
            _eventHandlers[eventName].Add(handler);
        }
        
        public void Unsubscribe(string eventName, Action<object> handler)
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName].Remove(handler);
            }
        }
        
        public void Publish(string eventName, object data = null)
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                foreach (var handler in _eventHandlers[eventName].ToArray())
                {
                    try
                    {
                        handler?.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        DebugWindow.LogError($"Event handler error for {eventName}: {ex}");
                    }
                }
            }
        }
        
        public void SubscribeToItemFound(Action<Item.HeistRewardInfo> handler)
        {
            Subscribe("ItemFound", data => handler?.Invoke(data as Item.HeistRewardInfo));
        }
        
        public void SubscribeToFilterChanged(Action<List<Config.FilterRule>> handler)
        {
            Subscribe("FilterChanged", data => handler?.Invoke(data as List<Config.FilterRule>));
        }
        
        public void SubscribeToBufferUpdated(Action<Data.BufferManager> handler)
        {
            Subscribe("BufferUpdated", data => handler?.Invoke(data as Data.BufferManager));
        }
        
        public void PublishItemFound(Item.HeistRewardInfo item)
        {
            Publish("ItemFound", item);
        }
        
        public void PublishFilterChanged(List<Config.FilterRule> rules)
        {
            Publish("FilterChanged", rules);
        }
        
        public void PublishBufferUpdated(Data.BufferManager bufferManager)
        {
            Publish("BufferUpdated", bufferManager);
        }
    }
}