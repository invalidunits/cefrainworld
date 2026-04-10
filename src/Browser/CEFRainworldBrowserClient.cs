using System;
using UnityEngine.UI;
using Xilium.CefGlue;

namespace CEFRainworld
{
    public class CEFRainworldClient : CefClient
    {
        
        private Lazy<CEFRainworldLifeSpanHandler> _LazyLifeSpanHandler;
        public CEFRainworldLifeSpanHandler LifeSpanHandler => _LazyLifeSpanHandler.Value;
        private Lazy<CEFRainworldRenderHandler> _LazyRenderHandler;
        public CEFRainworldRenderHandler RenderHandler => _LazyRenderHandler.Value;
        private Lazy<CEFRainworldDisplayHandler> _LazyDisplayHandler;
        private CEFRainworldDisplayHandler DisplayHandler => _LazyDisplayHandler.Value;

        public CEFRainworldClient()
        {
            _LazyLifeSpanHandler = new (System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
            _LazyRenderHandler = new (System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
            _LazyDisplayHandler = new (System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        }

        protected override CefRenderHandler GetRenderHandler()
        {
            return RenderHandler;
        }
        
        protected override CefLifeSpanHandler GetLifeSpanHandler()
        {
            return LifeSpanHandler;
        }

        protected override CefDisplayHandler GetDisplayHandler()
        {
            return DisplayHandler;
        }
    }
}