﻿using System;
using RogueEssence.Dungeon;
using RogueElements;
using RogueEssence.Dev;

namespace RogueEssence.Data
{
    [Serializable]
    public class PassiveData
    {
        /// <summary>
        /// Triggered when the character equips an item.
        /// </summary>
        [ListCollapse]
        public PriorityList<ItemGivenEvent> OnEquips;

        /// <summary>
        /// Triggered when the character picks up an item.
        /// </summary>
        [ListCollapse]
        public PriorityList<ItemGivenEvent> OnPickups;

        /// <summary>
        /// Occurs before a status condition is added to the player.
        /// Can be used to cancel the operation.
        /// </summary>
        [ListCollapse]
        public PriorityList<StatusGivenEvent> BeforeStatusAdds;

        /// <summary>
        /// Occurs before a status condition is being added to another player.
        /// Can be used to cancel the operation.
        /// </summary>
        [ListCollapse]
        public PriorityList<StatusGivenEvent> BeforeStatusAddings;

        /// <summary>
        /// Occurs after a status condition is added to the player.
        /// </summary>
        [ListCollapse]
        public PriorityList<StatusGivenEvent> OnStatusAdds;

        /// <summary>
        /// Occurs after a status condition is removed from the player.
        /// </summary>
        [ListCollapse]
        public PriorityList<StatusGivenEvent> OnStatusRemoves;

        /// <summary>
        /// Occurs after a status condition is added to the map.
        /// </summary>
        [ListCollapse]
        public PriorityList<MapStatusGivenEvent> OnMapStatusAdds;

        /// <summary>
        /// Occurs after a status condition is removed from the map.
        /// </summary>
        [ListCollapse]
        public PriorityList<MapStatusGivenEvent> OnMapStatusRemoves;

        /// <summary>
        /// Occurs when the map starts, or when the character is spawned.
        /// </summary>
        [ListCollapse]
        public PriorityList<SingleCharEvent> OnMapStarts;

        /// <summary>
        /// Occurs when the character's turn begins.
        /// </summary>
        [ListCollapse]
        public PriorityList<SingleCharEvent> OnTurnStarts;

        /// <summary>
        /// Occurs when the character's turn ends.
        /// </summary>
        [ListCollapse]
        public PriorityList<SingleCharEvent> OnTurnEnds;

        /// <summary>
        /// Occurs when the map's global turn ends.
        /// Distinct from character turn ends because a character may move multiple times a turn.
        /// </summary>
        [ListCollapse]
        public PriorityList<SingleCharEvent> OnMapTurnEnds;

        /// <summary>
        /// Occurs when the character walks.
        /// </summary>
        [ListCollapse]
        public PriorityList<SingleCharEvent> OnWalks;

        /// <summary>
        /// Occurs when the character dies.
        /// </summary>
        [ListCollapse]
        public PriorityList<SingleCharEvent> OnDeaths;

        /// <summary>
        /// Occurs whenever the character's features need to be updated.
        /// </summary>
        [ListCollapse]
        public PriorityList<RefreshEvent> OnRefresh;

        /// <summary>
        /// Occurs before the character attempts a battle action,
        /// such as attacking, using an item, activating a tile, etc.
        /// Cancelling the action at this phase prevents the turn from completing.
        /// </summary>
        [ListCollapse]
        public PriorityList<BattleEvent> BeforeTryActions;

        /// <summary>
        /// Occurs before the character executes a battle action,
        /// such as attacking, using an item, activating a tile, etc.
        /// Cancelling the action at this phase will still result in the turn completing.
        /// </summary>
        [ListCollapse]
        public PriorityList<BattleEvent> BeforeActions;

        /// <summary>
        /// Occurs after the character initiates the battle action.
        /// </summary>
        [ListCollapse]
        public PriorityList<BattleEvent> OnActions;

        /// <summary>
        /// Before the character hits a target in a battle action.
        /// </summary>
        [ListCollapse]
        public PriorityList<BattleEvent> BeforeHittings;

        /// <summary>
        /// Before the character gets hit in a battle action.
        /// </summary>
        [ListCollapse]
        public PriorityList<BattleEvent> BeforeBeingHits;

        /// <summary>
        /// After the character hits a target in a battle action.
        /// </summary>
        [ListCollapse]
        public PriorityList<BattleEvent> AfterHittings;

        /// <summary>
        /// After the character gets hit in a battle action.
        /// </summary>
        [ListCollapse]
        public PriorityList<BattleEvent> AfterBeingHits;

        /// <summary>
        /// Effects for when the character hits a tile in a battle action.
        /// </summary>
        [ListCollapse]
        public PriorityList<BattleEvent> OnHitTiles;

        /// <summary>
        /// Occurs after the character finishes the battle action.
        /// </summary>
        [ListCollapse]
        public PriorityList<BattleEvent> AfterActions;

        /// <summary>
        /// Modifies the elemental effect system when attacking.
        /// </summary>
        [ListCollapse]
        public PriorityList<ElementEffectEvent> UserElementEffects;

        /// <summary>
        /// Modifies the elemental effect system when being targeted by an attack.
        /// </summary>
        [ListCollapse]
        public PriorityList<ElementEffectEvent> TargetElementEffects;

        /// <summary>
        /// Used to modify HP regen values.
        /// </summary>
        [ListCollapse]
        public PriorityList<HPChangeEvent> ModifyHPs;

        /// <summary>
        /// Used to modify healing events.
        /// </summary>
        [ListCollapse]
        public PriorityList<HPChangeEvent> RestoreHPs;

        public PassiveData()
        {
            OnEquips = new PriorityList<ItemGivenEvent>();
            OnPickups = new PriorityList<ItemGivenEvent>();

            BeforeStatusAdds = new PriorityList<StatusGivenEvent>();
            BeforeStatusAddings = new PriorityList<StatusGivenEvent>();
            OnStatusAdds = new PriorityList<StatusGivenEvent>();
            OnStatusRemoves = new PriorityList<StatusGivenEvent>();
            OnMapStatusAdds = new PriorityList<MapStatusGivenEvent>();
            OnMapStatusRemoves = new PriorityList<MapStatusGivenEvent>();

            OnMapStarts = new PriorityList<SingleCharEvent>();
            OnTurnStarts = new PriorityList<SingleCharEvent>();
            OnTurnEnds = new PriorityList<SingleCharEvent>();
            OnMapTurnEnds = new PriorityList<SingleCharEvent>();
            OnWalks = new PriorityList<SingleCharEvent>();
            OnDeaths = new PriorityList<SingleCharEvent>();

            OnRefresh = new PriorityList<RefreshEvent>();

            BeforeTryActions = new PriorityList<BattleEvent>();
            BeforeActions = new PriorityList<BattleEvent>();
            OnActions = new PriorityList<BattleEvent>();
            BeforeHittings = new PriorityList<BattleEvent>();
            BeforeBeingHits = new PriorityList<BattleEvent>();
            AfterHittings = new PriorityList<BattleEvent>();
            AfterBeingHits = new PriorityList<BattleEvent>();
            OnHitTiles = new PriorityList<BattleEvent>();
            AfterActions = new PriorityList<BattleEvent>();

            UserElementEffects = new PriorityList<ElementEffectEvent>();
            TargetElementEffects = new PriorityList<ElementEffectEvent>();
            ModifyHPs = new PriorityList<HPChangeEvent>();
            RestoreHPs = new PriorityList<HPChangeEvent>();
        }
    }


    [Serializable]
    public class ProximityData : PassiveData
    {
        /// <summary>
        /// The radius of the effect.
        /// DO NOT SET THIS OVER 5.
        /// </summary>
        public int Radius;

        /// <summary>
        /// Who it affects.
        /// </summary>
        public Alignment TargetAlignments;

        //TODO: IMPORTANT: OnEnters and OnLeaves DO NOT WORK at this time
        //public PriorityList<SingleCharEvent> OnEnters;
        //public PriorityList<SingleCharEvent> OnLeaves;

        /// <summary>
        /// Triggers before a hitbox explodes for an affected character in radius of effect.
        /// </summary>
        public PriorityList<BattleEvent> BeforeExplosions;


        public ProximityData()
        {
            Radius = -1;

            //OnEnters = new PriorityList<SingleCharEvent>();
            //OnLeaves = new PriorityList<SingleCharEvent>();

            BeforeExplosions = new PriorityList<BattleEvent>();
        }
    }


    [Serializable]
    public class ProximityPassive : PassiveData
    {
        /// <summary>
        /// The passive effect applied to entities near the character with this effect.
        /// DO NOT ADD ANYTHING TO A PROXIMITY PASSIVE'S ONREFRESH
        /// </summary>
        public ProximityData ProximityEvent;

        public ProximityPassive()
        {
            ProximityEvent = new ProximityData();
        }
    }
}
