using System.Collections.Generic;
using System.Linq;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using System;

namespace DwarfCorp.GameStates
{
    public class OptionsState : GameState
    {
        private Gui.Root GuiRoot;
        private bool HasChanges = false;
        public Action OnClosed = null;
        private bool BuildingGUI = false;

        private Dictionary<string, int> AntialiasingOptions;
        private Dictionary<string, DisplayMode> DisplayModes;
        
        private Gui.Widget MainPanel;
        private Gui.Widgets.TabPanel TabPanel;

        private HorizontalFloatSlider MoveSpeed;
        private HorizontalFloatSlider ZoomSpeed;
        private CheckBox InvertZoom;
        private CheckBox ZoomTowardMouse;
        private CheckBox EdgeScrolling;
        private CheckBox FollowSurface;
        //private CheckBox FogOfWar;
        private CheckBox AutoFarming;
        private CheckBox AutoDigging;
        private CheckBox PlayIntro;
        private CheckBox AllowReporting;
        private ComboBox GuiScale;
        private HorizontalFloatSlider MasterVolume;
        private HorizontalFloatSlider SFXVolume;
        private HorizontalFloatSlider MusicVolume;
        private Gui.Widgets.ComboBox Resolution;
        private CheckBox Fullscreen;
        private HorizontalFloatSlider ChunkDrawDistance;
        private HorizontalFloatSlider EntityUpdateDistance;
        //private HorizontalFloatSlider VertexCullDistance;
        private CheckBox Glow;
        private Gui.Widgets.ComboBox Antialiasing;
        private CheckBox ReflectTerrain;
        private CheckBox ReflectEntities;
        private CheckBox AmbientOcclusion;
        //private CheckBox Ramps;
        private CheckBox CursorLight;
        private CheckBox VSync;
        private CheckBox EntityLight;
        private CheckBox SelfIllumination;
        private CheckBox ParticlePhysics;
        private CheckBox Motes;
        //private CheckBox LightMap;
        //private CheckBox DynamicShadows;
        private CheckBox GuiAutoScale;
        private Gui.Widgets.ComboBox EasyGraphicsSetting;
        private CheckBox Autosave;
        private Widget AutoSaveFrequency;
        private ComboBox MaxSaves;

        public WorldManager World = null;
        private CheckBox EnableTutorial;
        private CheckBox DisableTutorialForAllGames;
        private EditableTextField SaveLocation;

        public OptionsState(DwarfGame Game) :
            base(Game)
        { }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            // Setup antialiasing options.
            AntialiasingOptions = new Dictionary<string, int>
            {
                {"NONE", 0},
                {"FXAA", -1},
                {"2x MSAA", 2},
                {"4x MSAA", 4},
                {"16x MSAA", 16}
            };

            DisplayModes = new Dictionary<string, DisplayMode>();
            foreach (var displayMode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes.Where(dm =>
                dm.Format == SurfaceFormat.Color && dm.Width >= 801))
                DisplayModes.Add(string.Format("{0} x {1}", displayMode.Width, displayMode.Height), displayMode);

            RebuildGui();

            // Must be true or Render will not be called.
            IsInitialized = true;

            base.OnEnter();
        }

        private Widget CloseButton = null;

        private void RebuildGui()
        {
            BuildingGUI = true;

            // Create and initialize GUI framework.
            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.RenderData.CalculateScreenSize();
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            var screen = GuiRoot.RenderData.VirtualScreen;
            float scale = 0.95f;
            float newWidth = global::System.Math.Min(global::System.Math.Max(screen.Width * scale, 640), screen.Width* scale);
            float newHeight = global::System.Math.Min(global::System.Math.Max(screen.Height * scale, 480), screen.Height* scale);
            Rectangle rect = new Rectangle((int)(screen.Width / 2 - newWidth / 2), (int)(screen.Height/2 - newHeight/2),(int)newWidth, (int)newHeight);
            // CONSTRUCT GUI HERE...
            MainPanel = GuiRoot.RootItem.AddChild(new Gui.Widget
                {
                    Rect = rect,
                    Padding = new Margin(4,4,4,4),
                    MinimumSize = new Point(640, 480),
                    Font = "font10",
                    Background = new TileReference("basic", 0),
                    BackgroundColor = new Vector4(0, 0, 0, 0.5f),
            });
            var topbar = MainPanel.AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 36)
            });

            CloseButton = topbar.AddChild(new Gui.Widgets.Button
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
                        var confirm = new Gui.Widgets.Confirm
                        {
                            Text = "@options-apply-check",
                            OkayText = Library.GetString("yes"),
                            CancelText = Library.GetString("no"),
                            OnClose = (s2) =>
                                {
                                    if ((s2 as Gui.Widgets.Confirm).DialogResult == Gui.Widgets.Confirm.Result.OKAY)
                                        ConfirmSettings();
                                    if (OnClosed != null) OnClosed();
                                    GameStateManager.PopState();
                                }
                        };
                        GuiRoot.ShowModalPopup(confirm);
                    }
                    else
                    {
                        OnClosed?.Invoke();
                        GameStateManager.PopState();
                    }
                },
                AutoLayout = AutoLayout.FloatTopLeft
            });

            topbar.AddChild(new Gui.Widgets.Button
            {
                Text = "@options-apply",
                Font = "font16",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    ConfirmSettings();
                },
                AutoLayout = AutoLayout.FloatTopRight
            });

            TabPanel = MainPanel.AddChild(new Gui.Widgets.TabPanel
            {
                AutoLayout = AutoLayout.DockFill,
                TextSize = 1,
                SelectedTabColor = new Vector4(1,0,0,1),
                OnLayout = (sender) => sender.Rect.Height -= 36 // Keep it from overlapping bottom buttons.
            }) as Gui.Widgets.TabPanel;

            CreateGameplayTab();
            CreateAudioTab();
            CreateKeysTab();
            CreateGraphicsTab();

            TabPanel.SelectedTab = 0;

            GuiRoot.RootItem.Layout();

            LoadSettings();

            BuildingGUI = false;
        }

        private Widget LabelAndDockWidget(string Label, Widget Widget)
        {
            var r = GuiRoot.ConstructWidget(new Widget
            {
                MinimumSize = new Point(0, 20),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(0,0,4,4),
                ChangeColorOnHover = true,
                HoverTextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4()
            });

            var label = new Widget
            {
                Text = Label,
                AutoLayout = AutoLayout.DockLeft,
            };

            r.AddChild(label);

            Widget.AutoLayout = AutoLayout.DockFill;
            r.AddChild(Widget);
            Widget.OnMouseEnter += (sender, args) =>
            {
                label.TextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
                label.Invalidate();
            };

            Widget.OnMouseLeave += (sender, args) =>
            {
                label.TextColor = Color.Black.ToVector4();
                label.Invalidate();
            };
            return r;
        }

        private void CreateGameplayTab()
        {
            var panel = TabPanel.AddTab("@options-gameplay-tab", new Widget
            {
                Border = "border-thin",
                Padding = new Margin(4, 4, 0, 0)
            });

            panel.AddChild(new Widget
            {
                Text = "@options-restore-defaults",
                MinimumSize = new Point(0, 20),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(0, 0, 4, 4),
                Border = "border-button",
                ChangeColorOnHover = true,
                OnClick = (sender, args) =>
                {
                    var prompt = GuiRoot.ConstructWidget(new Gui.Widgets.Confirm
                    {
                        Text = "@options-restore-defaults-check",
                        OnClose = (confirm) =>
                        {
                            if ((confirm as Gui.Widgets.Confirm).DialogResult == Gui.Widgets.Confirm.Result.OKAY)
                            {
                                GameSettings.Default = new GameSettings.Settings();
                                RebuildGui();
                                ConfirmSettings();
                            }
                        }
                    });

                    GuiRoot.ShowDialog(prompt);
                }
            });

            // Todo: Display actual value beside slider.
            MoveSpeed = panel.AddChild(LabelAndDockWidget(
                "@options-camera-movement", 
                new HorizontalFloatSlider
            {
                ScrollArea = 20,
                OnSliderChanged = OnItemChanged,
                Tooltip = "@options-camera-movement-tooltip"
            })).GetChild(1) as HorizontalFloatSlider;

            ZoomSpeed = panel.AddChild(LabelAndDockWidget("@options-camera-zoom",
                new HorizontalFloatSlider
            {
                ScrollArea = 2,
                OnSliderChanged = OnItemChanged,
                Tooltip = "@options-camera-zoom-tooltip"
            })).GetChild(1) as HorizontalFloatSlider;

            var split = panel.AddChild(new Gui.Widgets.Columns
            {
                AutoLayout = AutoLayout.DockFill
            }) as Gui.Widgets.Columns;

            var leftPanel = split.AddChild(new Widget
            {
                Padding = new Margin(2, 2, 2, 2)
            });

            var rightPanel = split.AddChild(new Widget
            {
                Padding = new Margin(2, 2, 2, 2)
            });

            InvertZoom = leftPanel.AddChild(new CheckBox
            {
                Text = "@options-invert-zoom",
                Tooltip = "@options-invert-zoom-tooltip",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            ZoomTowardMouse = leftPanel.AddChild(new CheckBox
            {
                Text = "@options-zoom-to-mouse",
                Tooltip = "@options-zoom-to-mouse-tooltip",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;


            EdgeScrolling = leftPanel.AddChild(new CheckBox
            {
                Text = "@options-edge-scrolling",
                Tooltip = "@options-edge-scrolling-tooltip",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            FollowSurface = leftPanel.AddChild(new CheckBox
            {
                Text = "Camera Follows Surface Height",
                Tooltip = "When checked, the camera will follow the ground surface height when moving.",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            PlayIntro = rightPanel.AddChild(new CheckBox
            {
                Text = "Play Intro",
                Tooltip = "When checked, the intro animation will play at the beginning of the game.",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            AllowReporting = rightPanel.AddChild(new CheckBox
            {
                Text = "Crash Reporting",
                Tooltip = "When checked, you have been opted in to automatic crash reporting.",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            AutoDigging = rightPanel.AddChild(new CheckBox
            {
                Text = "Auto-digging",
                Tooltip = "When checked, dwarfs will automatically dig to get out of tricky situations.",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            AutoFarming = rightPanel.AddChild(new CheckBox
            {
                Text = "Auto-farming",
                Tooltip = "When checked, dwarfs will automatically harvest plants in farms and plant new seeds.",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            var guiScaleItems = new List<String>();
            for (int i = 1; i < 10; ++i)
                if (i * 480 <= GameSettings.Default.ResolutionY)
                    guiScaleItems.Add(i.ToString());

            GuiScale = rightPanel.AddChild(LabelAndDockWidget("Gui Scale", new ComboBox
            {
                Items = guiScaleItems
            })).GetChild(1) as ComboBox;

            GuiAutoScale = rightPanel.AddChild(new CheckBox
            {
                Text = "Autoscale GUI",
                Tooltip = "When checked, the GUI will get scaled up on high resolution screens",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            DisableTutorialForAllGames = leftPanel.AddChild(new CheckBox
            {
                Text = "Disable the tutorial for all new games",
                Tooltip = "When checked, the tutorial will not open in new games.",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            if (World != null)
            {
                EnableTutorial = leftPanel.AddChild(new CheckBox
                {
                    Text = "Enable Tutorial",
                    Tooltip = "When checked, tutorial will be displayed.",
                    OnCheckStateChange = OnItemChanged,
                    AutoLayout = AutoLayout.DockTop
                }) as CheckBox;

                leftPanel.AddChild(new Widget
                {
                    Text = "Reset tutorial",
                    MinimumSize = new Point(0, 20),
                    AutoLayout = AutoLayout.DockTop,
                    Padding = new Margin(0, 0, 4, 4),
                    Border = "border-button",
                    ChangeColorOnHover = true,
                    OnClick = (sender, args) => World.TutorialManager.ResetTutorials()
                });
            }

            Autosave = rightPanel.AddChild(new CheckBox
            {
                Text = "Enable Autosave",
                Tooltip = "When checked, game will auto save.",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            AutoSaveFrequency = rightPanel.AddChild(LabelAndDockWidget("Autosave Frequency        ", new HorizontalSlider
            {
                ScrollArea = 115,
                OnSliderChanged = (widget) =>
                {
                    AutoSaveFrequency.GetChild(0).Text = String.Format("Autosave Frequency ({0})",
                        (widget as HorizontalSlider).ScrollPosition + 5);
                    this.OnItemChanged(widget);
                },
                Tooltip = "Minutes between auto saves"
            }));

            MaxSaves = rightPanel.AddChild(LabelAndDockWidget("Max Saves", new Gui.Widgets.ComboBox
            {
                Items = new String[] { "5", "15", "30", "100", "999"}.ToList(),
                OnSelectedIndexChanged = OnItemChanged,
                Border = "border-thin",
                Tooltip = "The maximum number of user saves to keep on disc (doesn't count autosaves)."
            })).GetChild(1) as Gui.Widgets.ComboBox;

            SaveLocation = rightPanel.AddChild(LabelAndDockWidget("Save Location", new EditableTextField
            {
                Tooltip = "Where to look for save games. Leave blank to use default location.",
                Text = "",
                OnTextChange = this.OnItemChanged
            })).GetChild(1) as EditableTextField;

            leftPanel.AddChild(LabelAndDockWidget("Colors", new Button()
            {
                Text = "Edit Colors...",
                OnClick = (sender, args) =>
                {
                    GuiRoot.ShowModalPopup(new ColorOptionsEditor()
                    {
                        Settings = GameSettings.Default,
                        Border = "border-fancy",
                        AutoLayout = AutoLayout.DockFill,
                        MinimumSize = new Point(640, 480),
                        Font = "font10",
                        Rect = new Rectangle(GuiRoot.RenderData.VirtualScreen.Width / 2 - 320, GuiRoot.RenderData.VirtualScreen.Height / 2 - 320, 640, 480)
                    });
                }
            }));
        }

        private void CreateAudioTab()
        {
            var panel = TabPanel.AddTab("AUDIO", new Widget
            {
                Border = "border-thin",
                Padding = new Margin(4, 4, 0, 0)
            });

            if (!SoundManager.HasAudioDevice)
            {
                panel.AddChild(new Widget()
                {
                    Text = String.Format("ERROR. SOUND IS DISABLED: {0}", SoundManager.AudioError),
                    TextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4(),
                    Font = "font16",
                    AutoLayout = AutoLayout.DockTop
                });
            }

            MasterVolume = panel.AddChild(LabelAndDockWidget("Master Volume", new HorizontalFloatSlider
            {
                ScrollArea = 1.0f,
                OnSliderChanged = OnItemChanged,
                Tooltip = "Volume of all sounds in the game.",
            })).GetChild(1) as HorizontalFloatSlider;

            SFXVolume = panel.AddChild(LabelAndDockWidget("SFX Volume", new HorizontalFloatSlider
            {
                ScrollArea = 1.0f,
                OnSliderChanged = OnItemChanged,
                Tooltip = "Volume of sound effects."
            })).GetChild(1) as HorizontalFloatSlider;

            MusicVolume = panel.AddChild(LabelAndDockWidget("Music Volume", new HorizontalFloatSlider
            {
                ScrollArea = 1.0f,
                OnSliderChanged = OnItemChanged,
                Tooltip = "Volume of background music."
            })).GetChild(1) as HorizontalFloatSlider;

            panel.AddChild(new Button()
            {
                Text = "Mixer...",
                AutoLayout = AutoLayout.DockTop,
                MaximumSize = new Point(128, 32),
                OnClick = (sender, args) => panel.Root.ShowModalPopup(new AudioMixerWidget()
                {
                    Border = "border-fancy",
                    Rect = GuiRoot.RenderData.VirtualScreen
                })
            });
        }

        private void CreateKeysTab()
        {
            // Todo: Scroll when list is too long.
            var panel = TabPanel.AddTab("KEYS", new Widget
            {
                Border = "border-thin",
                Padding = new Margin(4, 4, 0, 0)
            });

            panel.AddChild(new Gui.Widgets.KeyEditor
            {
                KeyManager = new KeyManager(),
                AutoLayout = AutoLayout.DockFill
            });
        }

        private void CreateGraphicsTab()
        {
            var panel = TabPanel.AddTab("GRAPHICS", new Widget
            {
                Border = "border-thin",
                Padding = new Margin(4,4,4,4)
            });

            var split = panel.AddChild(new Gui.Widgets.Columns
            {
                AutoLayout = AutoLayout.DockFill
            }) as Gui.Widgets.Columns;

            var leftPanel = split.AddChild(new Widget
            {
                Padding = new Margin(2,2,2,2)
            });

            var rightPanel = split.AddChild(new Widget
            {
                Padding = new Margin(2,2,2,2)
            });

            EasyGraphicsSetting = leftPanel.AddChild(LabelAndDockWidget("Easy Settings", new Gui.Widgets.ComboBox
            {
                Items = new String[] { "Lowest", "Low", "Medium", "High", "Highest", "Custom" }.ToList(),
                OnSelectedIndexChanged = OnEasySettingChanged,
                Border = "border-thin"
            })).GetChild(1) as Gui.Widgets.ComboBox;

            Resolution = leftPanel.AddChild(LabelAndDockWidget("Resolution", new Gui.Widgets.ComboBox
                {
                    Items = DisplayModes.Select(dm => dm.Key).ToList(),
                    OnSelectedIndexChanged = OnItemChanged,
                    Border = "border-thin",
                    Tooltip = "Game screen size",
                })).GetChild(1) as Gui.Widgets.ComboBox;

            Fullscreen = leftPanel.AddChild(new CheckBox
                {
                    Text = "Fullscreen",
                    OnCheckStateChange = OnItemChanged,
                    AutoLayout = AutoLayout.DockTop,
                    Tooltip = "When checked, game will take up the whole screen."
                }) as CheckBox;

            ChunkDrawDistance = leftPanel.AddChild(LabelAndDockWidget("Terrain Draw Distance", new HorizontalFloatSlider
            {
                ScrollArea = 1000f,
                OnSliderChanged = OnItemChanged,
                Tooltip = "Higher values allow you to see more terrain. Lower values will make the game run faster."
            })).GetChild(1) as HorizontalFloatSlider;

            EntityUpdateDistance = leftPanel.AddChild(LabelAndDockWidget("Entity Update Distance", new HorizontalFloatSlider
            {
                ScrollArea = 1000f,
                OnSliderChanged = OnItemChanged,
                Tooltip = "Controls the distance beyond which entities will not be updated."
            })).GetChild(1) as HorizontalFloatSlider;

            /*
            VertexCullDistance = leftPanel.AddChild(LabelAndDockWidget("Geometry Draw Distance",
                new HorizontalFloatSlider
            {
                ScrollArea = 1000f,
                OnSliderChanged = OnItemChanged,
                Tooltip = "Higher values allow you to see more terrain. Lower values will make the game run faster."
            })).GetChild(1) as HorizontalFloatSlider;
            */
            /*
            GenerateDistance = leftPanel.AddChild(LabelAndDockWidget("Generate Distance",
                new HorizontalFloatSlider
                {
                    ScrollArea = 1000f,
                    OnScrollValueChanged = OnItemChanged
                })).GetChild(1) as HorizontalFloatSlider;
             */

            Glow = leftPanel.AddChild(new CheckBox
            {
                Text = "Glow",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, bright parts of the screen will have a glow effect. Turn off to make the game run faster."
            }) as CheckBox;

            Antialiasing = rightPanel.AddChild(LabelAndDockWidget("Antialiasing", new Gui.Widgets.ComboBox
            {
                Items = AntialiasingOptions.Select(o => o.Key).ToList(),
                OnSelectedIndexChanged = OnItemChanged,
                Border = "border-thin",
                Tooltip = "Higher values mean fewer jagged pixels. For best quality use FXAA. Fastest is no antialiasing."
            })).GetChild(1) as Gui.Widgets.ComboBox;

            ReflectTerrain = rightPanel.AddChild(new CheckBox
            {
                Text = "Water Reflects Terrain",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, water will reflect the terrain. Turn off to increase game performance."
            }) as CheckBox;

            ReflectEntities = rightPanel.AddChild(new CheckBox
            {
                Text = "Water Reflects Entities",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, water will reflect entities. Turn off to increase game performance."
            }) as CheckBox;

            AmbientOcclusion = rightPanel.AddChild(new CheckBox
            {
                Text = "Ambient Occlusion",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "Enables smooth lighting effects on terrain. Turn off to increase game performance."
            }) as CheckBox;


            CursorLight = rightPanel.AddChild(new CheckBox
            {
                Text = "Cursor Light",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, the cursor casts light. Turn off to increase game performance."
            }) as CheckBox;

            VSync = rightPanel.AddChild(new CheckBox
            {
                Text = "Vertical Sync",
                Tooltip = "When checked, the framerate will be fixed to the monitor refresh rate.",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            EntityLight = rightPanel.AddChild(new CheckBox
            {
                Text = "Dynamic Entity Lighting",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, creatures underground will be in shadow. Turn off to increase game performance."
            }) as CheckBox;

            SelfIllumination = rightPanel.AddChild(new CheckBox
            {
                Text = "Ore Glow",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked certain ores will glow underground. Turn off to increase game performance."
            }) as CheckBox;

            ParticlePhysics = leftPanel.AddChild(new CheckBox
            {
                Text = "Particle Physics",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, certain particles will bounce off of terrain. Turn off to increase game performance."
            }) as CheckBox;

            Motes = leftPanel.AddChild(new CheckBox
            {
                Text = "Motes",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, detail grass motes will spawn. Turn off to increase game performance."
            }) as CheckBox;
        }

        private void OnItemChanged(Gui.Widget Sender)
        {
            HasChanges = true;
        }

        private void OnEasySettingChanged(Gui.Widget Sender)
        {
            // This handler can be called while building the GUI, resulting in an infinite loop.
            if (BuildingGUI) return;

            GuiRoot.ShowDialog(GuiRoot.ConstructWidget(new Gui.Widgets.Confirm
            {
                Text = "This will automatically apply changes",
                OnClose = (confirm) =>
                {
                    if ((confirm as Gui.Widgets.Confirm).DialogResult == Gui.Widgets.Confirm.Result.OKAY)
                    {
                        var comboBox = Sender as Gui.Widgets.ComboBox;
                        var selection = comboBox.SelectedItem;

                        switch (selection)
                        {
                            case "Custom":
                                break;
                            case "Lowest":
                                GameSettings.Default.AmbientOcclusion = false;
                                GameSettings.Default.AntiAliasing = 0;
                                GameSettings.Default.CalculateRamps = false;
                                GameSettings.Default.CursorLightEnabled = false;
                                GameSettings.Default.DrawChunksReflected = false;
                                GameSettings.Default.DrawEntityReflected = false;
                                GameSettings.Default.DrawSkyReflected = false;
                                GameSettings.Default.EntityLighting = false;
                                GameSettings.Default.EnableGlow = false;
                                GameSettings.Default.SelfIlluminationEnabled = false;
                                GameSettings.Default.NumMotes = 100;
                                GameSettings.Default.GrassMotes = false;
                                GameSettings.Default.ParticlePhysics = false;
                                break;
                            case "Low":
                                GameSettings.Default.AmbientOcclusion = false;
                                GameSettings.Default.AntiAliasing = 0;
                                GameSettings.Default.CalculateRamps = true;
                                GameSettings.Default.CursorLightEnabled = false;
                                GameSettings.Default.DrawChunksReflected = false;
                                GameSettings.Default.DrawEntityReflected = false;
                                GameSettings.Default.DrawSkyReflected = true;
                                GameSettings.Default.EntityLighting = true;
                                GameSettings.Default.EnableGlow = false;
                                GameSettings.Default.SelfIlluminationEnabled = false;
                                GameSettings.Default.NumMotes = 300;
                                GameSettings.Default.GrassMotes = false;
                                GameSettings.Default.ParticlePhysics = false;
                                break;
                            case "Medium":
                                GameSettings.Default.AmbientOcclusion = true;
                                GameSettings.Default.AntiAliasing = 4;
                                GameSettings.Default.CalculateRamps = true;
                                GameSettings.Default.CursorLightEnabled = true;
                                GameSettings.Default.DrawChunksReflected = true;
                                GameSettings.Default.DrawEntityReflected = false;
                                GameSettings.Default.DrawSkyReflected = true;
                                GameSettings.Default.EntityLighting = true;
                                GameSettings.Default.EnableGlow = false;
                                GameSettings.Default.SelfIlluminationEnabled = true;
                                GameSettings.Default.NumMotes = 500;
                                GameSettings.Default.GrassMotes = true;
                                GameSettings.Default.ParticlePhysics = true;
                                break;
                            case "High":
                                GameSettings.Default.AmbientOcclusion = true;
                                GameSettings.Default.AntiAliasing = 16;
                                GameSettings.Default.CalculateRamps = true;
                                GameSettings.Default.CursorLightEnabled = true;
                                GameSettings.Default.DrawChunksReflected = true;
                                GameSettings.Default.DrawEntityReflected = true;
                                GameSettings.Default.DrawSkyReflected = true;
                                GameSettings.Default.EntityLighting = true;
                                GameSettings.Default.EnableGlow = true;
                                GameSettings.Default.SelfIlluminationEnabled = true;
                                GameSettings.Default.NumMotes = 1500;
                                GameSettings.Default.GrassMotes = true;
                                GameSettings.Default.ParticlePhysics = true;
                                break;
                            case "Highest":
                                GameSettings.Default.AmbientOcclusion = true;
                                GameSettings.Default.AntiAliasing = -1;
                                GameSettings.Default.CalculateRamps = true;
                                GameSettings.Default.CursorLightEnabled = true;
                                GameSettings.Default.DrawChunksReflected = true;
                                GameSettings.Default.DrawEntityReflected = false;
                                GameSettings.Default.DrawSkyReflected = true;
                                GameSettings.Default.EntityLighting = true;
                                GameSettings.Default.EnableGlow = true;
                                GameSettings.Default.SelfIlluminationEnabled = true;
                                GameSettings.Default.NumMotes = 2048;
                                GameSettings.Default.GrassMotes = true;
                                GameSettings.Default.ParticlePhysics = true;
                                break;
                        }

                        HasChanges = true;
                        RebuildGui();
                        ConfirmSettings();
                        TabPanel.SelectedTab = 3;
                    }
                }
            }));
        }

        public void SetBestResolution()
        {
            this.Resolution.SelectedIndex = this.Resolution.Items.IndexOf(string.Format("{0} x {1}",
                GameSettings.Default.ResolutionX, GameSettings.Default.ResolutionY));

            if (this.Resolution.SelectedIndex != -1) return;

            string bestMode = null;
            int bestScore = int.MaxValue;
            foreach (var mode in DisplayModes)
            {
                int score = global::System.Math.Abs(mode.Value.Width - GameSettings.Default.ResolutionX) +
                            global::System.Math.Abs(mode.Value.Height - GameSettings.Default.ResolutionY);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestMode = mode.Key;
                }
            }

            this.Resolution.SelectedIndex = this.Resolution.Items.IndexOf(bestMode);
        }

        public class SettingsApplier : Confirm
        {
            public OptionsState State;
            public GameSettings.Settings PreviousSettings;
            public GameSettings.Settings NewSettings;
            private double ElapsedSeconds = 0;
            private int ElapsedSecondsInt = 0;
            public override void Construct()
            {
                base.Construct();
                OnUpdate = (widget, time) =>
                {
                    // Do this instead of using timer in case it gets paused.
                    ElapsedSeconds += 0.0167;
                    int prevElapsed = ElapsedSecondsInt;
                    ElapsedSecondsInt = (int) ElapsedSeconds;
                    if (prevElapsed != ElapsedSecondsInt)
                    {
                        Text =
                            String.Format(
                                "Do you want to keep these settings? They will be reverted after {0} seconds...",
                                10 - ElapsedSecondsInt);
                        Invalidate();
                    }
                    if (ElapsedSeconds > 10)
                    {
                        State.ApplySettings(PreviousSettings.Clone());
                        this.Close();
                    }
                };
            }
        }

        public GameSettings.Settings GetNewSettings()
        {
            GameSettings.Settings toReturn = GameSettings.Default.Clone();
            // Copy all the states from widgets to game settings.
            // Gameplay settings
            toReturn.CameraScrollSpeed = this.MoveSpeed.ScrollPosition;
            toReturn.CameraZoomSpeed = this.ZoomSpeed.ScrollPosition;
            toReturn.EnableEdgeScroll = this.EdgeScrolling.CheckState;
            toReturn.CameraFollowSurface = this.FollowSurface.CheckState;
            //toReturn.FogofWar = this.FogOfWar.CheckState;
            toReturn.AllowAutoDigging = this.AutoDigging.CheckState;
            toReturn.AllowAutoFarming = this.AutoFarming.CheckState;
            toReturn.InvertZoom = this.InvertZoom.CheckState;
            toReturn.ZoomCameraTowardMouse = this.ZoomTowardMouse.CheckState;
            toReturn.DisplayIntro = this.PlayIntro.CheckState;
            toReturn.AllowReporting = this.AllowReporting.CheckState;
            toReturn.MaxSaves = int.Parse(this.MaxSaves.SelectedItem);
            toReturn.AutoSave = this.Autosave.CheckState;
            toReturn.AutoSaveTimeMinutes =
                (this.AutoSaveFrequency.GetChild(1) as HorizontalSlider).ScrollPosition + 5.0f;
            toReturn.SaveLocation = this.SaveLocation.Text;

            // Audio settings
            toReturn.MasterVolume = this.MasterVolume.ScrollPosition;
            toReturn.SoundEffectVolume = this.SFXVolume.ScrollPosition;
            toReturn.MusicVolume = this.MusicVolume.ScrollPosition;

            var newDisplayMode = DisplayModes[this.Resolution.SelectedItem];
            toReturn.ResolutionX = newDisplayMode.Width;
            toReturn.ResolutionY = newDisplayMode.Height;

            toReturn.Fullscreen = this.Fullscreen.CheckState;
            toReturn.ChunkDrawDistance = this.ChunkDrawDistance.ScrollPosition + 1.0f;
            toReturn.EntityUpdateDistance = this.EntityUpdateDistance.ScrollPosition + 1.0f;
            //toReturn.VertexCullDistance = this.VertexCullDistance.ScrollPosition + 0.1f;
            //toReturn.ChunkGenerateDistance = this.GenerateDistance.ScrollPosition + 1.0f;
            toReturn.EnableGlow = this.Glow.CheckState;
            toReturn.AntiAliasing = AntialiasingOptions[this.Antialiasing.SelectedItem];
            toReturn.DrawChunksReflected = this.ReflectTerrain.CheckState;
            toReturn.DrawEntityReflected =  this.ReflectEntities.CheckState;
            toReturn.AmbientOcclusion = this.AmbientOcclusion.CheckState;
            toReturn.CursorLightEnabled = this.CursorLight.CheckState;
            toReturn.EntityLighting = this.EntityLight.CheckState;
            toReturn.SelfIlluminationEnabled = this.SelfIllumination.CheckState;
            toReturn.ParticlePhysics = this.ParticlePhysics.CheckState;
            toReturn.GrassMotes = this.Motes.CheckState;
            //toReturn.NumMotes = (int)this.NumMotes.ScrollPosition + 100;

            toReturn.GuiScale = GuiScale.SelectedIndex + 1;
            toReturn.GuiAutoScale = this.GuiAutoScale.CheckState;
            return toReturn;
        }

        public void ConfirmSettings()
        {
            var prevSettings = GameSettings.Default.Clone();
            ApplySettings(GetNewSettings());
            OnEnter();
            var popup = new SettingsApplier()
            {
                PreviousSettings = prevSettings,
                State = this,
                Text = "Do you want to keep these settings? They will be reverted after 10 seconds...",
                CancelText = ""
            };
            GuiRoot.ShowModalPopup(popup);
            GuiRoot.RegisterForUpdate(popup);
        }

        public void ApplySettings(GameSettings.Settings settings)
        {
            // Graphics settings
            var preResolutionX = GameState.Game.Graphics.PreferredBackBufferWidth;
            var preResolutionY = GameState.Game.Graphics.PreferredBackBufferHeight;
            var preFullscreen = GameSettings.Default.Fullscreen;
            var preGuiScale = GameSettings.Default.GuiScale;
            var preVsync = GameSettings.Default.VSync;
            var prevAutoScale = GameSettings.Default.AutoSave;

            GameSettings.Default = settings.Clone();

            GameSettings.Default.ResolutionX = settings.ResolutionX;
            GameSettings.Default.ResolutionY = settings.ResolutionY;

            GameSettings.Default.Fullscreen = this.Fullscreen.CheckState;
            GameSettings.Default.ChunkDrawDistance = this.ChunkDrawDistance.ScrollPosition + 1.0f;
            GameSettings.Default.EntityUpdateDistance = this.EntityUpdateDistance.ScrollPosition + 1.0f;
            //GameSettings.Default.VertexCullDistance = this.VertexCullDistance.ScrollPosition + 0.1f;
            //GameSettings.Default.ChunkGenerateDistance = this.GenerateDistance.ScrollPosition + 1.0f;
            GameSettings.Default.EnableGlow = this.Glow.CheckState;
            GameSettings.Default.AntiAliasing = AntialiasingOptions[this.Antialiasing.SelectedItem];
            GameSettings.Default.DrawChunksReflected = this.ReflectTerrain.CheckState;
            GameSettings.Default.DrawEntityReflected = this.ReflectEntities.CheckState;
            //GameSettings.Default.CalculateSunlight = this.Sunlight.CheckState;
            GameSettings.Default.AmbientOcclusion = this.AmbientOcclusion.CheckState;
            //GameSettings.Default.CalculateRamps = this.Ramps.CheckState;
            GameSettings.Default.CursorLightEnabled = this.CursorLight.CheckState;
            GameSettings.Default.EntityLighting = this.EntityLight.CheckState;
            GameSettings.Default.SelfIlluminationEnabled = this.SelfIllumination.CheckState;
            GameSettings.Default.ParticlePhysics = this.ParticlePhysics.CheckState;
            GameSettings.Default.GrassMotes = this.Motes.CheckState;
            GameSettings.Default.VSync = this.VSync.CheckState;
            //GameSettings.Default.NumMotes = (int)this.NumMotes.ScrollPosition + 100;
            //GameSettings.Default.UseLightmaps = this.LightMap.CheckState;
            //GameSettings.Default.UseDynamicShadows = this.DynamicShadows.CheckState;
            GameSettings.Default.TutorialDisabledGlobally = this.DisableTutorialForAllGames.CheckState;
            GameSettings.Default.SaveLocation = settings.SaveLocation;

            GameSettings.Default.GuiScale = GuiScale.SelectedIndex + 1;
            
            if (preResolutionX != GameSettings.Default.ResolutionX || 
                preResolutionY != GameSettings.Default.ResolutionY ||
                preFullscreen != GameSettings.Default.Fullscreen ||
                preVsync != GameSettings.Default.VSync)
            {
                Game.Graphics.PreferredBackBufferWidth = GameSettings.Default.ResolutionX;
                Game.Graphics.PreferredBackBufferHeight = GameSettings.Default.ResolutionY;
                Game.Graphics.IsFullScreen = GameSettings.Default.Fullscreen;
                Game.Graphics.SynchronizeWithVerticalRetrace = GameSettings.Default.VSync;
                try
                {
                    Game.Graphics.ApplyChanges();
                }
                catch (NoSuitableGraphicsDeviceException)
                {
                    GameSettings.Default.ResolutionX = preResolutionX;
                    GameSettings.Default.ResolutionY = preResolutionY;
                    GameSettings.Default.Fullscreen = preFullscreen;
                    SetBestResolution();
                    this.Fullscreen.CheckState = GameSettings.Default.Fullscreen;
                    GuiRoot.ShowModalPopup(new Gui.Widgets.Popup
                        {
                            Text = "Could not change display mode. Previous settings restored.",
                            TextSize = 1,
                            PopupDestructionType = PopupDestructionType.DestroyOnOffClick
                        });
                }               
            }

            if (preResolutionX != GameSettings.Default.ResolutionX ||
                preResolutionY != GameSettings.Default.ResolutionY ||
                preGuiScale != GameSettings.Default.GuiScale ||
                prevAutoScale != GameSettings.Default.GuiAutoScale)
            {
                GuiRoot.RenderData.CalculateScreenSize();
                RebuildGui();
                DwarfGame.RebuildConsole();
            }

            HasChanges = false;

            

            if (World != null && EnableTutorial != null)
                World.TutorialManager.TutorialEnabled = EnableTutorial.CheckState;

            GameSettings.Save();
        }

        private void LoadSettings()
        {
            // Set all the widget states based on game settings.

            // Gameplay settings
            this.MoveSpeed.ScrollPosition = GameSettings.Default.CameraScrollSpeed;
            this.ZoomSpeed.ScrollPosition = GameSettings.Default.CameraZoomSpeed;
            this.EdgeScrolling.CheckState = GameSettings.Default.EnableEdgeScroll;
            this.FollowSurface.CheckState = GameSettings.Default.CameraFollowSurface;
            //this.FogOfWar.CheckState = GameSettings.Default.FogofWar;
            this.AutoFarming.CheckState = GameSettings.Default.AllowAutoFarming;
            this.AutoDigging.CheckState = GameSettings.Default.AllowAutoDigging;
            this.InvertZoom.CheckState = GameSettings.Default.InvertZoom;
            this.ZoomTowardMouse.CheckState = GameSettings.Default.ZoomCameraTowardMouse;
            this.PlayIntro.CheckState = GameSettings.Default.DisplayIntro;
            this.AllowReporting.CheckState = GameSettings.Default.AllowReporting;
            this.Autosave.CheckState = GameSettings.Default.AutoSave;
            (this.AutoSaveFrequency.GetChild(1) as HorizontalSlider).ScrollPosition =
                (int)(GameSettings.Default.AutoSaveTimeMinutes - 5);
            this.MaxSaves.SelectedIndex = this.MaxSaves.Items.IndexOf(GameSettings.Default.MaxSaves.ToString());
            this.DisableTutorialForAllGames.CheckState = GameSettings.Default.TutorialDisabledGlobally;
            this.SaveLocation.Text = String.IsNullOrEmpty(GameSettings.Default.SaveLocation) ? "" : GameSettings.Default.SaveLocation;

            // Audio settings
            this.MasterVolume.ScrollPosition = GameSettings.Default.MasterVolume;
            this.SFXVolume.ScrollPosition = GameSettings.Default.SoundEffectVolume;
            this.MusicVolume.ScrollPosition = GameSettings.Default.MusicVolume;

            // Graphics settings
            this.EasyGraphicsSetting.SelectedIndex = 5;
            SetBestResolution();
            this.Fullscreen.CheckState = GameSettings.Default.Fullscreen;
            this.ChunkDrawDistance.ScrollPosition = GameSettings.Default.ChunkDrawDistance - 1.0f;
            this.EntityUpdateDistance.ScrollPosition = GameSettings.Default.EntityUpdateDistance - 1.0f;
            //this.VertexCullDistance.ScrollPosition = GameSettings.Default.VertexCullDistance - 0.1f;
            //this.GenerateDistance.ScrollPosition = GameSettings.Default.ChunkGenerateDistance - 1.0f;
            this.Glow.CheckState = GameSettings.Default.EnableGlow;
            
            var antialiasingIndex = 0;
            foreach (var option in AntialiasingOptions)
            {
                if (option.Value == GameSettings.Default.AntiAliasing)
                    this.Antialiasing.SelectedIndex = antialiasingIndex;
                antialiasingIndex += 1;
            }

            this.ReflectTerrain.CheckState = GameSettings.Default.DrawChunksReflected;
            this.ReflectEntities.CheckState = GameSettings.Default.DrawEntityReflected;
            this.AmbientOcclusion.CheckState = GameSettings.Default.AmbientOcclusion;
            this.CursorLight.CheckState = GameSettings.Default.CursorLightEnabled;
            this.EntityLight.CheckState = GameSettings.Default.EntityLighting;
            this.SelfIllumination.CheckState = GameSettings.Default.SelfIlluminationEnabled;
            this.ParticlePhysics.CheckState = GameSettings.Default.ParticlePhysics;
            this.Motes.CheckState = GameSettings.Default.GrassMotes;
            //this.NumMotes.ScrollPosition = GameSettings.Default.NumMotes - 100;
            //this.LightMap.CheckState = GameSettings.Default.UseLightmaps;
            //this.DynamicShadows.CheckState = GameSettings.Default.UseDynamicShadows;

            GuiScale.SelectedIndex = GameSettings.Default.GuiScale - 1;
            GuiAutoScale.CheckState = GameSettings.Default.GuiAutoScale;
            VSync.CheckState = GameSettings.Default.VSync;

            if (World != null && EnableTutorial != null)
                EnableTutorial.CheckState = World.TutorialManager.TutorialEnabled;

            HasChanges = false;
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                    if (@event.Message == InputEvents.KeyUp && @event.Args.KeyValue == (int)Microsoft.Xna.Framework.Input.Keys.Escape)
                    {
                        CloseButton.OnClick(CloseButton, new InputEventArgs());
                    }
                }
            }

            GuiRoot.Update(gameTime.ToRealTime());
            SoundManager.Update(gameTime, null, null);
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}