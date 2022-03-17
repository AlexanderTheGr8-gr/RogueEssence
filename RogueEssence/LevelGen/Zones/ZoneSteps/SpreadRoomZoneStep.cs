﻿using System;
using System.Collections.Generic;
using RogueElements;

namespace RogueEssence.LevelGen
{
    [Serializable]
    public class SpreadRoomZoneStep : ZoneStep
    {
        public SpreadPlanBase SpreadPlan;
        //this is heavily hardcoded
        public SpawnList<RoomGenOption> Spawns;
        public Priority PriorityGrid;
        public Priority PriorityList;

        public SpreadRoomZoneStep()
        {
            Spawns = new SpawnList<RoomGenOption>();
        }
        public SpreadRoomZoneStep(Priority priorityGrid, Priority priorityList) : this()
        {
            PriorityGrid = priorityGrid;
            PriorityList = priorityList;
        }

        public SpreadRoomZoneStep(Priority priorityGrid, Priority priorityList, SpreadPlanBase plan) : this(priorityGrid, priorityList)
        {
            SpreadPlan = plan;
        }

        protected SpreadRoomZoneStep(SpreadRoomZoneStep other, ulong seed) : this()
        {
            Spawns = (SpawnList<RoomGenOption>)other.Spawns.CopyState();
            PriorityGrid = other.PriorityGrid;
            PriorityList = other.PriorityList;
            SpreadPlan = other.SpreadPlan.Instantiate(seed);
        }
        public override ZoneStep Instantiate(ulong seed) { return new SpreadRoomZoneStep(this, seed); }

        public override void Apply(ZoneGenContext zoneContext, IGenContext context, StablePriorityQueue<Priority, IGenStep> queue)
        {
            //find the first postproc that is a GridRoom postproc and add this to its special rooms
            //NOTE: if a room-based generator is not found as the generation step, it will just skip this floor but treat it as though it was placed.
            foreach(int floorId in SpreadPlan.DropPoints)
            {
                if (floorId != zoneContext.CurrentID)
                    continue;
                //TODO: allow arbitrary components to be added
                RoomGenOption genDuo = Spawns.Pick(context.Rand);
                SetGridSpecialRoomStep<MapGenContext> specialStep = new SetGridSpecialRoomStep<MapGenContext>();
                SetSpecialRoomStep<ListMapGenContext> listSpecialStep = new SetSpecialRoomStep<ListMapGenContext>();

                specialStep.Filters = genDuo.Filters;
                if (specialStep.CanApply(context))
                {
                    specialStep.Rooms = new PresetPicker<RoomGen<MapGenContext>>(genDuo.GridOption);
                    specialStep.RoomComponents.Set(new ImmutableRoom());
                    queue.Enqueue(PriorityGrid, specialStep);
                }
                else if (listSpecialStep.CanApply(context))
                {
                    listSpecialStep.Rooms = new PresetPicker<RoomGen<ListMapGenContext>>(genDuo.ListOption);
                    listSpecialStep.RoomComponents.Set(new ImmutableRoom());
                    PresetPicker<PermissiveRoomGen<ListMapGenContext>> picker = new PresetPicker<PermissiveRoomGen<ListMapGenContext>>();
                    picker.ToSpawn = new RoomGenAngledHall<ListMapGenContext>(0);
                    listSpecialStep.Halls = picker;
                    queue.Enqueue(PriorityList, listSpecialStep);
                }
            }
        }
    }

    [Serializable]
    public class RoomGenOption
    {
        public RoomGen<MapGenContext> GridOption;
        public RoomGen<ListMapGenContext> ListOption;

        public List<BaseRoomFilter> Filters;

        public RoomGenOption(RoomGen<MapGenContext> gridOption, RoomGen<ListMapGenContext> listOption, List<BaseRoomFilter> filters)
        {
            GridOption = gridOption;
            ListOption = listOption;
            Filters = filters;
        }
    }
}
