using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class GodMenu : HorizontalMenuTray.Tray
    {
        public WorldManager World;

        private void ActivateGodTool(String Command)
        {
            (World.UserInterface.Tools["God"] as GodModeTool).Command = Command;
            World.UserInterface.ChangeTool("God");
        }

        public override void Construct()
        {
            AutoSizeColumns = true;
            IsRootTray = true;

            ItemSource = new Gui.Widget[]
            {
                new HorizontalMenuTray.MenuItem
                {
                    Text = "DEBUG",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        ItemSize = new Point(200, 20),
                        ItemSource = Debugger.EnumerateSwitches().Select(s =>
                        new HorizontalMenuTray.CheckboxMenuItem
                        {
                            Text = Debugger.GetNicelyFormattedName(s.Name),
                            InitialState = s.State,
                            SetCallback = s.Set
                        })
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "CRASH",
                    OnClick = (sender, args) => throw new InvalidProgramException()
                },
            
                new HorizontalMenuTray.MenuItem
                {
                    Text = "BUILD",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        ItemSource = Library.EnumerateZoneTypes().Select(r =>
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = r.Name,
                                OnClick = (sender, args) => ActivateGodTool("Build/" + r)
                            })
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "SPAWN",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 8,
                        AutoSizeColumns = false,
                        ItemSize = new Point(110, 28),
                        ItemSource = EntityFactory.EnumerateEntityTypes()
                            .Where(s => !s.Contains("Resource") || !Library.GetResourceType(s.Substring(0, s.Length - " Resource".Length)).Generated)
                            .OrderBy(s => s).Select(s =>
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = s,
                                OnClick = (sender, args) => ActivateGodTool("Spawn/" + s),
                            })
                    }
                },
                
                new HorizontalMenuTray.MenuItem
                {
                    Text = "PLACE BLOCK",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 3,
                        ItemSource = Library.EnumerateVoxelTypes()
                            .Where(t => t.Name != "_empty" && t.Name != "water")
                            .OrderBy(s => s.Name)
                            .Select(s =>
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = s.Name,
                                    OnClick = (sender, args) => ActivateGodTool("Place/" + s.Name)
                                })
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "DELETE BLOCK",
                    OnClick = (sender, args) => ActivateGodTool("Delete Block")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "NUKE COLUMN",
                    OnClick = (sender, args) => ActivateGodTool("Nuke Column")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "KILL BLOCK",
                    OnClick = (sender, args) => ActivateGodTool("Kill Block")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "PLACE GRASS",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 3,
                        ItemSource = Library.EnumerateGrassTypes()
                            .OrderBy(s => s.Name)
                            .Select(s =>
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = s.Name,
                                    OnClick = (sender, args) => ActivateGodTool("Grass/" + s.Name)
                                })
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "PLACE RAIL",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 1,
                        ItemSource = new HorizontalMenuTray.MenuItem[]
                        {
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "RAW PIECES",
                                ExpansionChild = new HorizontalMenuTray.Tray
                                {
                                    Columns = 2,
                                    ItemSize = new Point(200, 20),
                                    ItemSource = Library.EnumerateRailPieces().Select(p =>
                                        new HorizontalMenuTray.MenuItem
                                        {
                                            Text = p.Name,
                                            OnClick = (sender, args) => ActivateGodTool("Rail/" + p.Name)
                                        })
                                }
                            },

                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "USING PATTERNS",
                                ExpansionChild = new HorizontalMenuTray.Tray
                                {
                                    Columns = 1,
                                    ItemSource = Library.EnumerateRailPatterns().Select( p =>
                                        new HorizontalMenuTray.MenuItem
                                        {
                                            Text = p.Name,
                                            OnClick = (sender, args) =>
                                            {
                                                var railTool = World.UserInterface.Tools["BuildRail"] as Rail.BuildRailTool;
                                                railTool.Pattern = p;
                                                World.UserInterface.ChangeTool("BuildRail");
                                                railTool.GodModeSwitch = true;
                                            }
                                        })
                                }
                            },

                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "PAINT",
                                OnClick = (sender, args) =>
                                {
                                    var railTool = World.UserInterface.Tools["PaintRail"] as Rail.PaintRailTool;
                                    railTool.SelectedResources = new List<ResourceAmount>(new ResourceAmount[] { new ResourceAmount("Rail", 1) });
                                    World.UserInterface.ChangeTool("PaintRail");
                                    railTool.GodModeSwitch = true;
                                }
                            }
                        }
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "AUTO SAVE",
                    OnClick = (sender, args) =>
                    {
                        World.UserInterface.AutoSave();
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "KILL THINGS",
                    OnClick = (sender, args) => ActivateGodTool("Kill Things")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "TRAILER",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        ItemSource = new List<HorizontalMenuTray.MenuItem>()
                        {
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "SPIN +",
                                    OnClick = (sender, args) => World.Renderer.Camera.Trailer(Vector3.Zero, 2.0f, 0.0f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "SPIN -",
                                    OnClick = (sender, args) => World.Renderer.Camera.Trailer(Vector3.Zero, -2.0f, 0.0f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "ZOOM -",
                                    OnClick = (sender, args) => World.Renderer.Camera.Trailer(Vector3.Zero, 0.0f, 2.5f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "ZOOM +",
                                    OnClick = (sender, args) => World.Renderer.Camera.Trailer(Vector3.Zero, 0.0f, -2.5f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "FWD",
                                    OnClick = (sender, args) => World.Renderer.Camera.Trailer(Vector3.Forward * 5, 0.0f, 0.0f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "BACK",
                                    OnClick = (sender, args) => World.Renderer.Camera.Trailer(Vector3.Backward * 5, 0.0f, 0.0f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "LEFT",
                                    OnClick = (sender, args) => World.Renderer.Camera.Trailer(Vector3.Left * 5, 0.0f, 0.0f),
                                },
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = "RIGHT",
                                    OnClick = (sender, args) => World.Renderer.Camera.Trailer(Vector3.Right * 5, 0.0f, 0.0f),
                                },
                        }

                    }

                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "FILL WATER",
                    OnClick = (sender, args) => ActivateGodTool("Fill Water")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "FILL LAVA",
                    OnClick = (sender, args) => ActivateGodTool("Fill Lava")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "TRADE ENVOY",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {

                            ItemSource = World.Factions.Factions.Values.Where(f => f.Race.IsIntelligent && f != World.PlayerFaction).Select(s =>
                            {
                                return new HorizontalMenuTray.MenuItem
                                {
                                    Text = s.ParentFaction.Name,
                                    OnClick = (sender, args) => s.SendTradeEnvoy()
                                };

                            }),
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "EVENT",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                            ItemSource = Events.Library.EnumerateEvents().Select(e =>
                            {
                                return new HorizontalMenuTray.MenuItem
                                {
                                    Text = e.Name,
                                    OnClick = (sender, args) => World.EventScheduler.ActivateEvent(World, e)
                                };

                            }),
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "WAR PARTY",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {

                            ItemSource = World.Factions.Factions.Values.Where(f => f.Race.IsIntelligent && f != World.PlayerFaction).Select(s =>
                            {
                                return new HorizontalMenuTray.MenuItem
                                {
                                    Text = s.ParentFaction.Name,
                                    OnClick = (sender, args) => s.SendWarParty()
                                };

                            }),
                    }
                },


                new HorizontalMenuTray.MenuItem
                {
                    Text = "DWARF BUX",
                    OnClick = (sender, args) => World.PlayerFaction.AddMoney(100m)
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "MINIONS",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        ItemSource = new HorizontalMenuTray.MenuItem[]
                        {
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "PAY",
                                OnClick = (sender, args) => World.PayEmployees()
                            },
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "STARVE",
                                OnClick = (sender, args) =>
                                {
                                    foreach(var minion in World.PlayerFaction.Minions)
                                        minion.Stats.Hunger.CurrentValue = 0;
                                }
                            },                
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "XP",
                                OnClick = (sender, args) =>
                                {
                                    foreach(var minion in World.PlayerFaction.Minions)
                                        minion.AddXP(100);
                                } 
                            },
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "DISEASE",
                                OnClick = (sender, args) => ActivateGodTool("Disease")
                            },
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "HAPPY",
                                OnClick = (sender, args) =>
                                {
                                    foreach (var minion in World.PlayerFaction.Minions)
                                        minion.Creature.AddThought("You used the god menu to make me happy.", new TimeSpan(0, 8, 0, 0), 100.0f);
                                }
                            },
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "PISSED",
                                OnClick = (sender, args) =>
                                {
                                    foreach (var minion in World.PlayerFaction.Minions)
                                        minion.Creature.AddThought("You used the god menu to piss me off.", new TimeSpan(0, 8, 0, 0), -100.0f);
                                }
                            },
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "GAMBLE",
                                OnClick = (sender, args) =>
                                {
                                    foreach(var employee in World.PlayerFaction.Minions)
                                    {
                                        employee.Stats.Boredom.CurrentValue = employee.Stats.Boredom.MinValue;
                                        employee.AddMoney(100);
                                        employee.AssignTask(new Scripting.GambleTask() { Priority = TaskPriority.High });
                                    }
                                }
                            },
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = "PASS OUT",
                                OnClick = (sender, args) =>
                                {
                                    var employee = Datastructures.SelectRandom(World.PlayerFaction.Minions);
                                    if (employee != null)
                                        employee.Creature.Heal(-employee.Stats.Health.CurrentValue * employee.Creature.MaxHealth + 1);
                                }
                            }
                        }
                    }
                },
                 
                new HorizontalMenuTray.MenuItem
                {
                    Text = "SPAWN TEST",
                    OnClick = (sender, args) =>
                    {
                        // Copy is required because spawning some types results in the creation of new types. EG, snakes create snake meat.
                        var keys = EntityFactory.EnumerateEntityTypes().ToList();
                        int num = keys.Count();
                        float gridSize = (float)Math.Ceiling(Math.Sqrt((double)num));
                        Vector3 gridCenter = World.Renderer.CursorLightPos;
                        int i = 0;
                        for (float dx = -gridSize/2; dx <= gridSize/2; dx++)
                        {
                            for (float dz = -gridSize/2; dz <= gridSize/2; dz++)
                            {
                                if (i >= num)
                                {
                                    continue;
                                }

                                Vector3 pos = MathFunctions.Clamp(gridCenter + new Vector3(dx, World.WorldSizeInVoxels.Y, dz), World.ChunkManager.Bounds);
                                VoxelHandle handle = VoxelHelpers.FindFirstVisibleVoxelOnRay(World.ChunkManager, pos, pos + Vector3.Down * 100);
                                if (handle.IsValid)
                                    EntityFactory.CreateEntity<GameComponent>(keys[i], handle.WorldPosition + Vector3.Up);
                                i++;
                            }
                        }
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "SPAWN CRAFTS",
                    OnClick = (sender, args) =>
                    {
                        // Copy is required because spawning some types results in the creation of new types. EG, snakes create snake meat.
                        var itemTypes = Library.EnumerateCraftables().Where(craft => craft.Type == CraftItem.CraftType.Object).ToList();
                        int num = itemTypes.Count();
                        float gridSize = (float)Math.Ceiling(Math.Sqrt((double)num));
                        Vector3 gridCenter = World.Renderer.CursorLightPos;

                        int i = 0;
                        for (float dx = -gridSize/2; dx <= gridSize/2; dx++)
                        {
                            for (float dz = -gridSize/2; dz <= gridSize/2; dz++)
                            {
                                if (i < num)
                                {
                                    var item = itemTypes[i];
                                    if (item.Name != "Explosive")
                                    {
                                        Vector3 pos = MathFunctions.Clamp(gridCenter + new Vector3(dx, World.WorldSizeInVoxels.Y, dz), World.ChunkManager.Bounds);
                                        VoxelHandle handle = VoxelHelpers.FindFirstVisibleVoxelOnRay(World.ChunkManager, pos, pos + Vector3.Down * 100);

                                        if (handle.IsValid)
                                        {

                                            var blackboard = new Blackboard();
                                            List<ResourceAmount> resources = item.RequiredResources.Select(r => new ResourceAmount(Library.EnumerateResourceTypesWithTag(r.Type).First(), r.Count)).ToList();
                                            blackboard.SetData<List<ResourceAmount>>("Resources", resources);
                                            blackboard.SetData<string>("CraftType", item.Name);

                                            var entity = EntityFactory.CreateEntity<GameComponent>(item.EntityName, handle.WorldPosition + Vector3.Up + item.SpawnOffset, blackboard);
                                            if (entity != null)
                                            {
                                                if (item.AddToOwnedPool)
                                                    World.PlayerFaction.OwnedObjects.Add(entity as GameComponent);
                                                if (item.Moveable)
                                                    entity.Tags.Add("Moveable");
                                                if (item.Deconstructable)
                                                    entity.Tags.Add("Deconstructable");
                                            }
                                        }
                                    }
                                }
                                i++;
                            }
                        }
                        
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "+1 HOUR",
                    OnClick = (sender, args) =>
                    {
                        World.Time.CurrentDate += new TimeSpan(1, 0, 0);
                        
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "FORCE REBUILD",
                    OnClick = (sender, args) =>
                    {
                         foreach (var chunk in World.ChunkManager.ChunkMap)
                            for (int Y = 0; Y < VoxelConstants.ChunkSizeY; ++Y)
                                chunk.InvalidateSlice(Y);
                    }
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "REPULSE",
                    OnClick = (sender, args) => ActivateGodTool("Repulse")
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "SLOWMO",
                    OnClick = (sender, args) => GameSettings.Default.EnableSlowMotion = !GameSettings.Default.EnableSlowMotion
                },
                new HorizontalMenuTray.MenuItem
                {
                    Text = "LET IT SNOW",
                    OnClick = (sender, args) =>
                    {
                        var storm = Weather.CreateStorm(Vector3.One, 100.0f, World);
                        storm.TypeofStorm = StormType.SnowStorm;
                        storm.Start();
                    }
                }
            };

            base.Construct();
        }        
    }
}
