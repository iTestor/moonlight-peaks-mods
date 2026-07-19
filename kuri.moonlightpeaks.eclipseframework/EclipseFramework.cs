using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EclipseFramework
{
    public class EF
    {
        private BepInEx.PluginInfo _pluginInfo;

        public EF(BepInEx.PluginInfo pluginInfo)
        {
            _pluginInfo = pluginInfo;
        }

        public MailSystemManager MailSystem => new MailSystemManager(_pluginInfo);
    }
}
