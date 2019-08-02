// GoToZoneAct.cs
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
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature takes an item to an open stockpile and leaves it there.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GoToZoneAct : CompoundCreatureAct
    {
        public Zone Destination { get; set; }
        public string DestinationName { get; set; }
        public GoToZoneAct()
        {

        }

        public GoToZoneAct(CreatureAI agent, string zone) :
            base(agent)
        {
            Tree = null;
            Name = "Goto zone : " + zone;
            Destination = null;
            DestinationName = zone;
        }

        public GoToZoneAct(CreatureAI agent, Zone zone) :
            base(agent)
        {
            Tree = null;
            Name = "Goto zone : " + zone.ID;
            Destination = zone;
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            if (DestinationName != null && Destination == null)
            {
                Destination = Agent.Blackboard.GetData<Zone>(DestinationName);
            }
            if (Tree == null)
            {
                var voxel = Datastructures.SelectRandom(Destination.Voxels);
                Tree = new GoToVoxelAct(VoxelHelpers.GetVoxelAbove(voxel), PlanAct.PlanType.Into, Agent);
                Tree.Initialize();
            }

            if (Tree == null)
                yield return Status.Fail;
            else
                foreach (Status s in base.Run())
                    yield return s;
        }
    }
}