﻿using System;
using System.Collections;
using System.Collections.Generic;
using RogueEssence.Data;
using System.Runtime.Serialization;
using RogueElements;
using RogueEssence.LevelGen;

namespace RogueEssence.Dungeon
{
    [Serializable]
    public abstract class Team
    {
        public List<Character> Players;
        public List<Character> Guests;

        public int LeaderIndex;

        /// <summary>
        /// If set to true, will attack/be attacked by Foe faction when in Ally faction.
        /// </summary>
        public bool FoeConflict;

        private List<InvItem> inventory;

        public Team()
        {
            Players = new List<Character>();
            Guests = new List<Character>();
            inventory = new List<InvItem>();
        }
        
        public Character Leader { get { return Players[LeaderIndex]; } }

        public int MemberGuestCount { get { return Players.Count + Guests.Count; } }

        public IEnumerable<Character> IterateByRank()
        {
            foreach(Character character in IterateMainByRank())
                yield return character;
            foreach (Character character in Guests)
                yield return character;
        }

        public IEnumerable<Character> IterateMainByRank()
        {
            yield return Leader;
            foreach (Character character in Players)
            {
                if (character != Leader)
                    yield return character;
            }
        }

        public IEnumerable<Character> EnumerateChars()
        {
            foreach (Character chara in Players)
                yield return chara;
            foreach (Character chara in Guests)
                yield return chara;
        }


        public int GetInvCount()
        {
            return inventory.Count;
        }

        public InvItem GetInv(int slot)
        {
            return inventory[slot];
        }

        public IEnumerable<InvItem> EnumerateInv()
        {
            foreach (InvItem item in inventory)
                yield return item;
        }

        public void AddToInv(InvItem invItem, bool skipCheck = false)
        {
            inventory.Add(invItem);
            if (skipCheck)
                return;
            UpdateInv(null, invItem);
        }
        public void RemoveFromInv(int index, bool skipCheck = false)
        {
            InvItem invItem = inventory[index];
            inventory.RemoveAt(index);
            if (skipCheck)
                return;
            UpdateInv(invItem, null);
        }
        public void UpdateInv(InvItem oldItem, InvItem newItem)
        {
            bool update = false;
            if (oldItem != null)
            {
                ItemData itemEntry = DataManager.Instance.GetItem(oldItem.ID);
                if (itemEntry.BagEffect)
                    update = true;
            }
            if (newItem != null)
            {
                ItemData itemEntry = DataManager.Instance.GetItem(newItem.ID);
                if (itemEntry.BagEffect)
                    update = true;
            }
            if (oldItem == null && newItem == null)
                update = true;
            if (update)
            {
                foreach (Character chara in Players)
                    chara.RefreshTraits();
                foreach (Character chara in Guests)
                    chara.RefreshTraits();
            }
        }

        public void SortItems()
        {
            List<InvItem> newInv = new List<InvItem>();
            //for each inv item
            for (int kk = 0; kk < inventory.Count; kk++)
            {
                //find its new place
                for (int ii = newInv.Count; ii >= 0; ii--)
                {
                    if (ii == 0 || SucceedsInvItem(inventory[kk], newInv[ii - 1]))
                    {
                        newInv.Insert(ii, inventory[kk]);
                        break;
                    }
                }
            }
            inventory = newInv;
        }

        private bool SucceedsInvItem(InvItem inv1, InvItem inv2)
        {
            ItemData entry1 = DataManager.Instance.GetItem(inv1.ID);
            ItemData entry2 = DataManager.Instance.GetItem(inv2.ID);
            if (entry1.UsageType > entry2.UsageType)
                return true;
            else if (entry1.UsageType < entry2.UsageType)
                return false;
            return (inv1.ID >= inv2.ID);
        }

        public int GetInvValue()
        {
            int invValue = 0;
            foreach (InvItem item in inventory)
                invValue += item.GetSellValue();
            return invValue;
        }


        public CharIndex GetCharIndex(Character character)
        {
            for (int jj = 0; jj < Players.Count; jj++)
            {
                if (character == Players[jj])
                    return new CharIndex(Faction.None, -1, false, jj);
            }
            for (int jj = 0; jj < Guests.Count; jj++)
            {
                if (character == Guests[jj])
                    return new CharIndex(Faction.None, -1, true, jj);
            }
            return CharIndex.Invalid;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            ReconnectTeamReference();
        }

        protected virtual void ReconnectTeamReference()
        {
            //reconnect Players' references
            foreach (Character player in Players)
                player.MemberTeam = this;
            foreach (Character player in Guests)
                player.MemberTeam = this;
        }

        public virtual void SaveLua()
        {
            foreach (Character player in Players)
                player.SaveLua();
            foreach (Character player in Guests)
                player.SaveLua();
        }

        public virtual void LoadLua()
        {
            foreach (Character player in Players)
                player.LoadLua();
            foreach (Character player in Guests)
                player.LoadLua();
        }
    }

    [Serializable]
    public class MonsterTeam : Team
    {
        public bool Unrecruitable;
    }

    [Serializable]
    public class ExplorerTeam : Team
    {
        public const int MAX_TEAM_SLOTS = 4;

        public int MaxInv;

        public string Name;
        public List<Character> Assembly;
        public int[] Storage;
        public List<InvItem> BoxStorage;
        public int Bank;
        public int Money;
        public int Rank { get; private set; }
        public int Fame;
        public int RankExtra;

        public ExplorerTeam()
        {
            Name = "";
            Assembly = new List<Character>();
            BoxStorage = new List<InvItem>();
            Storage = new int[10000];//TODO: remove this magic number and make it an adjustable value
        }

        public void SetRank(int rank)
        {
            Rank = rank;
            MaxInv = DataManager.Instance.GetRank(rank).BagSize;
        }

        public int GetMaxInvSlots(Zone zone)
        {
            int slots = MaxInv;
            if (zone != null && zone.BagSize > -1 && zone.BagSize < slots)
                slots = zone.BagSize;
            foreach (Character player in Players)
            {
                if (player.EquippedItem.ID > -1)
                    slots--;
            }
            return slots;
        }

        public int GetMaxTeam(Zone zone)
        {
            int slots = MAX_TEAM_SLOTS;
            if (zone != null && zone.TeamSize > -1 && zone.TeamSize < slots)
                slots = zone.TeamSize;
            return slots;
        }

        public string GetReferenceName()
        {
            if (Name != "")
                return Name;
            else
                return Players[0].BaseName;
        }

        public string GetDisplayName()
        {
            string name = Players[0].BaseName;
            if (Name != "")
                name = Name;
            return String.Format("[color=#FFA5FF]{0}[color]", name);
        }


        public List<InvItem> TakeItems(List<int> indices, bool remove = true)
        {
            List<int> removedBoxSlots = new List<int>();
            List<InvItem> invToTake = new List<InvItem>();
            for (int ii = 0; ii < indices.Count; ii++)
            {
                int index = indices[ii];
                if (index < DataManager.Instance.DataIndices[DataManager.DataType.Item].Count)
                {
                    ItemData entry = DataManager.Instance.GetItem(index);
                    if (entry.MaxStack > 1)
                    {
                        int existingStack = -1;
                        for (int jj = 0; jj < invToTake.Count; jj++)
                        {
                            if (invToTake[jj].ID == index && invToTake[jj].HiddenValue < entry.MaxStack)
                            {
                                existingStack = jj;
                                break;
                            }
                        }
                        if (existingStack > -1)
                            invToTake[existingStack].HiddenValue++;
                        else
                            invToTake.Add(new InvItem(index, false, 1));
                    }
                    else
                        invToTake.Add(new InvItem(index));
                    if (remove)
                        Storage[index]--;
                }
                else
                {
                    invToTake.Add(BoxStorage[index - DataManager.Instance.DataIndices[DataManager.DataType.Item].Count]);
                    removedBoxSlots.Add(index - DataManager.Instance.DataIndices[DataManager.DataType.Item].Count);
                }
            }
            removedBoxSlots.Sort();
            if (remove)
            {
                for (int ii = removedBoxSlots.Count - 1; ii >= 0; ii--)
                    BoxStorage.RemoveAt(removedBoxSlots[ii]);
            }

            return invToTake;
        }

        public void StoreItems(List<InvItem> invToStore)
        {
            foreach(InvItem item in invToStore)
            {
                ItemData entry = DataManager.Instance.GetItem(item.ID);
                if (entry.MaxStack > 1)
                    Storage[item.ID] += item.HiddenValue;
                else if (entry.UsageType == ItemData.UseType.Box)
                    BoxStorage.Add(item);
                else
                    Storage[item.ID]++;
            }
        }

        public int GetTotalScore()
        {
            return GetInvValue() + Money + Bank;
        }

        public void AddToSortedAssembly(Character chara)
        {
            int idx = 0;
            while (idx < Assembly.Count)
            {
                if (!Assembly[idx].IsFavorite)
                    break;
                idx++;
            }
            Assembly.Insert(idx, chara);
        }

        public Character CreatePlayer(IRandom rand, MonsterID form, int level, int intrinsic, int personality)
        {
            MonsterID formData = form;
            MonsterData dex = DataManager.Instance.GetMonster(formData.Species);

            CharData character = new CharData();
            character.BaseForm = formData;
            character.Level = level;

            BaseMonsterForm formEntry = dex.Forms[formData.Form];

            List<int> final_skills = formEntry.RollLatestSkills(character.Level, new List<int>());
            for(int ii = 0; ii < final_skills.Count; ii++)
                character.BaseSkills[ii] = new SlotSkill(final_skills[ii]);

            if (form.Gender == Gender.Unknown)
                character.BaseForm.Gender = dex.Forms[formData.Form].RollGender(rand);
            
            if (intrinsic == -1)
                character.BaseIntrinsics[0] = formEntry.RollIntrinsic(rand, 2);
            else
                character.BaseIntrinsics[0] = intrinsic;

            if (personality == -1)
                character.Discriminator = rand.Next();
            else
                character.Discriminator = personality;


            character.OriginalUUID = DataManager.Instance.Save.UUID;
            character.OriginalTeam = DataManager.Instance.Save.ActiveTeam.Name;
            character.MetAt = Text.FormatKey("MET_AT_START");
            character.MetLoc = ZoneLoc.Invalid;

            return CreatePlayer(character);
        }



        public Character CreatePlayer(CharData character)
        {
            Character player = new Character(character, this);
            foreach (BackReference<Skill> skill in player.Skills)
            {
                if (skill.Element.SkillNum > -1)
                {
                    SkillData entry = DataManager.Instance.GetSkill(skill.Element.SkillNum);
                    skill.Element.Enabled = (entry.Data.Category == BattleData.SkillCategory.Physical || entry.Data.Category == BattleData.SkillCategory.Magical);
                }
            }
            AITactic tactic = DataManager.Instance.GetAITactic(0);
            player.Tactic = new AITactic(tactic);

            return player;
        }


        protected override void ReconnectTeamReference()
        {
            base.ReconnectTeamReference();
            foreach (Character player in Assembly)
                player.MemberTeam = this;
        }

        public override void SaveLua()
        {
            base.SaveLua();
            foreach (Character player in Assembly)
                player.SaveLua();
        }

        public override void LoadLua()
        {
            base.LoadLua();
            foreach (Character player in Assembly)
                player.LoadLua();
        }
    }

    [Serializable]
    public struct TempCharBackRef
    {
        public bool Assembly;
        public int Index;

        public TempCharBackRef(bool assembly, int index)
        {
            Assembly = assembly;
            Index = index;
        }
    }
}
