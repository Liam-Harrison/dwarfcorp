﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.Factions.Trading
{
    static class RecallEnvoyYarnCommand
    {
        [YarnCommand("recall_envoy")]
        private static void _recall_envoy(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var envoy = Memory.GetValue("$envoy").AsObject as TradeEnvoy;

            if (envoy == null)
            {
                State.PlayerInterface.Output("Command 'recall_envoy' can only be called from a TradeEnvoy initiated conversation.");
                return;
            }

            envoy.RecallEnvoy();
        }
    }
}
