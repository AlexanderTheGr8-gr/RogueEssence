﻿using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence.Content;
using RogueEssence.Dev;
using System.Runtime.Serialization;

namespace RogueEssence.Data
{

    [Serializable]
    public class BattleData : GameEventOwner
    {

        public enum SkillCategory
        {
            None = 0,
            Physical,
            Magical,
            Status
        }

        public override GameEventPriority.EventCause GetEventCause()
        {
            return GameEventPriority.EventCause.Skill;
        }

        public override int GetID() { return ID; }
        //TODO: later on, make child classes for skills, traps, item usages and throws?
        public override string GetDisplayName()
        {
            return DataManager.Instance.GetSkill(ID).GetIconName();
        }

        public override string ToString()
        {
            ElementData element = DataManager.Instance.GetElement(Element);
            string type = Text.FormatKey("MENU_SKILLS_ELEMENT", element.Name.ToLocal());
            string category = Text.FormatKey("MENU_SKILLS_CATEGORY", Category.ToLocal());
            BasePowerState powerState = SkillStates.GetWithDefault<BasePowerState>();
            string power = Text.FormatKey("MENU_SKILLS_POWER", (powerState != null ? powerState.Power.ToString() : "---"));
            string acc = Text.FormatKey("MENU_SKILLS_HIT_RATE", (HitRate > -1 ? HitRate + "%" : "---"));
            return type + ",  " + category + "\n" + power + ", " + acc;
        }

        [NonSerialized]
        public int ID;

        [DataType(0, DataManager.DataType.Element, false)]
        public int Element;
        public SkillCategory Category;

        public int HitRate;

        public StateCollection<SkillState> SkillStates;
        public PriorityList<BattleEvent> BeforeTryActions;
        public PriorityList<BattleEvent> BeforeActions;
        public PriorityList<BattleEvent> OnActions;
        public PriorityList<BattleEvent> BeforeExplosions;
        public PriorityList<BattleEvent> BeforeHits;
        public PriorityList<BattleEvent> OnHits;
        public PriorityList<BattleEvent> OnHitTiles;
        public PriorityList<BattleEvent> AfterActions;
        public PriorityList<ElementEffectEvent> ElementEffects;


        public List<BattleFX> IntroFX;

        public BattleFX HitFX;

        public CharAnimData HitCharAction;

        public BattleData()
        {
            ID = -1;

            HitRate = -1;

            SkillStates = new StateCollection<SkillState>();

            BeforeTryActions = new PriorityList<BattleEvent>();
            BeforeActions = new PriorityList<BattleEvent>();
            OnActions = new PriorityList<BattleEvent>();
            BeforeExplosions = new PriorityList<BattleEvent>();
            BeforeHits = new PriorityList<BattleEvent>();
            OnHits = new PriorityList<BattleEvent>();
            OnHitTiles = new PriorityList<BattleEvent>();
            AfterActions = new PriorityList<BattleEvent>();
            ElementEffects = new PriorityList<ElementEffectEvent>();

            IntroFX = new List<BattleFX>();
            HitFX = new BattleFX();
            HitCharAction = new CharAnimProcess();
        }

        public BattleData(BattleData other)
        {
            ID = other.ID;

            Element = other.Element;
            Category = other.Category;

            HitRate = other.HitRate;

            SkillStates = other.SkillStates.Clone();

            copyEventList<BattleEvent>(ref BeforeTryActions, other.BeforeTryActions, true);
            copyEventList<BattleEvent>(ref BeforeActions, other.BeforeActions, true);
            copyEventList<BattleEvent>(ref OnActions, other.OnActions, true);
            copyEventList<BattleEvent>(ref BeforeExplosions, other.BeforeExplosions, true);
            copyEventList<BattleEvent>(ref BeforeHits, other.BeforeHits, true);
            copyEventList<BattleEvent>(ref OnHits, other.OnHits, true);
            copyEventList<BattleEvent>(ref OnHitTiles, other.OnHitTiles, true);
            copyEventList<BattleEvent>(ref AfterActions, other.AfterActions, true);
            copyEventList<ElementEffectEvent>(ref ElementEffects, other.ElementEffects, true);

            IntroFX = new List<BattleFX>();
            foreach (BattleFX fx in other.IntroFX)
                IntroFX.Add(new BattleFX(fx));
            HitFX = new BattleFX(other.HitFX);
            HitCharAction = other.HitCharAction.Clone();
        }

        protected void copyEventList<T>(ref PriorityList<T> effectsTo, PriorityList<T> effectsFrom, bool init) where T : GameEvent
        {
            if (init)
                effectsTo = new PriorityList<T>();
            foreach (Priority priority in effectsFrom.GetPriorities())
            {
                foreach(T effect in effectsFrom.GetItems(priority))
                    effectsTo.Add(priority, (T)effect.Clone());
            }
        }

        public IEnumerator<YieldInstruction> Hit(BattleContext context)
        {
            DungeonScene.EventEnqueueFunction<BattleEvent> function = (StablePriorityQueue<GameEventPriority, EventQueueElement<BattleEvent>> queue, Priority maxPriority, ref Priority nextPriority) =>
            {
                //include the universal effect here
                DataManager.Instance.UniversalEvent.AddEventsToQueue(queue, maxPriority, ref nextPriority, DataManager.Instance.UniversalEvent.OnHits, null);
                ZoneManager.Instance.CurrentMap.MapEffect.AddEventsToQueue(queue, maxPriority, ref nextPriority, ZoneManager.Instance.CurrentMap.MapEffect.OnHits, null);
                AddEventsToQueue<BattleEvent>(queue, maxPriority, ref nextPriority, OnHits, null);
            };
            foreach (EventQueueElement<BattleEvent> effect in DungeonScene.IterateEvents<BattleEvent>(function))
                yield return CoroutineManager.Instance.StartCoroutine(effect.Event.Apply(effect.Owner, effect.OwnerChar, context));
        }
    }
}
