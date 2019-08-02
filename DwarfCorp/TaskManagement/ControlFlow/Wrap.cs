﻿// Wrap.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// A behavior which wraps a coroutine. (Something which returns IEnumerable(status)))
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Wrap : Act
    {
        private Func<IEnumerable<Status>> Function { get; set; }

        public Wrap(Func<IEnumerable<Status>> fn)
        {
            Name = fn.Method.Name;
            Function = fn;
        }

        public override void Initialize()
        {
            Enumerator = Run().GetEnumerator();
        }

        public override IEnumerable<Status> Run()
        {
            LastTickedChild = this;
            foreach (var result in Function())
                yield return result;
        }
    }

    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Do : Act
    {
        private Func<bool> Function { get; set; }

        public Do(Func<bool> fn)
        {
            Name = fn.Method.Name;
            Function = fn;
        }

        public override void Initialize()
        {
            Enumerator = Run().GetEnumerator();
        }

        public override IEnumerable<Status> Run()
        {
            LastTickedChild = this;
            if (Function())
            {
                yield return Status.Success;
            }
            yield return Status.Fail;
        }
    }
}