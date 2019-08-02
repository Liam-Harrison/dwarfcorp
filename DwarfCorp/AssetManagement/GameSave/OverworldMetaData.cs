using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DwarfCorp
{
    [Serializable]
    public class OverworldMetaData
    {
        public string Version;
        public Overworld Overworld;

        public Dictionary<int, String> BiomeTypeMap;
        public List<Resource> Resources; // Dislike the way resources are generated on the fly.

        public OverworldMetaData()
        {
        }

        public OverworldMetaData(GraphicsDevice device, Overworld Overworld)
        {
            this.Overworld = Overworld;

            BiomeTypeMap = Library.GetBiomeTypeMap(); // This may need to be saved in branch meta data.
            Resources = Library.EnumerateResourceTypes().Where(r => r.Generated).ToList();
        }
    }
}