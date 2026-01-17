using System;
using ExileCore;
using ExileCore.Shared.NativeInput;
using CurioDataScience.Core;

namespace CurioDataScience
{
    public class Curio_Data_Science : BaseSettingsPlugin<CurioDataScienceSettings>
    {
        private PluginCore _pluginCore;
        
        public override bool Initialise()
        {
            _pluginCore = new PluginCore(GameController, Graphics);
            DebugWindow.LogMsg("[Heist Data Science] initialized with modular architecture", 5);
            DebugWindow.LogMsg("[Heist Data Science] Filter system active - Press F2 to configure filters", 5);
            DebugWindow.LogMsg("[Heist Data Science] Press F1 for debug window, F3 to force export, F4 to clear buffer", 5);
            return true;
        }
        
        public override void Render()
        {
            base.Render();
            _pluginCore.Update();
            _pluginCore.Render();
        }
        
        public override bool KeyDown(VKeys key)
        {
            _pluginCore.OnKeyDown(key);
            return base.KeyDown(key);
        }
    }
}