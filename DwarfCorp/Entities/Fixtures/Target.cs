﻿using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class Target : CraftedFixture
    {
        [EntityFactory("Target")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Target(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources"));
        }

        public Target()
        {

        }

        public Target(ComponentManager componentManager, Vector3 position, List<ResourceAmount> resources) :
            base(componentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(0, 5), new DwarfCorp.CraftDetails(componentManager, "Target", resources))
        {
            Name = "Target";
            Tags.Add("Target");
            Tags.Add("Train");

            if (GetRoot().GetComponent<Health>().HasValue(out var health))
            {
                health.MaxHealth = 500;
                health.Hp = 500;
            }
        }
    }
}
