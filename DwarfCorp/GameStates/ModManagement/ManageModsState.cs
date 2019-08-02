using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using System.Linq;
using System;
using DwarfCorp.AssetManagement.Steam;

namespace DwarfCorp.GameStates.ModManagement
{
    // Todo: Dump gui stuff to main screen so steam popups can play over any gamestate.


    /// <summary>
    /// This game state allows the player to design their own dwarf company.
    /// </summary>
    public class ManageModsState : GameState
    {
        private Gui.Root GuiRoot;

        private bool HasChanges = false;
        public Action OnSystemChanges = null;

        public void MadeSystemChanges()
        {
            HasChanges = true;
            OnSystemChanges?.Invoke();
        }

        public ManageModsState(DwarfGame game) :
    base(game)
        {
        }

        public override void OnEnter()
        {
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);

            var screen = GuiRoot.RenderData.VirtualScreen;
            float scale = 0.9f;
            float newWidth = global::System.Math.Min(global::System.Math.Max(screen.Width * scale, 640), screen.Width * scale);
            float newHeight = global::System.Math.Min(global::System.Math.Max(screen.Height * scale, 480), screen.Height * scale);
            Rectangle rect = new Rectangle((int)(screen.Width / 2 - newWidth / 2), (int)(screen.Height / 2 - newHeight / 2), (int)newWidth, (int)newHeight);

            var main = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = rect
            });

            var bottom = main.AddChild(new Widget
            {
                Transparent = true,
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockBottom,
                Padding = new Margin(2, 2, 2, 2)
            });

            bottom.AddChild(new Gui.Widgets.Button
            {
                Text = "Close",
                Font = "font16",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    // If changes, prompt before closing.
                    if (HasChanges)
                    {
                        var confirm = new Popup
                        {
                            Text = "Dwarf Corp must be restarted for changes to take effect.",
                            OkayText = "Okay",
                            OnClose = (s2) => GameStateManager.PopState()
                        };
                        GuiRoot.ShowModalPopup(confirm);
                    }
                    else
                        GameStateManager.PopState();
                },
                AutoLayout = AutoLayout.DockRight
            });

            var tabs = main.AddChild(new Gui.Widgets.TabPanel
            {
                AutoLayout = AutoLayout.DockFill,
                TextSize = 1,
                SelectedTabColor = new Vector4(1, 0, 0, 1)
            }) as Gui.Widgets.TabPanel;

            tabs.AddTab("Installed", new InstalledModsWidget
            {
                OwnerState = this
            });

            tabs.AddTab("Search", new SearchWidget
            {
                Owner = this
            });

            GuiRoot.RootItem.Layout();

            IsInitialized = true;

            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }

            GuiRoot.Update(gameTime.ToRealTime());
            base.Update(gameTime);
        }
        
        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}