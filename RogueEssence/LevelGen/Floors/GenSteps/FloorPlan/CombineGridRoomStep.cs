﻿using System;
using System.Collections.Generic;
using RogueElements;

namespace RogueEssence.LevelGen
{
    [Serializable]
    public class GridCombo<T> where T : class, IFloorPlanGenContext
    {
        public Loc Size;
        public RoomGen<T> GiantRoom;

        public GridCombo()
        {

        }
        public GridCombo(Loc size, RoomGen<T> giantRoom)
        {
            Size = size;
            GiantRoom = giantRoom;
        }
    }

    /// <summary>
    /// Merges single-cell rooms together into larger rooms, specified in Combos
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class CombineGridRoomStep<T> : GridPlanStep<T> where T : class, IRoomGridGenContext
    {
        //just combine simple squares for now
        public SpawnList<GridCombo<T>> Combos;
        public ComponentCollection RoomComponents { get; set; }
        public RandRange MergeRate;

        public List<BaseRoomFilter> Filters { get; set; }

        public CombineGridRoomStep()
        {
            Combos = new SpawnList<GridCombo<T>>();
            RoomComponents = new ComponentCollection();
            Filters = new List<BaseRoomFilter>();
        }

        public CombineGridRoomStep(RandRange mergeRate, List<BaseRoomFilter> filters)
        {
            MergeRate = mergeRate;
            Combos = new SpawnList<GridCombo<T>>();
            RoomComponents = new ComponentCollection();
            Filters = filters;
        }


        public override void ApplyToPath(IRandom rand, GridPlan floorPlan)
        {
            int merges = MergeRate.Pick(rand);
            for (int ii = 0; ii < merges; ii++)
            {
                //roll a merge
                GridCombo<T> combo = Combos.Pick(rand);
                List<Loc> viableLocs = new List<Loc>();
                //attempt to place it
                for (int xx = 0; xx < floorPlan.GridWidth - (combo.Size.X - 1); xx++)
                {
                    for (int yy = 0; yy < floorPlan.GridHeight - (combo.Size.Y - 1); yy++)
                    {
                        bool viable = true;
                        //check for room presence in all rooms (must be SINGLE and immutable)

                        for (int x2 = xx; x2 < xx + combo.Size.X; x2++)
                        {
                            for (int y2 = yy; y2 < yy + combo.Size.Y; y2++)
                            {
                                if (!roomViable(floorPlan, x2, y2))
                                {
                                    viable = false;
                                    break;
                                }
                            }
                            if (!viable)
                                break;
                        }
                        if (!viable)
                            continue;


                        //TODO: check for connectivity: all constituent rooms must be connected to each other somehow
                        //Check for connectivity within the whole map.

                        viableLocs.Add(new Loc(xx, yy));
                    }
                }

                if (viableLocs.Count == 0)
                    continue;

                Loc destLoc = viableLocs[rand.Next(viableLocs.Count)];
                //erase the constituent rooms
                for (int x2 = destLoc.X; x2 < destLoc.X + combo.Size.X; x2++)
                {
                    for (int y2 = destLoc.Y; y2 < destLoc.Y + combo.Size.Y; y2++)
                    {
                        floorPlan.EraseRoom(new Loc(x2, y2));
                        if (x2 > destLoc.X)
                            floorPlan.SetHall(new LocRay4(x2, y2, Dir4.Left), null, new ComponentCollection());
                        if (y2 > destLoc.Y)
                            floorPlan.SetHall(new LocRay4(x2, y2, Dir4.Up), null, new ComponentCollection());
                    }
                }

                //place the room
                floorPlan.AddRoom(new Rect(destLoc.X, destLoc.Y, combo.Size.X, combo.Size.Y), combo.GiantRoom.Copy(), this.RoomComponents.Clone(), false);
            }
        }

        private bool roomViable(GridPlan floorPlan, int xx, int yy)
        {
            //must be PRESENT, SINGLE and immutable
            GridRoomPlan plan = floorPlan.GetRoomPlan(new Loc(xx, yy));
            if (plan == null)
                return false;
            if (plan.Bounds.Area > 1)
                return false;
            if (!BaseRoomFilter.PassesAllFilters(plan, this.Filters))
                return false;

            return true;
        }


        public override string ToString()
        {
            return string.Format("{0}[{1}]: Amount:{2}", this.GetType().Name, Combos.Count, MergeRate.ToString());
        }
    }
}
