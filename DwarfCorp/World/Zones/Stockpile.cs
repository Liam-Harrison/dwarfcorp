using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Stockpile : Zone
    {
        [ZoneFactory("Stockpile")]
        private static Zone _factory(ZoneType Data, WorldManager World)
        {
            return new Stockpile(Data, World);
        }

        public Stockpile()
        {

        }

        protected Stockpile(ZoneType Data, WorldManager World) :
            base(Data, World)
        {
            Boxes = new List<GameComponent>();
            BlacklistResources = new List<Resource.ResourceTags>()
            {
                Resource.ResourceTags.Corpse,
                Resource.ResourceTags.Money
            };
        }

        private static uint maxID = 0;
        public List<GameComponent> Boxes { get; set; }
        public string BoxType = "Crate";
        public Vector3 BoxOffset = Vector3.Zero;
        private Timer HandleStockpilesTimer = new Timer(5.5f, false, Timer.TimerMode.Real);

        public override string GetDescriptionString()
        {
            return ID;
        }

        // If this is empty, all resources are allowed if and only if whitelist is empty. Otherwise,
        // all but these resources are allowed.
        public List<Resource.ResourceTags> BlacklistResources = new List<Resource.ResourceTags>();
        // If this is empty, all resources are allowed if and only if blacklist is empty. Otherwise,
        // only these resources are allowed.
        public List<Resource.ResourceTags> WhitelistResources = new List<Resource.ResourceTags>(); 

        public static uint NextID()
        {
            maxID++;
            return maxID;
        }

        public bool IsAllowed(String type)
        {
            var resource = Library.GetResourceType(type);
            if (WhitelistResources.Count == 0)
            {
                if (BlacklistResources.Count == 0)
                    return true;

                return !BlacklistResources.Any(tag => resource.Tags.Any(otherTag => otherTag == tag));
            }

            if (BlacklistResources.Count != 0) return true;
            return WhitelistResources.Count == 0 || WhitelistResources.Any(tag => resource.Tags.Any(otherTag => otherTag == tag));
        }

        public void KillBox(GameComponent component)
        {
            ZoneBodies.Remove(component);
            var deathMotion = new EaseMotion(0.8f, component.LocalTransform, component.LocalTransform.Translation + new Vector3(0, -1, 0));
            component.AnimationQueue.Add(deathMotion);
            deathMotion.OnComplete += component.Die;
            SoundManager.PlaySound(ContentPaths.Audio.whoosh, component.LocalTransform.Translation);
            World.ParticleManager.Trigger("puff", component.LocalTransform.Translation + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 90);
        }

        public void CreateBox(Vector3 pos)
        {
            //WorldManager.DoLazy(() =>
            //{
                Vector3 startPos = pos + new Vector3(0.0f, -0.1f, 0.0f) + BoxOffset;
                Vector3 endPos = pos + new Vector3(0.0f, 1.1f, 0.0f) + BoxOffset;

                GameComponent crate = EntityFactory.CreateEntity<GameComponent>(BoxType, startPos);
                crate.AnimationQueue.Add(new EaseMotion(0.8f, crate.LocalTransform, endPos));
                Boxes.Add(crate);
                AddBody(crate);
                SoundManager.PlaySound(ContentPaths.Audio.whoosh, startPos);
                if (World.ParticleManager != null)
                    World.ParticleManager.Trigger("puff", pos + new Vector3(0.5f, 1.5f, 0.5f), Color.White, 90);
            //});
        }

        public void HandleBoxes()
        {
            if (Voxels == null || Boxes == null)
                return;

            if (Boxes.Any(b => b.IsDead))
            {
                ZoneBodies.RemoveAll(z => z.IsDead);
                Boxes.RemoveAll(c => c.IsDead);

                for (int i = 0; i < Boxes.Count; i++)
                    Boxes[i].LocalPosition = new Vector3(0.5f, 1.5f, 0.5f) + Voxels[i].WorldPosition + VertexNoise.GetNoiseVectorFromRepeatingTexture(Voxels[i].WorldPosition + new Vector3(0.5f, 0, 0.5f));
            }

            if (Voxels.Count == 0)
            {
                foreach(GameComponent component in Boxes)
                    KillBox(component);
                Boxes.Clear();
            }

            int numBoxes = Math.Min(Math.Max(Resources.CurrentResourceCount / ResourcesPerVoxel, 1), Voxels.Count);

            if (Resources.CurrentResourceCount == 0)
                numBoxes = 0;

            if (Boxes.Count > numBoxes)
            {
                for (int i = Boxes.Count - 1; i >= numBoxes; i--)
                {
                    KillBox(Boxes[i]);
                    Boxes.RemoveAt(i);
                }
            }
            else if (Boxes.Count < numBoxes)
            {
                for (int i = Boxes.Count; i < numBoxes; i++)
                    CreateBox(Voxels[i].WorldPosition + VertexNoise.GetNoiseVectorFromRepeatingTexture(Voxels[i].WorldPosition + new Vector3(0.5f, 0, 0.5f)));
            }
        }
        
        public override bool AddItem(GameComponent component)
        {
            if (component.Tags.Count == 0)
                return false;

            var resourceType = component.Tags[0];
            if (!IsAllowed(resourceType))
                return false;

            bool worked =  base.AddItem(component);
            HandleBoxes();

            if (Boxes.Count > 0)
            {
                TossMotion toss = new TossMotion(1.0f, 2.5f, component.LocalTransform,
                    Boxes[Boxes.Count - 1].LocalTransform.Translation + new Vector3(0.5f, 0.5f, 0.5f));

                if (component.GetRoot().GetComponent<Physics>().HasValue(out var physics))
                    physics.CollideMode = Physics.CollisionMode.None;

                component.AnimationQueue.Add(toss);
                toss.OnComplete += component.Die;
            }
            else
                component.Die();

            World.RecomputeCachedResourceState();
            return worked;
        }

        public override void Destroy()
        {
            var box = GetBoundingBox();
            box.Min += Vector3.Up;
            box.Max += Vector3.Up;

            foreach(var resource in EntityFactory.CreateResourcePiles(Resources.Resources.Values, box))
            {

            }

            foreach (var resource in Resources.Enumerate())
            {
                var resourceType = Library.GetResourceType(resource.Type);

                foreach (var tag in resourceType.Tags)
                {
                    if (World.PersistentData.CachedResourceTagCounts.ContainsKey(tag)) // Todo: Move to World Manager.
                    {
                        World.PersistentData.CachedResourceTagCounts[tag] -= resource.Count;
                        System.Diagnostics.Trace.Assert(World.PersistentData.CachedResourceTagCounts[tag] >= 0);
                    }
                }
            }

            World.RecomputeCachedVoxelstate();

            base.Destroy();
        }

        public override void RecalculateMaxResources()
        {
            HandleBoxes();
            base.RecalculateMaxResources();
        }

        public override void Update(DwarfTime Time)
        {
            HandleStockpilesTimer.Update(Time);

            if (HandleStockpilesTimer.HasTriggered)
                foreach (var blacklist in BlacklistResources)
                    foreach (var resourcePair in Resources.Resources)
                    {
                        if (resourcePair.Value.Count == 0)
                            continue;

                        var resourceType = Library.GetResourceType(resourcePair.Key);

                        if (resourceType.Tags.Any(tag => tag == blacklist))
                        {
                            var transferTask = new TransferResourcesTask(World, ID, resourcePair.Value.CloneResource());
                            if (World.TaskManager.HasTask(transferTask))
                                continue;
                            World.TaskManager.AddTask(transferTask);
                        }
                    }

            base.Update(Time);
        }
    }
}
