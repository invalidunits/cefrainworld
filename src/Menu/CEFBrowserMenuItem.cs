using UnityEngine;
using Menu;
using System;

namespace CEFRainworld
{
    class CEFBrowserMenuItem : RectangularMenuObject, SelectableMenuObject, IDisposable
    {
        private FSprite backdrop;        
        private CEFRainworldBrowserView view;
        public string URL
        {
            get => view.URL;
            set => view.URL = value;
        }

        public bool Transparent = false;

        public CEFBrowserMenuItem(string URL, Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            lastSize = size;
            base.page.selectables.Add(this);
            view = new CEFRainworldBrowserView(pos.x, pos.y, Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), URL);
            backdrop = new FSprite(Futile.whiteElement, true);
            
            
            Container.AddChild(backdrop);
            Container.AddChild(view);
            GrafUpdate(0);
        }

        bool wasSelected = false;
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            view.SetPosition(DrawPos(timeStacker));
            view.WindowWidth = Mathf.RoundToInt(DrawSize(timeStacker).x);
            view.WindowHeight = Mathf.RoundToInt(DrawSize(timeStacker).y);

            bool selected = menu.selectedObject == this;
            if (wasSelected != selected)
            {
                wasSelected = selected;
                view.Browser.GetHost().SetFocus(selected);
            }

            backdrop.SetPosition(view.GetPosition());
            backdrop.width = view.width;
            backdrop.height = view.height;

            backdrop.isVisible = !Transparent;
        }

        public override void RemoveSprites()
        {
            Container.RemoveChild(view);
        }

        public void Dispose() => this.view.Dispose();

        public bool IsMouseOverMe => base.MouseOver;
        public bool CurrentlySelectableMouse => true;
        public bool CurrentlySelectableNonMouse => false;
    }
}