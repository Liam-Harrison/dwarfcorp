using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class FactionSet
    {
        public Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();
        
        public MaybeNull<OverworldFaction> GenerateOverworldFaction(Overworld Settings, int idx, int n)
        {
            if (Library.GetRandomIntelligentRace().HasValue(out var race))
            {
                var fact = new OverworldFaction()
                {
                    Race = race.Name,
                    Name = TextGenerator.ToTitleCase(TextGenerator.GenerateRandom(Datastructures.SelectRandom(race.FactionNameTemplates).ToArray())),
                    PrimaryColor = new HSLColor(idx * (255.0f / n), 255.0, MathFunctions.Rand(100.0f, 200.0f)),
                    GoodWill = MathFunctions.Rand(-1, 1),
                    InteractiveFaction = true
                };

                return fact;
            }
            else
                return null;
        }

        public void Initialize(WorldManager world, CompanyInformation CompanyInformation)
        {
            Factions["Player"] = new Faction(world, new OverworldFaction
            {
                Name = "Player",
                Race = "Corporate",
            });

            Factions["Corporate"] = new Faction(world, new OverworldFaction
            {
                Name = "Corporate",
                Race = "Corporate",
                InteractiveFaction = true,
                IsCorporate = true
            });

            Factions["Herbivore"] = new Faction(world, new OverworldFaction
            {
                Name = "Herbivore",
                Race = "Herbivore"
            });

            Factions["Carnivore"] = new Faction(world, new OverworldFaction
            {
                Name = "Carnivore",
                Race = "Carnivore"
            });

            Factions["Evil"] = new Faction(world, new OverworldFaction
            {
                Name = "Evil",
                Race = "Evil"
            });

            Factions["Dwarves"] = new Faction(world, new OverworldFaction
            {
                Name = "Dwarves",
                Race = "Dwarf"
            });

            Factions["Goblins"] = new Faction(world, new OverworldFaction // Todo: Normalize race name
            {
                Name = "Goblins",
                Race = "Goblins"
            });

            Factions["Elf"] = new Faction(world, new OverworldFaction
            {
                Name = "Elf",
                Race = "Elf"
            });

            Factions["Undead"] = new Faction(world, new OverworldFaction
            {
                Name = "Undead",
                Race = "Undead"
            });

            Factions["Demon"] = new Faction(world, new OverworldFaction
            {
                Name = "Demon",
                Race = "Demon"
            });

            Factions["Molemen"] = new Faction(world, new OverworldFaction
            {
                Name = "Molemen",
                Race = "Molemen"
            });
        }

        public FactionSet()
        {


        }

        public void Update(DwarfTime time)
        {
            foreach(var faction in Factions)
                faction.Value.Update(time);
        }

        public void AddFaction(Faction Faction)
        {
            Factions[Faction.ParentFaction.Name] = Faction;
        }
    }
}
