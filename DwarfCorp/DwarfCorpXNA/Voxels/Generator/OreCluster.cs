using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using LibNoise;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Math = System.Math;

namespace DwarfCorp
{
    public struct OreCluster
    {
        public VoxelType Type { get; set; }
        public Vector3 Size { get; set; }
        public Matrix Transform { get; set; }
    }
}
