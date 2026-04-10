using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xilium.CefGlue;

namespace CEFRainworld
{
    public class CEFRainworldLifeSpanHandler : CefLifeSpanHandler
    {
        public CEFRainworldLifeSpanHandler()
        {
            CEFRainworldPlugin.OnClose += OnClose;
        }
        
        public ConcurrentDictionary<int, CefBrowser> browsers = new();
        public event Action<CEFRainworldLifeSpanHandler, int> OnBrowserClose = delegate { };
        protected override void OnAfterCreated(CefBrowser browser)
        {
            if (closed)
            {
                CEFRainworldPlugin.Log.LogWarning($"browser {browser.Identifier} created after application quit");
            }
            browsers.TryAdd(browser.Identifier, browser);
            base.OnAfterCreated(browser);
        }

        protected override void OnBeforeClose(CefBrowser browser)
        {
            CEFRainworldPlugin.Log.LogDebug("Browser Closing!");
            browsers.TryRemove(browser.Identifier, out _);
            OnBrowserClose(this, browser.Identifier);
        }

        protected override bool OnBeforePopup(CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, CefWindowOpenDisposition targetDisposition, bool userGesture, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings, ref CefDictionaryValue extraInfo, ref bool noJavascriptAccess)
        {
            return true;
        }


        public bool closed;
        private void OnClose()
        {
            SpinWait.SpinUntil(() => {
                CefRuntime.DoMessageLoopWork();
                CEFRainworldPlugin.Log.LogDebug("OnClose: Spinning!");
                foreach (var item in browsers)
                {
                    item.Value.GetHost().TryCloseBrowser();
                }

                return !browsers.Any();        
            }, TimeSpan.FromSeconds(10));
        }
    }
}