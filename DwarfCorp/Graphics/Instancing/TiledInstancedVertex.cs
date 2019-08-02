﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// Extends the vertex declaration to support which instance the vertex
    /// refers to. The GPU draws instances based on this information.
    /// </summary>
    public struct TiledInstancedVertex
    {
        public Matrix Transform;
        public Color LightRamp;
        public Color SelectionBufferColor;
        public Color VertexColorTint;
        public Vector4 TileBounds;

        public TiledInstancedVertex(Matrix world, Color lightRamp, Color selectionColor, Color vertexColor, Vector4 TileBounds)
        {
            Transform = world;
            LightRamp = lightRamp;
            SelectionBufferColor = selectionColor;
            VertexColorTint = vertexColor;
            this.TileBounds = TileBounds;
        }

        public static int SizeOf<T>(T obj)
        {
            return global::System.Runtime.InteropServices.Marshal.SizeOf(obj);
        }


        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
            // World Matrix Data
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(SizeOf(Vector4.One), VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
            new VertexElement(2 * SizeOf(Vector4.One), VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
            new VertexElement(3 * SizeOf(Vector4.One), VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3),
            //Colour Data
            new VertexElement(4 * SizeOf(Vector4.One), VertexElementFormat.Color, VertexElementUsage.Color, 3),
            new VertexElement(4 * SizeOf(Vector4.One) + SizeOf(Microsoft.Xna.Framework.Color.White), VertexElementFormat.Color, VertexElementUsage.Color, 4),
            new VertexElement(4 * SizeOf(Vector4.One) + 2 * SizeOf(Microsoft.Xna.Framework.Color.White), VertexElementFormat.Color, VertexElementUsage.Color, 5),
            // TILE BOUNDS
            new VertexElement(4 * SizeOf(Vector4.One) + 3 * SizeOf(Color.White), VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 5)
            );
    }

}