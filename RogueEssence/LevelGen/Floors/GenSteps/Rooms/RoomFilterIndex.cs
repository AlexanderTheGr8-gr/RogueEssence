﻿// <copyright file="RoomFilterIndex.cs" company="Audino">
// Copyright (c) Audino
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using RogueElements;

namespace RogueEssence.LevelGen
{
    [Serializable]
    public class RoomFilterIndex : BaseRoomFilter
    {
        public RoomFilterIndex()
        {
            this.Indices = new HashSet<int>();
        }

        public RoomFilterIndex(bool negate, params int[] indices)
        {
            this.Negate = negate;
            this.Indices = new HashSet<int>();
            foreach (int idx in indices)
                this.Indices.Add(idx);
        }

        public bool Negate { get; set; }

        public HashSet<int> Indices { get; set; }

        public override bool PassesFilter(IRoomPlan plan)
        {
            IndexRoom indexRoom;
            if (!plan.Components.TryGet<IndexRoom>(out indexRoom))
                return false;

            return this.Indices.Contains(indexRoom.Index) != this.Negate;
        }

        public override string ToString()
        {
            if (this.Negate)
                return string.Format("{0}: ^{1}", this.GetType().GetFormattedTypeName(), this.Indices.ToString());
            else
                return string.Format("{0}: {1}", this.GetType().GetFormattedTypeName(), this.Indices.ToString());
        }
    }
}
