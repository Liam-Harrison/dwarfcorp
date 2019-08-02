using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public static class TextureTool
    {
        public static MemoryTexture MemoryTextureFromTexture2D(Texture2D Source)
        {
            if (Source == null || Source.IsDisposed || Source.GraphicsDevice.IsDisposed)
            {
                return null;
            }
            var r = new MemoryTexture(Source.Width, Source.Height);
            Source.GetData(r.Data);
            return r;
        }

        public static Texture2D Texture2DFromMemoryTexture(GraphicsDevice Device, MemoryTexture Source)
        {
            if (Source == null || Device.IsDisposed)
            {
                return null;
            }
            var r = new Texture2D(Device, Source.Width, Source.Height);
            r.SetData(Source.Data);
            return r;
        }

        public static Palette OptimizedPaletteFromMemoryTexture(MemoryTexture Source)
        {
            if (Source == null)
            {
                return null;
            }
            return new Palette(Source.Data.Distinct());
        }

        public static Palette RawPaletteFromMemoryTexture(MemoryTexture Source)
        {
            if (Source == null)
            {
                return null;
            }
            return new Palette(Source.Data);
        }

        public static Palette RawPaletteFromTexture2D(Texture2D Source)
        {
            return RawPaletteFromMemoryTexture(MemoryTextureFromTexture2D(Source));
        }

        public static IndexedDecomposition DecomposeTexture(MemoryTexture Source)
        {
            var r = new IndexedDecomposition();
            if (Source == null)
            {
                return r;
            }
            r.Palette = OptimizedPaletteFromMemoryTexture(Source);
            r.IndexedTexture = new IndexedTexture(Source.Width, Source.Height);
            for (var i = 0; i < Source.Data.Length; ++i)
                r.IndexedTexture.Data[i] = (byte)r.Palette.IndexOf(Source.Data[i]);
            return r;
        }

        public static IndexedTexture DecomposeTexture(MemoryTexture Source, Palette Palette)
        {
            if (Source == null)
                return null;

            var r = new IndexedTexture(Source.Width, Source.Height);
            for (var i = 0; i < Source.Data.Length; ++i)
            {
                var index = Palette.IndexOf(Source.Data[i]);
                if (index >= 0)
                    r.Data[i] = (byte)index;
                else
                    r.Data[i] = 0;
            }
            return r;
        }

        public static MemoryTexture ComposeTexture(IndexedTexture Source, Palette Palette)
        {
            if (Source == null)
            {
                return null;
            }
            var r = new MemoryTexture(Source.Width, Source.Height);
            for (var i = 0; i < Source.Data.Length; ++i)
                r.Data[i] = Palette[Source.Data[i]];
            return r;
        }

        public static MemoryTexture MemoryTextureFromPalette(Palette Palette)
        {
            if (Palette == null)
            {
                return null;
            }
            var dim = (int)Math.Ceiling(Math.Sqrt(Palette.Count));
            var r = new MemoryTexture(dim, (int)Math.Ceiling((float)Palette.Count / dim));
            Palette.CopyTo(r.Data);
            return r;
        }

        public static Palette ExtractPaletteFromDirectoryRecursive(String Path)
        {
            var r = new Palette();
            foreach (var file in AssetManager.EnumerateAllFiles(Path))
            {
                var texture = AssetManager.RawLoadTexture(file);
                if (texture != null)
                    r.AddRange(OptimizedPaletteFromMemoryTexture(MemoryTextureFromTexture2D(texture)));
            }
            r = new Palette(r.Distinct());
            r.Sort((a, b) => (int)a.PackedValue - (int)b.PackedValue);
            return r;
        }

        public static void ClearMemoryTexture(MemoryTexture Texture)
        {
            if (Texture == null)
            {
                return;
            }

            for (var i = 0; i < Texture.Data.Length; ++i)
                Texture.Data[i] = Color.Transparent;
        }

        public static void Blit(IndexedTexture Source, Palette SourcePalette, MemoryTexture Destination)
        {
            if (Source == null || SourcePalette == null || Destination == null)
                return;

            var width = Math.Min(Source.Width, Destination.Width);
            var height = Math.Min(Source.Height, Destination.Height);

            for (var y = 0; y < height; ++y)
            {
                var sourceIndex = Source.Index(0, y);
                var destinationIndex = Destination.Index(0, y);
                var endSourceIndex = sourceIndex + width;

                while (sourceIndex < endSourceIndex)
                {
                    var sourcePixel = SourcePalette[Source.Data[sourceIndex]];
                    if (sourcePixel.A != 0)
                        Destination.Data[destinationIndex] = sourcePixel;

                    sourceIndex += 1;
                    destinationIndex += 1;
                }
            }
        }

        [ConsoleCommandHandler("PALETTE")]
        private static String DumpPalette(String Path)
        {
            var palette = TextureTool.ExtractPaletteFromDirectoryRecursive(Path);
            var paletteTexture = TextureTool.Texture2DFromMemoryTexture(DwarfGame.GuiSkin.Device, TextureTool.MemoryTextureFromPalette(palette));
            paletteTexture.SaveAsPng(File.OpenWrite("palette.png"), paletteTexture.Width, paletteTexture.Height);
            return "Dumped.";
        }
    }
}