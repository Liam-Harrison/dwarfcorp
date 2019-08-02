﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates.YarnSpinner
{
    static class ShowPortrait
    {
        [YarnCommand("show_portrait")]
        private static void _show_portrait(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            State.PlayerInterface.ShowPortrait();
        }
    }
}
