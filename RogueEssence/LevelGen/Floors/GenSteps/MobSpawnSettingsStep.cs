﻿using System;
using RogueElements;
using RogueEssence.Dev;

namespace RogueEssence.LevelGen
{
    [Serializable]
    public class MobSpawnSettingsStep<T> : GenStep<T> where T : BaseMapGenContext
    {
        public int MaxFoes;
        public int RespawnTime;

        public MobSpawnSettingsStep()
        {
            MaxFoes = -1;
            RespawnTime = -1;
        }

        public MobSpawnSettingsStep(int maxTeams, int respawnTime)
        {
            MaxFoes = maxTeams;
            RespawnTime = respawnTime;
        }

        public override void Apply(T map)
        {
            if (MaxFoes > -1)
                map.MaxFoes = MaxFoes;
            if (RespawnTime > -1)
                map.RespawnTime = RespawnTime;
        }


        public override string ToString()
        {
            return String.Format("{0}: MaxFoes:{1} RespawnTurns:{2}", this.GetType().Name, MaxFoes, RespawnTime);
        }
    }
}
