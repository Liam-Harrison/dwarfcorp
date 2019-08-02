﻿
# How to create a DwarfCorp mod

This is a basic guide to creating a mod for DwarfCorp. By the end of this tutorial, we will have created a new kind of entity - a Mana Lamp. This is exactly like a regular lamp, except powered by MANA! Also it is blue.

## Data Files
Mods for DwarfCorp are pretty simple. All they do is replace content files. Inside the DwarfCorp folder, you will find two subfolders - Content (Where all of the base content is stored) and Mods. Inside Mods, there is a subfolder for each unique mod. The directory structure of a mod mirrors the directory structure of Content. So for our mana lamp, the first thing we need to do is create a folder.

`DwarfCorp/Mods/ManaLamp`

To replace base content, just give the file the same name, and put it in the same directory. You can reskin voxels by replacing `Content/Terrain/terrain_tiles.xnb` with `Mods/MyMod/Terrain/terrain_tiles.png`. The game will automatically load your replacement file instead of the base content.

Mods are loaded in a specific order. When a mod is loaded, it's content replaces base content. The game lets you change the order of mod loading to troubleshoot conflicting mods (mods that modify the same assets). Whichever mod is loaded last will override the content of anything that comes before.

Textures are a special case - while the base content is compiled to compressed XNBs, the engine is capable of loading PNGs or JPGs. You don't need to compile texture assets to XNB to use them in a mod. The game will happily load your PNG instead of the base content's XNB. So, for our mana lamp, the first thing we need are some custom graphics - mana-lamp.png. It's just the regular lamp tinted blue.

To allow dwarves to build mana lamps, we need an entry in craft-items.json. Json assets are another special case - we don't need to repeat the entirerity of the base content's json. Json from mods will be appended to the base content, rather than replacing it, so we only need to add one entry. In this case, I copied the entry for a normal lamp and changed a few values.

```json
<<craft-items.json>>
[
  {
    "Name": "Mana Lamp",
    "EntityName": "Mana Lamp",
    "RequiredResources": [
      {
        "ResourceType": "Magical",
        "NumResources": 1
      }
    ],
    "Icon": {
      "Sheet": "mana-lamp",
      "Tile": 0
    },
    "BaseCraftTime": 10.0,
    "Description": "Dwarves need to see sometimes too!",
    "Type": "Object",
    "Prerequisites": [
      "OnGround"
    ],
    "ResourceCreated": {
      "_value": ""
    },
    "CraftLocation": "",
    "Verb": "Build",
    "PastTeseVerb": "Built",
    "CurrentVerb": "Building",
    "AllowHeterogenous": false,
    "SpawnOffset": "0, 0.5, 0",
    "AddToOwnedPool": true,
    "Moveable": true,
    "CraftActBehavior": "Normal"
  }
]
```

1. First I changed the name. If a craft-item has the same name as something in the base content, it will replace it in the list. This is true of all json list style assets.

2. Second, I changed EntityName. This is the name of the actual entity type to spawn when this item is built - we'll see where this is used when we get to the code portion of the mana lamp.

3. I also changed the required resource from fuel to magical. Because, duh - a mana lamp costs mana!

4. Finally, the "Icon" field is the graphic displayed in the GUI build menu. The sheet here is a reference to the sheet listed in the asset 'newgui/sheets.json', so we'll have to modify that as well.

```json
<<newgui/sheets.json>>
[
  {
    "Name": "mana-lamp",
    "Texture": "mana-lamp",
    "TileWidth": 32,
    "TileHeight": 32
  }
]
```

This one is pretty simple. Remember that these json files need to be at exactly the right path or the engine won't be able to find them.

## Code

The last piece of our mana lamp is the code that backs it up. When mods are loaded, the game will find any .cs files in the mod's root directory and compile them into an assembly. We decided to go with this method because it brings us a number of features that are important to us.
	
A. You don't need to be able to build the source of the game to create a mod. Setting up the build environment for DwarfCorp can be challenging, so we didn't want to make it a requirement.
B. We can see the source code for the mod. We experimented with app domains and other methods of making mods secure, but reached a point where mods were either going to be 'dangerous' or just weren't going to happen. This method, at least, allows mods to be vetted.
C. It forces mods to be open source. Besides the aformentioned ability to see what a mod is actually doing, we felt that this would encourage modders to build upon each other in interesting ways.
D. Mods aren't tied to a specific version of the game. Yes, updates to the core game can and will break them, but if we required modders to compile themselves, every new version would definitely break every mod. This way - it might break, but most of the time, it won't.

On to the code. The mana lamp source file looks like any other C# file -

```cs
<<ManaLamp.cs>>
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp;

namespace ManaLampMod
{
    public class ManaLamp : CraftedBody
    {
```

ManaLamp is craftable so it inherits from DwarfCorp.CraftedBody. The namespace is mostly irrelevant.

This is an example of a 'mod hook' - this particular hook is for the entity factory. It's telling the factory, if you're asked to create a 'Mana Lamp', call this function. All our function actually does is pass some arguments on to the ManaLamp constructor. Be sure to pass on the resources used to craft the item, or the player won't get them back if the item is destroyed.

```cs
        [EntityFactory("Mana Lamp")]
        private static DwarfCorp.GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new ManaLamp(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
        }
```

Always give your new entities a default constructor or you won't be able to load save games that contain them.

```cs
        public ManaLamp()
        {

        }
```

`CreateCosmeticChildren` is called when an object is loaded as part of a save game. This is a good pattern to follow and is how most items in the game are implemented.

```cs
        public ManaLamp(ComponentManager Manager, Vector3 position, List<ResourceAmount> Resources) :
            base(Manager, "Lamp", Matrix.CreateTranslation(position), new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(Manager, "Mana Lamp", Resources))
        {
            Tags.Add("Lamp");
            CollisionType = CollisionManager.CollisionType.Static;

            CreateCosmeticChildren(Manager);
        }
```

Honestly I just copied this all from the default lamp and changed the sprite sheet. This is beyond the scope of our tutorial here - but did I mention that the game is OPEN SOURCE? You can also copy the code for existing game objects and modify it!

```cs
        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = new SpriteSheet("mana-lamp", 32);

            List<Point> frames = new List<Point>
            {
                new Point(0, 0),
                new Point(2, 0),
                new Point(1, 0),
                new Point(2, 0)
            };

            var lampAnimation = AnimationLibrary.CreateAnimation(spriteSheet, frames, "ManaLampAnimation");
            lampAnimation.Loops = true;

            var sprite = AddChild(new AnimatedSprite(Manager, "sprite", Matrix.Identity, false)
            {
                LightsWithVoxels = false,
                OrientationType = AnimatedSprite.OrientMode.YAxis,
            }) as AnimatedSprite;

            sprite.AddAnimation(lampAnimation);
            sprite.AnimPlayer.Play(lampAnimation);
            sprite.SetFlag(Flag.ShouldSerialize, false);

            // This is a hack to make the animation update at least once even when the object is created inactive by the craftbuilder.
            sprite.AnimPlayer.Update(new DwarfTime());

            AddChild(new LightEmitter(Manager, "light", Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 255, 8)
            {
                HasMoved = true
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
```

So the last thing to get our mana lamp into the game is to install all of this into the mod directory. It should look like this:

```
DwarfCorp/ Mods/ ManaLamp/ newgui/           sheets.json
                           craft-items.json
						   mana-lamp.png
						   ManaLamp.cs
```

Fire up the game, and you should find the mod in the mod manager off the main menu.