using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Door : CraftedFixture
    {
        [EntityFactory("Door")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var resources = Data.GetData<List<ResourceAmount>>("Resources", null);
            var craftType = Data.GetData<string>("CraftType", null);
            if (resources == null && craftType != null)
            {
                resources = new List<ResourceAmount>();
                if (Library.GetCraftable(craftType).HasValue(out var craftItem))
                    foreach (var resource in craftItem.RequiredResources)
                    {
                        var genericResource = Library.EnumerateResourceTypesWithTag(resource.Type).FirstOrDefault();
                        resources.Add(new ResourceAmount(genericResource, resource.Count));
                    }
            }
            else if (resources == null && craftType == null)
            {
                craftType = "Wooden Door";
                resources = new List<ResourceAmount>() { new ResourceAmount("Wood") };
            }
            else if (craftType == null)
                craftType = "Wooden Door";

            return new Door(Manager, Position, Manager.World.PlayerFaction, resources, craftType);
        }

        public Faction TeamFaction { get; set; }
        public Matrix ClosedTransform { get; set; }
        public Timer OpenTimer { get; set; }
        bool IsOpen { get; set; }
        bool IsMoving { get; set; }

        protected static Dictionary<Resource.ResourceTags, Point> Sprites = new Dictionary<Resource.ResourceTags, Point>()
        {
            {
                Resource.ResourceTags.Metal,
                new Point(1, 8)
            },
            {
                Resource.ResourceTags.Stone,
                new Point(0, 8)
            },
            {
                Resource.ResourceTags.Wood,
                new Point(3, 1)
            }
        };

        protected static Dictionary<Resource.ResourceTags, float> Healths = new Dictionary<Resource.ResourceTags, float>()
        {
            {
                Resource.ResourceTags.Metal,
                75.0f
            },
            {
                Resource.ResourceTags.Stone,
                80.0f
            },
            {
                Resource.ResourceTags.Wood,
                30.0f
            }
        };

        protected static float DefaultHealth = 30.0f;
        protected static Point DefaultSprite = new Point(0, 8);

        protected static float GetHealth(String type)
        {
            var resource = Library.GetResourceType(type);
            foreach(var tag in resource.Tags)
                if (Healths.ContainsKey(tag))
                    return Healths[tag];
            return DefaultHealth;
        }

        public Door()
        {
            IsOpen = false;
        }

        public Door(ComponentManager manager, Vector3 position, Faction team, List<ResourceAmount> resourceType, string craftType) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new FixtureCraftDetails(manager)
            {
                Resources = resourceType.ConvertAll(p => new ResourceAmount(p)),
                Sprites = Door.Sprites,
                DefaultSpriteFrame = Door.DefaultSprite,
                CraftType = craftType,
            }, SimpleSprite.OrientMode.Fixed)
        {
            IsMoving = false;
            IsOpen = false;
            OpenTimer = new Timer(0.5f, false);
            TeamFaction = team;
            Name = resourceType.FirstOrDefault().Type + " Door";
            Tags.Add("Door");

            OrientToWalls();
            ClosedTransform = LocalTransform;
            CollisionType = CollisionType.Static;
            var hp = GetHealth(resourceType.FirstOrDefault().Type);
            AddChild(new Health(manager, "Health", hp, 0.0f, hp));
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            if (GetComponent<SimpleSprite>().HasValue(out var sprite))
            {
                sprite.OrientationType = SimpleSprite.OrientMode.Fixed;
                sprite.LocalTransform = Matrix.CreateRotationY(0.5f * (float)Math.PI);
            }
        }

        public Matrix CreateHingeTransform(float angle)
        {
            Matrix toReturn = Matrix.Identity;
            Vector3 hinge = new Vector3(0, 0, 0.5f);
            toReturn = Matrix.CreateTranslation(hinge) * toReturn;
            toReturn = Matrix.CreateRotationY(angle) * toReturn;
            toReturn = Matrix.CreateTranslation(-hinge)* toReturn;
            return toReturn;
        }

        public void Open()
        {
            if (!IsOpen)
            {
                IsMoving = true;
                OpenTimer.Reset();
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_door_open_generic, Position, true, 0.5f);
            }

            IsOpen = true;
        }

        public void Close()
        {
            if (IsOpen)
            {
                IsMoving = true;
                OpenTimer.Reset();
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_door_close_generic, Position, true, 0.5f);
            }
            IsOpen = false;
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (!Active)
                return;

            if (IsMoving)
            {
                OpenTimer.Update(gameTime);
                if (OpenTimer.HasTriggered)
                    IsMoving = false;
                else
                {
                    float t = Easing.CubicEaseInOut(OpenTimer.CurrentTimeSeconds, 0.0f, 1.0f,
                        OpenTimer.TargetTimeSeconds);

                    if (GetComponent<SimpleSprite>().HasValue(out var sprite))
                    {
                        // Transform the sprite instead of the entire thing.
                        if (IsOpen)
                            sprite.LocalTransform = Matrix.CreateRotationY(0.5f * (float)Math.PI) * CreateHingeTransform(t * 1.57f);
                        else
                            sprite.LocalTransform = Matrix.CreateRotationY(0.5f * (float)Math.PI) * CreateHingeTransform((1.0f - t) * 1.57f);
                    }
                }
            }
            else
            {
                bool anyInside = false;
                foreach (CreatureAI minion in TeamFaction.Minions)
                {
                    if ((minion.Physics.Position - Position).LengthSquared() < 1)
                    {
                        if (!IsOpen)
                            Open();
                        anyInside = true;
                        break;
                    }
                }

                if (!IsMoving && !anyInside && IsOpen)
                    Close();
            }
        }
    }
}
