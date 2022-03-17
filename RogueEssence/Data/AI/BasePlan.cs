﻿using System;
using RogueElements;
using RogueEssence.Dungeon;

namespace RogueEssence.Data
{

    [Serializable]
    public abstract class BasePlan
    {

        public BasePlan() { }

        public abstract BasePlan CreateNew();
        public virtual void Initialize(Character controlledChar) { }//called at the beginning of a floor, or when it spawns, to start the AI
        public virtual void SwitchedIn() { }//called whenever this plan is switched in from another plan
        public abstract GameAction Think(Character controlledChar, bool preThink, IRandom rand);

    }

}
