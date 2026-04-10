using UnityEngine;
using Menu;
using RWCustom;

namespace CEFRainworld
{
    class CEFBrowseMenu : Menu.Menu
    {
        private CEFBrowserMenuItem browser;
        Page mainPage;
        public CEFBrowseMenu(ProcessManager manager) : base(manager, CEFRainworldPlugin.Ext_ProcessID.CEFBrowseMenu)
        {
            pages.Add(mainPage = new Page(this, null, "main", 0));
            // Leave a small margin from the screen edges so the browser isn't flush to the bounds.
            float margin = 40f;
            Vector2 browserSize = new Vector2(Mathf.Max(Futile.screen.pixelWidth - margin * 2f, 0f), Mathf.Max(Futile.screen.pixelHeight - margin * 2f, 0f));
            Vector2 browserPos = new Vector2(Futile.screen.pixelWidth / 2f, Futile.screen.pixelHeight / 2f);
            mainPage.subObjects.Add(browser = new CEFBrowserMenuItem("http://google.com", this, this.mainPage,
                browserPos, browserSize));

            // Add a simple "Back to Main Menu" button in the top-left corner
            Vector2 backPos = new Vector2(5f, 5f);
            Vector2 backSize = new Vector2(160f, 30f);
            mainPage.subObjects.Add(new SimpleButton(this, this.mainPage, "Back to Main Menu", "BackToMain", backPos, backSize));
        }

        public override void Singal(MenuObject sender, string message)
        {
            switch (message)
            {
                case "BackToMain":
                    Custom.rainWorld.processManager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                    break;
            }
        }


        public override void ShutDownProcess()
        {
            this.browser.Dispose();
            base.ShutDownProcess();
        }
    }
}