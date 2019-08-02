using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class Confirm : Widget
    {
        public enum Result
        {
            OKAY,
            CANCEL
        }

        public Result DialogResult = Result.CANCEL;
        public string OkayText = "Okay";
        public string CancelText = "Cancel";

        public override void Construct()
        {
            //Set size and center on screen.
            if (Rect.Width == 0)
            {
                Rect = new Rectangle(0, 0, 400, 100 + (String.IsNullOrEmpty(Text) ? 0 : 100));
                Rect.X = (Root.RenderData.VirtualScreen.Width/2) - 200;
                Rect.Y = (Root.RenderData.VirtualScreen.Height/2) - 50;
            }

            Border = "border-fancy";
            Font = "font10";
            TextVerticalAlign = VerticalAlign.Center;
            TextHorizontalAlign = HorizontalAlign.Center;

            if (!String.IsNullOrEmpty(OkayText))
            {
                AddChild(new Gui.Widgets.Button
                {
                    Text = OkayText,
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center,
                    MinimumSize = new Point(64, 32),
                    Border = "border-button",
                    OnClick = (sender, args) =>
                    {
                        DialogResult = Result.OKAY;
                        this.Close();
                    },
                    AutoLayout = AutoLayout.FloatBottomRight
                });
            }

            if (!String.IsNullOrEmpty(CancelText))
            {
                AddChild(new Gui.Widgets.Button
                {
                    Text = CancelText,
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center,
                    MinimumSize = new Point(64, 32),
                    Border = "border-button",
                    OnClick = (sender, args) =>
                    {
                        DialogResult = Result.CANCEL;
                        this.Close();
                    },
                    AutoLayout = AutoLayout.FloatBottomLeft
                });
            }

            Layout();
        }
    }
}
