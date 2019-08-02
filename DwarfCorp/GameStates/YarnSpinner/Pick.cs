﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates.YarnSpinner
{
    static class Pick
    {
        private static Random Random = new Random();

        [YarnCommand("pick")]
        private static void _pick(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            State.EnterQueueingAction((list) =>
            {
                if (list.Count > 0)
                    State.PlayerInterface.Speak(list[Random.Next(list.Count)] + "\n");
            });
        }
    }
}
