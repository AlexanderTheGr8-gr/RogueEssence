﻿using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;

namespace RogueEssence.Dungeon
{
    [Serializable]
    public class Tile : ITile
    {
        //ground, water, lava that can be changed
        [Dev.SubGroup]
        public TerrainTile Data;
        //traps, wonder tiles, stairs, etc. that can be removed
        public EffectTile Effect;

        public int ID { get { return Data.ID; } set { Data.ID = value; } }

        public Tile()
        {
            Data = new TerrainTile();
            Effect = new EffectTile();
        }

        public Tile(int type)
        {
            Data = new TerrainTile(type);
            Effect = new EffectTile();
        }

        public Tile(int type, Loc loc)
        {
            Data = new TerrainTile(type);
            Effect = new EffectTile(loc);
        }

        protected Tile(Tile other)
        {
            Data = other.Data.Copy();
            Effect = new EffectTile(other.Effect);
        }
        public ITile Copy() { return new Tile(this); }

        public bool TileEquivalent(ITile other)
        {
            Tile tile = other as Tile;
            if (tile == null)
                return false;
            return tile.ID == ID;
        }

        public override string ToString()
        {
            List<string> values = new List<string>();
            if (Data.ID > -1)
                values.Add(DataManager.Instance.DataIndices[DataManager.DataType.Terrain].Entries[Data.ID].Name.ToLocal());
            if (Effect.ID > -1)
                values.Add(DataManager.Instance.DataIndices[DataManager.DataType.Tile].Entries[Effect.ID].Name.ToLocal());
            string features = string.Join("/", values);
            return string.Format("{0}: {1}", this.GetType().Name, features);
        }
    }
}
