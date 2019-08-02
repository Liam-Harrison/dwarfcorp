using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class ParticleEffect
    {
        public List<ParticleEmitter> Emitters { get; set; }

        public ParticleEffect()
        {
            Emitters = new List<ParticleEmitter>();
        }       

        public void Trigger(int num, Vector3 position, Color tint)
        {
            for (int i = 0; i < num; i++)
            {
                Emitters[MathFunctions.Random.Next(Emitters.Count)].Trigger(1, position, tint);
            }
        }

        public void Create(Vector3 position, Vector3 velocity, Color tint)
        {
            Emitters[MathFunctions.Random.Next(Emitters.Count)].CreateParticle(position, velocity, tint);
        }

        public void Create(Vector3 position, Vector3 velocity, Color tint, Vector3 direction)
        {
            Emitters[MathFunctions.Random.Next(Emitters.Count)].CreateParticle(position, velocity, tint, direction);
        }
    }

    /// <summary>
    /// This class manages a set of particle effects, and allows them to be triggered
    /// at locations in 3D space.
    /// </summary>
    [JsonObject(IsReference =  true)]
    public class ParticleManager
    {
        public Dictionary<string, ParticleEffect> Effects { get; set; }

        public ParticleManager()
        {
          
        }

        public void Load(ComponentManager Components, Dictionary<string, List<EmitterData>> data)
        {
            Effects.Clear();
            foreach (var effect in data)
            {
                RegisterEffect(Components, effect.Key, effect.Value.ToArray());
            }
        }

        public ParticleManager(ComponentManager Components)
        {
            // Todo: Better modding support - make it a list of named emitters.
            Effects = new Dictionary<string, ParticleEffect>();
            Load(Components, FileUtils.LoadJsonFromResolvedPath<Dictionary<string, List<EmitterData>>>(ContentPaths.Particles.particles));
        }

        public void Trigger(string emitter, Vector3 position, Color tint, int num)
        {
            Effects[emitter].Trigger(num, position, tint);
        }

        public void TriggerRay(string emitter, Vector3 position, Vector3 dest, float spacing = 0.5f)
        {
            var r = (dest - position);
            r.Normalize();
            float l = (dest - position).Length();
            for (float t = 0; t < l; t += spacing)
            {
                Create(emitter, position + r * t, r, Color.White);
            }
        }


        public void Create(string emitter, Vector3 position, Vector3 velocity, Color tint)
        {
            Effects[emitter].Create(position, velocity, tint);
        }

        public void Create(string emitter, Vector3 position, Vector3 velocity, Color tint, Vector3 direction)
        {
            Effects[emitter].Create(position, velocity, tint, direction);
        }


        public void RegisterEffect(ComponentManager Components, string name, params EmitterData[] data)
        {
            List<ParticleEmitter> emitters = new List<ParticleEmitter>();

            foreach (EmitterData emitter in data)
            {
                emitters.Add(new ParticleEmitter(Components, name, Matrix.Identity, emitter));
            }
            Effects[name] = new ParticleEffect()
            {
                Emitters = emitters
            };
        }

        public void Update(DwarfTime time, WorldManager world)
        {
            foreach(var effect in Effects)
            {
                foreach(var emitter in effect.Value.Emitters)
                {
                    emitter.Update(this, time, world.ChunkManager, world.Renderer.Camera);
                }
            }
        }

        public void Render(WorldManager world, GraphicsDevice device)
        {
            foreach (var effect in Effects)
            {
                foreach (var emitter in effect.Value.Emitters)
                {
                    emitter.Render(world.Renderer.Camera, DwarfGame.SpriteBatch, device, world.Renderer.DefaultShader);
                }
            }
        }
    }
}
