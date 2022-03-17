﻿using System;
using RogueElements;
using System.Collections.Generic;
using RogueEssence.Dev;

namespace RogueEssence.LevelGen
{
    [Serializable]
    public class TeamMemberSpawn
    {
        public enum MemberRole
        {
            Normal, // Can be put in teams of any size at any quantity.
            Support, // Only one can spawn in a team.  Team size > 1.
            Leader, // Only one can spawn in a team.  Any size team.
            Loner // Only one can spawn in a team.  Team of 1.
        }

        [SubGroup]
        public MobSpawn Spawn;

        public MemberRole Role;

        public TeamMemberSpawn()
        {

        }

        public TeamMemberSpawn(MobSpawn spawn, MemberRole role)
        {
            Spawn = spawn;
            Role = role;
        }

        public TeamMemberSpawn(TeamMemberSpawn other)
        {
            Spawn = other.Spawn.Copy();
            Role = other.Role;
        }

        public override string ToString()
        {
            return Spawn.ToString();
        }
    }

    [Serializable]
    public class PoolTeamSpawner : TeamSpawner
    {
        /// <summary>
        /// Normal spawns.  Can be put in teams of any size at any quantity.
        /// </summary>
        [SubGroup]
        public SpawnList<TeamMemberSpawn> Spawns;

        [SubGroup]
        public SpawnList<int> TeamSizes;

        public PoolTeamSpawner()
        {
            Spawns = new SpawnList<TeamMemberSpawn>();
            TeamSizes = new SpawnList<int>();
        }
        protected PoolTeamSpawner(PoolTeamSpawner other)
        {
            Spawns = new SpawnList<TeamMemberSpawn>();
            for(int ii = 0; ii < other.Spawns.Count; ii++)
                Spawns.Add(new TeamMemberSpawn(other.Spawns.GetSpawn(ii)), other.Spawns.GetSpawnRate(ii));
            TeamSizes = new SpawnList<int>();
            for (int ii = 0; ii < other.TeamSizes.Count; ii++)
                TeamSizes.Add(other.TeamSizes.GetSpawn(ii), other.TeamSizes.GetSpawnRate(ii));
        }
        public override TeamSpawner Clone() { return new PoolTeamSpawner(this); }

        public override SpawnList<MobSpawn> GetPossibleSpawns()
        {
            SpawnList<MobSpawn> spawnerList = new SpawnList<MobSpawn>();

            for (int ii = 0; ii < Spawns.Count; ii++)
                spawnerList.Add(Spawns.GetSpawn(ii).Spawn, Spawns.GetSpawnRate(ii));

            return spawnerList;
        }
        public override List<MobSpawn> ChooseSpawns(IRandom rand)
        {
            List<MobSpawn> chosenSpawns = new List<MobSpawn>();

            if (!TeamSizes.CanPick)
                return chosenSpawns;
            int teamSize = TeamSizes.Pick(rand);

            bool selectedLeader = false;
            bool selectedNonSupport = false;

            //pick first team member
            SpawnList<TeamMemberSpawn> eligibleSpawns = new SpawnList<TeamMemberSpawn>();
            for (int ii = 0; ii < Spawns.Count; ii++)
            {
                TeamMemberSpawn spawn = Spawns.GetSpawn(ii);
                if (!spawn.Spawn.CanSpawn())
                    continue;

                bool add = false;
                switch (spawn.Role)
                {
                    case TeamMemberSpawn.MemberRole.Normal:
                    case TeamMemberSpawn.MemberRole.Leader:
                        add = true;
                        break;
                    case TeamMemberSpawn.MemberRole.Support:
                        add = (teamSize > 1);
                        break;
                    case TeamMemberSpawn.MemberRole.Loner:
                        add = (teamSize == 1);
                        break;
                }
                if (add)
                    eligibleSpawns.Add(spawn, Spawns.GetSpawnRate(ii));
            }

            if (!eligibleSpawns.CanPick)
                return chosenSpawns;
            TeamMemberSpawn chosenSpawn = eligibleSpawns.Pick(rand);
            if (chosenSpawn.Role == TeamMemberSpawn.MemberRole.Leader)
                selectedLeader = true;
            if (chosenSpawn.Role != TeamMemberSpawn.MemberRole.Support)
                selectedNonSupport = true;

            chosenSpawns.Add(chosenSpawn.Spawn);

            //pick remaining team members
            for (int jj = 1; jj < teamSize; jj++)
            {
                eligibleSpawns.Clear();

                for (int ii = 0; ii < Spawns.Count; ii++)
                {
                    TeamMemberSpawn spawn = Spawns.GetSpawn(ii);
                    if (!spawn.Spawn.CanSpawn())
                        continue;
                    bool add = false;
                    switch (spawn.Role)
                    {
                        case TeamMemberSpawn.MemberRole.Normal:
                            add = true;
                            break;
                        case TeamMemberSpawn.MemberRole.Leader:
                            add = !selectedLeader;
                            break;
                        case TeamMemberSpawn.MemberRole.Support:
                            add = selectedNonSupport;
                            break;
                    }
                    if (add)
                        eligibleSpawns.Add(spawn, Spawns.GetSpawnRate(ii));
                }

                if (!eligibleSpawns.CanPick)
                    return chosenSpawns;
                chosenSpawn = eligibleSpawns.Pick(rand);
                if (chosenSpawn.Role == TeamMemberSpawn.MemberRole.Leader)
                    selectedLeader = true;
                if (chosenSpawn.Role != TeamMemberSpawn.MemberRole.Support)
                    selectedNonSupport = true;

                chosenSpawns.Add(chosenSpawn.Spawn);
            }

            return chosenSpawns;
        }
    }
}
