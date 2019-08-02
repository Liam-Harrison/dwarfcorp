using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;

namespace DwarfCorp.Tutorial
{
    public class TutorialManager
    {
        public class TutorialEntry
        {
            public String Title;
            public String Text;
            public bool Shown;
            public String GuiHilite;
            public bool Popup = false;
            public String Name;
            public TileReference Icon;
            public String NextTutorial;
        }

        private Dictionary<String, TutorialEntry> Entries;
        public bool TutorialEnabled = true;
        private String PendingTutorial = null;
        private Widget ExistingTutorial = null;
        private bool TutorialVisible = false;
        private Widget HighlightWidget = null;
        public bool TutorialHidden = false;

        public TutorialManager()
        {
            var entries = FileUtils.LoadJsonListFromMultipleSources<JsonTutorialEntry>(ContentPaths.tutorials, null, t => t.Name);

            Entries = new Dictionary<string, TutorialEntry>();
            foreach (var entry in entries)
                Entries.Add(entry.Name, new TutorialEntry
                {
                    Text = entry.Text,
                    Shown = false,
                    Title = entry.Title,
                    GuiHilite = entry.GuiHilite,
                    Popup = entry.Popup,
                    NextTutorial = entry.NextTutorial
                });
        }

        public void AddTutorial(string name, string text, TileReference icon = null)
        {
            Entries[name] = new TutorialEntry()
            {
                Text = text,
                Title = name,
                Shown = false,
                Popup = false,
                Icon = icon
            };
        }

        public Dictionary<String, TutorialEntry> EnumerateTutorials()
        {
            return Entries;
        }

        public void ResetTutorials()
        {
            foreach (var entry in Entries)
                entry.Value.Shown = false;
        }

        public void ShowTutorial(String Name)
        {
            if (TutorialEnabled && !TutorialVisible && Entries.ContainsKey(Name) && !Entries[Name].Shown)
                PendingTutorial = Name;
        }

        public void HideTutorial()
        {
            TutorialHidden = true;
            PendingTutorial = null;
            if (TutorialVisible && ExistingTutorial != null)
            {
                ExistingTutorial.Hidden = true;
            }
        }

        public void ShowTutorial()
        {
            TutorialHidden = false;
            if (TutorialVisible && ExistingTutorial != null)
            {
                ExistingTutorial.Hidden = false;
            }
        }

        public void Update(Gui.Root Gui)
        {
            if (!TutorialHidden && TutorialEnabled && !String.IsNullOrEmpty(PendingTutorial) && Gui != null &&!Entries[PendingTutorial].Shown)
            {
                if (TutorialVisible && ExistingTutorial != null)
                {
                    ExistingTutorial.Close();
                    ExistingTutorial = null;
                }

                var entry = Entries[PendingTutorial];
                entry.Shown = true;
                entry.Name = PendingTutorial;
                TutorialVisible = true;

                var popup = Gui.ConstructWidget(new Gui.Widgets.TutorialPopup
                {
                    Message = entry,
                    OnClose = (sender) =>
                    {
                        TutorialEnabled = !(sender as Gui.Widgets.TutorialPopup).DisableChecked;
                        TutorialVisible = false;
                        Gui.ClearSpecials();
                        if (!String.IsNullOrEmpty(entry.NextTutorial))
                        {
                            ShowTutorial(entry.NextTutorial);
                        }
                    },
                    OnLayout = (sender) =>
                    {
                        sender.Rect.X = Gui.RenderData.VirtualScreen.Width - sender.Rect.Width;
                        sender.Rect.Y = 64;
                    }
                });

                if (entry.Popup)
                {
                    Gui.ShowMinorPopup(popup);
                    popup.PopupDestructionType = PopupDestructionType.Keep;
                }
                else
                    Gui.RootItem.AddChild(popup);
                ExistingTutorial = popup;
                PendingTutorial = null;

                Gui.SpecialHiliteWidgetName = entry.GuiHilite;
            }

            if ((HighlightWidget != null) && HighlightWidget.IsAnyParentHidden() && TutorialVisible && (ExistingTutorial != null))
            {
                Gui.ClearSpecials();
                ExistingTutorial.Close();
            }
        }

        public TutorialSaveData GetSaveData()
        {
            var r = new TutorialSaveData();
            r.TutorialEnabled = this.TutorialEnabled;
            r.EntryShown = new Dictionary<string, bool>();
            foreach (var entry in Entries)
                r.EntryShown.Add(entry.Key, entry.Value.Shown);
            return r;
        }

        public void SetFromSaveData(TutorialSaveData Data)
        {
            this.TutorialEnabled = Data.TutorialEnabled;
            foreach (var entry in Data.EntryShown)
                if (Entries.ContainsKey(entry.Key)) Entries[entry.Key].Shown = entry.Value;
        }
    }
}
