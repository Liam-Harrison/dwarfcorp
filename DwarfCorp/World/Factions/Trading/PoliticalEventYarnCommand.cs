﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.Factions.Trading
{
    static class PoliticalEventYarnCommand
    {
        [YarnCommand("political_event", "STRING", "NUMBER")]
        private static void _political_event(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var envoy = Memory.GetValue("$envoy").AsObject as TradeEnvoy;
            var playerFaction = Memory.GetValue("$player_faction").AsObject as Faction;
            var world = Memory.GetValue("$world").AsObject as WorldManager;

            if (envoy == null || playerFaction == null || world == null)
            {
                State.PlayerInterface.Output("Command 'political_event' can only be called from a TradeEnvoy initiated conversation.");
                return;
            }

            var politics = world.Overworld.GetPolitics(playerFaction.ParentFaction, envoy.OwnerFaction.ParentFaction);
            if (!politics.HasEvent((string)Arguments[0].Value))
            {
                politics.AddEvent(new PoliticalEvent()
                {
                    Description = (string)Arguments[0].Value,
                    Change = (float)Arguments[1].Value,
                });
            }
        }
    }
}
