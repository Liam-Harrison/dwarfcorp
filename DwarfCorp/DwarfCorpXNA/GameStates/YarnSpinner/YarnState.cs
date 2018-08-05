using System.Collections.Generic;
using System.Linq;
using DwarfCorp.Gui;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using System;

namespace DwarfCorp
{
    public class YarnState : GameState, IYarnPlayerInterface
    {
        private YarnEngine YarnEngine;
        private Gui.Root GuiRoot;
        private Gui.Widgets.TextBox _Output;
        private Widget ChoicePanel;
        private AnimationPlayer SpeakerAnimationPlayer;
        private Animation SpeakerAnimation;
        private bool SpeakerVisible = false;
        private Gui.Mesh SpeakerRectangle = null;
        private SpeechSynthesizer Language;
        private IEnumerator<String> CurrentSpeach;
        public bool SkipNextLine = false;

        public YarnState(
            String ConversationFile,
            String StartNode,
            Yarn.MemoryVariableStore Memory) :
            base(Game, "YarnState", GameState.Game.StateManager)
        {
            YarnEngine = new YarnEngine(ConversationFile, StartNode, Memory, this);
        }

        public void Output(String S)
        {
            if (_Output != null)
                _Output.AppendText(S);
        }

        public void Speak(String S)
        {
            SpeakerAnimationPlayer?.Play();

            if (Language != null)
            {
                CurrentSpeach = Language.Say(S).GetEnumerator();
                YarnEngine.EnterSpeakState();
            }
            else
            {
                _Output?.AppendText(S);
            }
        }

        public bool AdvanceSpeech(DwarfTime gameTime)
        {
            if (!SkipNextLine)
            {
                if (CurrentSpeach.MoveNext())
                {
                    SpeakerAnimationPlayer?.Update(gameTime, false, Timer.TimerMode.Real);
                    Output(CurrentSpeach.Current);
                    return true;
                }
                else
                {
                    SpeakerAnimationPlayer?.Stop();
                    return false;
                }
            }
            else
            {
                Language.IsSkipping = true;
                while (CurrentSpeach.MoveNext())
                    Output(CurrentSpeach.Current);
                SpeakerAnimationPlayer?.Stop();
                SkipNextLine = false;
                return false;
            }
        }

        public void ClearOutput()
        {
            _Output?.ClearText();
        }

        public void SetLanguage(Language Language)
        {
            // Todo: Remove the reference to Language entirely
            this.Language = new SpeechSynthesizer(Language);
        }

        public void SetPortrait(String Gfx, int FrameWidth, int FrameHeight, float Speed, List<int> Frames)
        {
            SpeakerAnimation = AnimationLibrary.CreateAnimation(new Animation.SimpleDescriptor
            {
                AssetName = Gfx,
                Speed = 1.0f/Speed,
                Frames = Frames,
                Width = FrameWidth,
                Height = FrameHeight
            });

            SpeakerAnimation.Loops = true;

            SpeakerAnimationPlayer = new AnimationPlayer(SpeakerAnimation);
            SpeakerAnimationPlayer.Play();
        }

        public void ShowPortrait()
        {
            SpeakerVisible = true;
        }

        public void HidePortrait()
        {
            SpeakerVisible = false;
        }

        public void EndConversation()
        {
            StateManager.PopState();
        }

        public override void OnEnter()
        {
            DwarfGame.GumInputMapper.GetInputQueue();

            if (GuiRoot == null)
            {
                GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
                GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
                GuiRoot.RootItem.Font = "font8";

                int w = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Width - 256, 550);
                int h = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Height - 256, 300);
                int x = GuiRoot.RenderData.VirtualScreen.Width / 2 - w / 2;
                int y = System.Math.Max(GuiRoot.RenderData.VirtualScreen.Height / 2 - h / 2, 280);

                _Output = GuiRoot.RootItem.AddChild(new Gui.Widgets.TextBox
                {
                    Border = "speech-bubble-reverse",
                    Rect = new Rectangle(x, y - 260, w, 260),
                    TextSize = 1,
                    Font = "font10"
                }) as Gui.Widgets.TextBox;
                SpeakerRectangle = Gui.Mesh.Quad().Scale(256, 256).Translate(x - w/2, y - 260);
                ChoicePanel = GuiRoot.RootItem.AddChild(new Widget
                {
                    Rect = new Rectangle(x, y, w, h),
                    Border = null,
                    TextSize = 1,
                    Font = "font16"
                });
                int inset = 32;
                var border = GuiRoot.RootItem.AddChild(new Widget
                {
                    Border = "border-dark",
                    Rect = new Rectangle(x - w / 2 + inset/2, y - 260 + inset, 256 - inset, 256 - inset)
                });
            }

            IsInitialized = true;
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            SoundManager.Update(gameTime, null, null);

            SkipNextLine = false;
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (@event.Message == InputEvents.KeyUp || @event.Message == InputEvents.MouseClick)
                {
                    SkipNextLine = true;
                }
            }

            GuiRoot.Update(gameTime.ToRealTime());

            YarnEngine.Update(gameTime);
        }
    
        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();

            if (SpeakerVisible && SpeakerAnimationPlayer != null)
            {
                var sheet = SpeakerAnimationPlayer.GetCurrentAnimation().SpriteSheet;
                var frame = SpeakerAnimationPlayer.GetCurrentAnimation().Frames[SpeakerAnimationPlayer.CurrentFrame];
                SpeakerRectangle.ResetQuadTexture();
                SpeakerRectangle.Texture(sheet.TileMatrix(frame.X, frame.Y));
                GuiRoot.DrawMesh(SpeakerRectangle, sheet.GetTexture());
            }

            base.Render(gameTime);
        }

        public void ClearChoices()
        {
            ChoicePanel.Clear();
        }

        public void AddChoice(String Option, Action Callback)
        {

            ChoicePanel.AddChild(new Gui.Widgets.Button()
            {
                Text = Option,
                MinimumSize = new Point(0, 20),
                AutoLayout = AutoLayout.DockTop,
                ChangeColorOnHover = true,
                WrapText = true,
                OnClick = (sender, args) =>
                {
                    Output("> " + sender.Text + "\n");
                    ChoicePanel.Clear();
                    ChoicePanel.Invalidate();

                    Callback();
                }
            });
        }

        public void DoneAddingChoices()
        {
            ChoicePanel.Layout();
        }
    }
}
