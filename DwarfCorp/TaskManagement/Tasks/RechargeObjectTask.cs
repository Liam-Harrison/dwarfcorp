// KillEntityTask.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class RechargeObjectTask : Task
    {
        public MagicalObject Entity = null;

        public RechargeObjectTask()
        {
            MaxAssignable = 3;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Tiring;
        }

        public RechargeObjectTask(MagicalObject entity)
        {
            Entity = entity;
            MaxAssignable = 3;
            Name = "Recharge " + entity.Name + " " + entity.GlobalID;
            Priority = TaskPriority.Medium;
            AutoRetry = true;
            Category = TaskCategory.Research;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Tiring;
        }


        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            return new RechargeObjectAct(Entity, creature.AI);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            if (agent == null || Entity == null)
            {
                return 10000;
            }

            else return (agent.AI.Position - (Entity.GetRoot() as GameComponent).LocalTransform.Translation).LengthSquared() * 0.01f;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return Entity != null && !Entity.IsDead && Entity.CurrentCharges < Entity.MaxCharges;
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (Entity == null || Entity.IsDead || ((Entity.GetRoot() as GameComponent).Position - agent.AI.Position).Length() > 100)
            {
                return true;
            }

            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent == null || agent.IsDead || Entity == null || Entity.IsDead || Entity.CurrentCharges >= Entity.MaxCharges)
                return Feasibility.Infeasible;
            else
            {
                return Feasibility.Feasible;
            }
        }

        public override bool IsComplete(WorldManager World)
        {
            return Entity == null || Entity.IsDead || Entity.CurrentCharges == Entity.MaxCharges;
        }
    }

}
