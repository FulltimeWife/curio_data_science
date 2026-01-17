using System;
using ExileCore;
using CurioDataScience.Core;

namespace CurioDataScience
{
    public class Curio_Data_Science : BaseSettingsPlugin<CurioDataScienceSettings>
    {
        private PluginCore _pluginCore;
        
        public override bool Initialise()
        {
            _pluginCore = new PluginCore(GameController);
            LogMessage("Heist Data Science initialized with modular architecture");
            LogMessage("Filter system active - showing only filtered items");
            return true;
        }
        
        public override void Render()
        {
            base.Render();
            _pluginCore.Update();
            _pluginCore.Render(Graphics);
        }
        
        private void LogMessage(string message) => 
            DebugWindow.LogMsg($"[Heist Data Science] {message}", 1);
    }
}