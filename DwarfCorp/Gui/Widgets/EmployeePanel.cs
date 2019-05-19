using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class EmployeePanel : Columns
    {
        public Faction Faction;
        private Gui.Widgets.WidgetListView EmployeeList;

        private void RebuildEmployeeList()
        {
            EmployeeList.ClearItems();

            EmployeeList.AddItem(new Widget
            {
                Text = "+ Hire New Employee",
                MinimumSize = new Point(128, 64),
                OnClick = (sender, args) =>
                {
                    // Show hire dialog.
                    var dialog = Root.ConstructWidget(
                        new HireEmployeeDialog(Faction.Economy.Information)
                        {
                            Faction = Faction,
                            OnClose = (_s) =>
                            {
                                EmployeeList.Hidden = false;
                                RebuildEmployeeList();
                            }
                        });
                    Root.ShowModalPopup(dialog);
                    Faction.World.Tutorial("hire");
                    EmployeeList.Hidden = true;
                }
            });

            foreach (var employee in Faction.Minions)
            {
                var bar = Root.ConstructWidget(new Widget
                {
                    Background = new TileReference("basic", 0)
                });

                var employeeSprite = employee.GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>();
               
                if (employeeSprite != null)
                    bar.AddChild(new EmployeePortrait
                    {
                        AutoLayout = AutoLayout.DockLeft,
                        MinimumSize = new Point(48, 40),
                        MaximumSize = new Point(48, 40),
                        Sprite = employeeSprite.GetLayers(),
                        AnimationPlayer = employeeSprite.AnimPlayer
                    });

                bar.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockFill,
                    TextVerticalAlign = VerticalAlign.Center,
                    MinimumSize = new Point(128, 64),
                    Text = (employee.Stats.IsOverQualified ? employee.Stats.FullName + "*" : employee.Stats.FullName) + " (" + employee.Stats.Title ?? employee.Stats.CurrentLevel.Name + ")"
                });

                EmployeeList.AddItem(bar);
            }

            EmployeeList.SelectedIndex = 1;
        }

        public override void Construct()
        {
            var left = AddChild(new Widget());
            var right = AddChild(new EmployeeInfo
            {
                OnFireClicked = (sender) =>
                {
                    RebuildEmployeeList();
                }
            }) as EmployeeInfo;

            var bottomBar = left.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 30)
            });

            EmployeeList = left.AddChild(new Gui.Widgets.WidgetListView
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "font10",
                ItemHeight = 64,
                OnSelectedIndexChanged = (sender) =>
                {
                    if ((sender as Gui.Widgets.WidgetListView).SelectedIndex > 0 &&
                        (sender as Gui.Widgets.WidgetListView).SelectedIndex <= Faction.Minions.Count)
                    {
                        right.Hidden = false;
                        right.Employee = Faction.Minions[(sender as Gui.Widgets.WidgetListView).SelectedIndex - 1];
                    }
                    else
                        right.Hidden = true;
                }
            }) as Gui.Widgets.WidgetListView;

            RebuildEmployeeList();
        }
    }
}
