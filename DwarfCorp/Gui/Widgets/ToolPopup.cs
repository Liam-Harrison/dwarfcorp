using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class ToolPopup : Widget
    {
        public float Lifetime = 3.0f;
        private DateTime CreationTime = DateTime.Now;

        public override void Construct()
        {
            Border = "border-dark";
            Root.RegisterForUpdate(this);
            var bestSize = GetBestSize();
            Rect.Width = bestSize.X;
            Rect.Height = bestSize.Y;
            IsFloater = true;

            TextColor = Vector4.One;
            Rect = MathFunctions.SnapRect(Rect, Root.RenderData.VirtualScreen);

            OnUpdate = (sender, time) =>
            {
                if ((DateTime.Now - CreationTime).TotalSeconds > Lifetime)
                    sender.Close();
            };
        }
    }
}
