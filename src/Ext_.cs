using Menu;
using System.Collections.Generic;
namespace CEFRainworld
{
    public partial class CEFRainworldPlugin
    {
        public class Ext_ProcessID
        {
            public static ProcessManager.ProcessID CEFAppManager = new("CEFAppManager", true);
            public static ProcessManager.ProcessID CEFBrowseMenu = new("CEFBrowseMenu", true);
        }

    }
}
