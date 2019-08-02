using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class EditableTextField : Widget
    {
        private int CursorPosition = 0;
        public bool HiliteOnMouseOver = true;
        public String PromptText = "";

        public Action<Widget> OnTextChange = null;
        public Action<Widget> OnEnter = null;

        public class BeforeTextChangeEventArgs
        {
            public String NewText;
            public bool Cancelled = false;
        }

        public Action<Widget, BeforeTextChangeEventArgs> BeforeTextChange = null;

        public Action<Widget, int> ArrowKeyUpDown = null;

        public override void Construct()
        {
            if (String.IsNullOrEmpty(Border)) Border = "border-thin";
            if (Text == null) Text = "";

            // Note: Cursor won't draw properly if these are changed. Click events may also break.
            // Widget should probably be able to handle different alignments.
            TextVerticalAlign = VerticalAlign.Center;
            TextHorizontalAlign = HorizontalAlign.Left;

            OnClick += (sender, args) =>
                {
                    if (IsAnyParentHidden())
                    {
                        return;
                    }
                    if (Object.ReferenceEquals(this, Root.FocusItem))
                    {
                        // This widget already has focus - move cursor to click position.

                        var clickIndex = 0;
                        var clickX = args.X - this.GetDrawableInterior().X;
                        var searchIndex = 0;
                        var font = Root.GetTileSheet(Font);
                        
                        while (true)
                        {
                            if (searchIndex == Text.Length)
                            {
                                clickIndex = Text.Length;
                                break;
                            }

                            var glyphSize = font.GlyphSize(Text[searchIndex] - ' ');
                            if (clickX < glyphSize.X)
                            {
                                clickIndex = searchIndex;
                                if (clickX > (glyphSize.X / 2)) clickIndex += 1;
                                break;
                            }

                            clickX -= glyphSize.X;
                            searchIndex += 1;
                        }

                        CursorPosition = clickIndex;
                        Invalidate();
                        args.Handled = true;
                    }
                    else
                    {
                        // Take focus and move cursor to end of text.
                        Root.SetFocus(this);
                        CursorPosition = Text == null ? 0 : Text.Length;
                        Invalidate();
                        args.Handled = true;
                    }
                };

            OnGainFocus += (sender) => this.Invalidate();
            OnLoseFocus += (sender) => this.Invalidate();
            OnUpdateWhileFocus += (sender) => this.Invalidate();
            OnKeyUp += (sender, args) =>
            {
                if (IsAnyParentHidden())
                {
                    return;
                }

                args.Handled = true;
            };
            OnKeyPress += (sender, args) =>
                {
                    var font = Root.GetTileSheet(Font);
                    var glyphSize = font.GlyphSize(' ');
                    int numChars = 2 * (Rect.Width / glyphSize.X);
                    if (IsAnyParentHidden())
                    {
                        return;
                    }

                    // Actual logic of modifying the string is outsourced.
                    var beforeEventArgs = new BeforeTextChangeEventArgs
                        {
                            NewText = TextFieldLogic.Process(Text, CursorPosition, args.KeyValue, out CursorPosition),
                            Cancelled = false
                        };
                    Root.SafeCall(BeforeTextChange, this, beforeEventArgs);
                    if (beforeEventArgs.Cancelled == false)
                    {
                        Text = beforeEventArgs.NewText.Substring(0, Math.Min(numChars, beforeEventArgs.NewText.Length));
                        //Text = beforeEventArgs.NewText;
                        Root.SafeCall(OnTextChange, this);
                        Invalidate();
                        args.Handled = true;
                    }
                };

            OnKeyDown += (sender, args) =>
                {
                    if (IsAnyParentHidden())
                    {
                        return;
                    }
#if XNA_BUILD
                    if (args.KeyValue == (int)global::System.Windows.Forms.Keys.Up)
                    {
                        Root.SafeCall(ArrowKeyUpDown, this, 1);
                    }
                    else if (args.KeyValue == (int)global::System.Windows.Forms.Keys.Down)
                    {
                        Root.SafeCall(ArrowKeyUpDown, this, -1);
                    }

                    if (args.KeyValue == (int)global::System.Windows.Forms.Keys.Enter)
                    {
                        Root.SafeCall(OnEnter, this);
                    }
#else
                    if (args.KeyValue == (int)Microsoft.Xna.Framework.Input.Keys.Up)
                    {
                        Root.SafeCall(ArrowKeyUpDown, this, 1);
                    }
                    else if (args.KeyValue == (int)Microsoft.Xna.Framework.Input.Keys.Down)
                    {
                        Root.SafeCall(ArrowKeyUpDown, this, -1);
                    }

                    if (args.KeyValue == (int)Microsoft.Xna.Framework.Input.Keys.Enter)
                    {
                        Root.SafeCall(OnEnter, this);
                    }
#endif

                    var beforeEventArgs = new BeforeTextChangeEventArgs
                        {
                            NewText = TextFieldLogic.HandleSpecialKeys(Text, CursorPosition, args.KeyValue, out CursorPosition),
                            Cancelled = false
                        };
                    Root.SafeCall(BeforeTextChange, this, beforeEventArgs);
                    if (beforeEventArgs.Cancelled == false)
                    {
                        Text = beforeEventArgs.NewText;
                        Root.SafeCall(OnTextChange, this);
                        Invalidate();
                    }
                    //Root.SafeCall(OnTextChange, this);
                    Invalidate();
                    args.Handled = true;
                };

            if (HiliteOnMouseOver)
            {
                var color = TextColor;
                OnMouseEnter += (widget, action) =>
                {
                    widget.TextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4();
                    widget.Invalidate();
                };

                OnMouseLeave += (widget, action) =>
                {
                    widget.TextColor = color;
                    widget.Invalidate();
                };
            }
        }

        protected Mesh GetBackgroundMesh()
        {
            if (Hidden) throw new InvalidOperationException();

            if (Transparent || Root == null)
                return Mesh.EmptyMesh();

            var result = new List<Mesh>();

            if (Background != null)
                result.Add(Mesh.Quad()
                    .Scale(Rect.Width, Rect.Height)
                    .Translate(Rect.X, Rect.Y)
                    .Colorize(BackgroundColor)
                    .Texture(Root.GetTileSheet(Background.Sheet).TileMatrix(Background.Tile)));

            if (!String.IsNullOrEmpty(Border))
            {
                //Create a 'scale 9' background 
                result.Add(
                    Mesh.CreateScale9Background(Rect, Root.GetTileSheet(Border))
                    .Colorize(BackgroundColor));
            }
            return Mesh.Merge(result.ToArray());
        }

        protected Mesh GetTextMesh()
        {
            var result = new List<Mesh>();

            // Add text label
            if (!String.IsNullOrEmpty(Text))
                GetTextMesh(result);
            else
                GetTextMesh(result, PromptText, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
            

            return Mesh.Merge(result.ToArray());
        }

        protected override Mesh Redraw()
        {
            if (Object.ReferenceEquals(this, Root.FocusItem))
            {
                if (CursorPosition > Text.Length || CursorPosition < 0)
                {
                    CursorPosition = Text.Length;
                }

                var cursorTime = (int)(Math.Floor(Root.RunTime / Root.CursorBlinkTime));
                if ((cursorTime % 2) == 0)
                {
                    var font = Root.GetTileSheet(Font);
                    var drawableArea = this.GetDrawableInterior();

                    var pipeGlyph = font.GlyphSize('|' - ' ');
                    var cursorMesh = Mesh.Quad()
                        .Scale(pipeGlyph.X * TextSize, pipeGlyph.Y * TextSize)
                        .Translate(drawableArea.X 
                            + font.MeasureString(Text.Substring(0, CursorPosition)).X * TextSize 
                            - ((pipeGlyph.X * TextSize) / 2),
                            drawableArea.Y + ((drawableArea.Height - (pipeGlyph.Y * TextSize)) / 2))
                        .Texture(font.TileMatrix((int)('|' - ' ')))
                        .Colorize(new Vector4(1, 0, 0, 1));
                    return Mesh.Merge(GetBackgroundMesh(), Mesh.Clip(Mesh.Merge(GetTextMesh(), cursorMesh), GetDrawableInterior()));
                }
            }
            
            return Mesh.Merge(GetBackgroundMesh(), Mesh.Clip(GetTextMesh(), GetDrawableInterior()));
        }
    }
}
