﻿// Bobber.cs
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
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This component follows its parent at a specified radius;
    /// </summary>
    public class Follower : Body, IUpdateableComponent
    {
        public Body ParentBody { get; set; }
        public float FollowRadius { get; set;  }
        public Vector3 TargetPos { get; set; }
        public float FollowRate { get; set; }
        public Follower()
        {

        }

        public Follower(Body parentBody) :
            base(parentBody.Manager, "Follower", parentBody, Matrix.Identity, Vector3.One, Vector3.Zero, false)
        {
            ParentBody = parentBody;
            FollowRadius = 1.5f;
            TargetPos = ParentBody.Position;
            FollowRate = 0.1f;
        }

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            Vector3 parentCurrentPos = ParentBody.Position;
            if ((parentCurrentPos - TargetPos).Length() > FollowRadius)
            {
                TargetPos = parentCurrentPos;
            }
            Vector3 newPos = (Position*(1.0f - FollowRate) + TargetPos*(FollowRate));
            Matrix newTransform = GlobalTransform;
            newTransform.Translation = newPos;
            newTransform = newTransform * Matrix.Invert(ParentBody.GlobalTransform);
            LocalTransform = newTransform;
            base.Update(gameTime, chunks, camera);
        }
    }

}