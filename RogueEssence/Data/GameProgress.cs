﻿using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence.Ground;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using RogueEssence.Content;
using RogueEssence.Script;
using RogueEssence.Menu;
using Newtonsoft.Json;

namespace RogueEssence.Data
{
    public enum TimeOfDay
    {
        Unknown = -1,
        Day,
        Dusk,
        Night,
        Dawn
    }
    [Serializable]
    public class ProfilePic
    {
        public MonsterID ID;
        public int Emote;

        public ProfilePic(MonsterID id, int emote)
        {
            ID = id;
            Emote = emote;
        }
    }
    [Serializable]
    public class RescueState
    {
        public SOSMail SOS;
        public bool Rescuing;

        public RescueState(SOSMail sos, bool rescuing)
        {
            SOS = sos;
            Rescuing = rescuing;
        }
    }

    [Serializable]
    public abstract class GameProgress
    {
        public const int MAJOR_TIME_DUR = 600;
        public const int MINOR_TIME_DUR = 300;

        public int TimeCycle;
        public TimeOfDay Time
        {
            get
            {
                if (TimeCycle < MAJOR_TIME_DUR)
                    return TimeOfDay.Day;
                else if (TimeCycle < MAJOR_TIME_DUR + MINOR_TIME_DUR)
                    return TimeOfDay.Dusk;
                else if (TimeCycle < MAJOR_TIME_DUR * 2 + MINOR_TIME_DUR)
                    return TimeOfDay.Night;
                else if (TimeCycle < (MAJOR_TIME_DUR + MINOR_TIME_DUR) * 2)
                    return TimeOfDay.Dawn;
                return TimeOfDay.Unknown;
            }
        }

        public enum ResultType
        {
            Unknown = -1,
            Downed,
            Failed,
            Cleared,
            Escaped,
            TimedOut,
            GaveUp,
            Rescue
        }
        public enum DungeonStakes
        {
            None,//progress in the dungeon is not recorded
            Progress,//progress in the dungeon is recorded
            Risk//progress in the dungeon is recorded, losing gives a penalty
        }
        public enum UnlockState
        {
            None,
            Discovered,
            Completed
        }

        public Version GameVersion;
        public ModHeader Quest;
        public List<ModHeader> Mods;

        public ExplorerTeam ActiveTeam;
        public ReRandom Rand;

        public List<UnlockState> Dex;
        public List<bool> RogueStarters;
        public List<UnlockState> DungeonUnlocks;

        //TODO: set dungeon unlocks and event flags to save variables

        public string StartDate;
        public string UUID;
        public ProfilePic[] ProfilePics;

        public bool NoSwitching;
        public RescueState Rescue;

        public ZoneLoc NextDest;

        public bool TeamMode;

        public int TotalTurns;
        public int TotalEXP;
        public int StartLevel;
        public int RescuesLeft;

        //these values update and never clear
        public string EndDate;
        public string Location;
        public List<string> Trail;
        public DungeonStakes Stakes;
        public bool MidAdventure;
        public ResultType Outcome;
        public string GeneratedAOK;
        public bool AllowRescue;

        public bool CutsceneMode;
        public string Song;//TODO: save the currently playing song


        //Added this for storing the serialized ScriptVar table
        [JsonConverter(typeof(Dev.ScriptVarsConverter))]
        public LuaTableContainer ScriptVars;

        public GameProgress()
        {
            GameVersion = new Version();
            Quest = ModHeader.Invalid;
            Mods = new List<ModHeader>();

            ActiveTeam = new ExplorerTeam();

            Dex = new List<UnlockState>();
            RogueStarters = new List<bool>();
            DungeonUnlocks = new List<UnlockState>();

            NextDest = ZoneLoc.Invalid;

            StartLevel = -1;
            StartDate = "";
            UUID = "";
            ProfilePics = new ProfilePic[0];
            EndDate = "";
            Location = "";
            Trail = new List<string>();
            Outcome = ResultType.Unknown;
        }

        public GameProgress(ulong seed, string uuid) : this()
        {
            Rand = new ReRandom(seed);
            UUID = uuid;
        }

        public ContactInfo CreateContactInfo()
        {
            ContactInfo info = new ContactInfo(UUID);
            info.Data.TeamName = ActiveTeam.Name;
            info.Data.Rank = ActiveTeam.Rank;
            info.Data.RankStars = ActiveTeam.RankExtra;

            info.Data.TeamProfile = ProfilePics;

            return info;
        }

        public void UpdateTeamProfile(bool standard)
        {
            ulong curSeed = Rand.FirstSeed;
            ulong totalEmotes = (ulong)GraphicsManager.Emotions.Count;
            List<ProfilePic> teamProfile = new List<ProfilePic>();
            foreach (Character chara in ActiveTeam.Players)
            {
                int piece = (int)((curSeed ^ (ulong)chara.Discriminator) % totalEmotes);
                if (standard)
                    piece = 0;
                teamProfile.Add(new ProfilePic(chara.BaseForm, piece));
            }
            ProfilePics = teamProfile.ToArray();
        }

        public UnlockState GetMonsterUnlock(int index)
        {
            return CollectionExt.GetExtendList(Dex, index);
        }

        public int GetTotalMonsterUnlock(UnlockState state)
        {
            int total = 0;
            for (int ii = 0; ii < Dex.Count; ii++)
            {
                if (Dex[ii] == state)
                    total++;
            }
            return total;
        }

        public virtual void RegisterMonster(int index)
        {
            CollectionExt.AssignExtendList(Dex, index, UnlockState.Completed);
        }

        public virtual void SeenMonster(int index)
        {
            if (CollectionExt.GetExtendList(Dex, index) == UnlockState.None)
                CollectionExt.AssignExtendList(Dex, index, UnlockState.Discovered);
        }

        public virtual void RogueUnlockMonster(int index)
        {
            CollectionExt.AssignExtendList(RogueStarters, index, true);
        }
        public bool GetRogueUnlock(int index)
        {
            return CollectionExt.GetExtendList(RogueStarters, index);
        }
        public UnlockState GetDungeonUnlock(int index)
        {
            return CollectionExt.GetExtendList(DungeonUnlocks, index);
        }
        public void UnlockDungeon(int index)
        {
            if (CollectionExt.GetExtendList(DungeonUnlocks, index) == UnlockState.None)
                CollectionExt.AssignExtendList(DungeonUnlocks, index, UnlockState.Discovered);
        }
        public void CompleteDungeon(int index)
        {
            CollectionExt.AssignExtendList(DungeonUnlocks, index, UnlockState.Completed);
        }

        public abstract IEnumerator<YieldInstruction> BeginGame(int zoneID, ulong seed, DungeonStakes stakes, bool recorded, bool noRestrict);
        public abstract IEnumerator<YieldInstruction> EndGame(ResultType result, ZoneLoc nextArea, bool display, bool fanfare);


        public IEnumerator<YieldInstruction> RestrictTeam(ZoneData zone, bool silent)
        {
            int teamSize = ExplorerTeam.MAX_TEAM_SLOTS;
            if (zone.TeamSize > -1)
                teamSize = zone.TeamSize;
            if (zone.TeamRestrict)
                teamSize = 1;

            List<string> teamRestrictions = new List<string>();
            while (ActiveTeam.Players.Count > teamSize)
            {
                int sendHomeIndex = ActiveTeam.Players.Count - 1;
                if (sendHomeIndex == ActiveTeam.LeaderIndex)
                    sendHomeIndex--;
                teamRestrictions.Add(ActiveTeam.Players[sendHomeIndex].GetDisplayName(true));
                if (GameManager.Instance.CurrentScene == GroundScene.Instance)
                    GroundScene.Instance.SilentSendHome(sendHomeIndex);
                else if (GameManager.Instance.CurrentScene == DungeonScene.Instance)
                    DungeonScene.Instance.SilentSendHome(sendHomeIndex);
            }

            if (!silent)
            {
                if (teamRestrictions.Count > 1)
                {
                    string compositeList = Text.BuildList(teamRestrictions.ToArray());
                yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.SetDialogue(Text.FormatKey("MSG_TEAM_SENT_HOME_PLURAL", compositeList)));
                }
                else if (teamRestrictions.Count == 1)
                yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.SetDialogue(Text.FormatKey("MSG_TEAM_SENT_HOME", teamRestrictions[0])));
            }
            
            List<string> bagRestrictions = new List<string>();
            if (zone.MoneyRestrict && ActiveTeam.Money > 0)
            {
                ActiveTeam.Bank += ActiveTeam.Money;
                ActiveTeam.Money = 0;
                bagRestrictions.Add(Text.FormatKey("DLG_RESTRICT_MONEY"));
            }

            if (zone.BagRestrict > -1)
            {
                bool removedItems = false;
                int heldSlots = 0;
                foreach (Character player in ActiveTeam.Players)
                {
                    if (player.EquippedItem.ID > -1)
                        heldSlots++;
                }
                List<InvItem> itemsToStore = new List<InvItem>();
                while (ActiveTeam.GetInvCount() + heldSlots > zone.BagRestrict && ActiveTeam.GetInvCount() > 0)
                {
                    removedItems = true;
                    itemsToStore.Add(ActiveTeam.GetInv(ActiveTeam.GetInvCount() - 1));
                    
                    ActiveTeam.RemoveFromInv(ActiveTeam.GetInvCount() - 1);
                }
                while (ActiveTeam.GetInvCount() + heldSlots > zone.BagRestrict)
                {
                    foreach (Character player in ActiveTeam.Players)
                    {
                        if (player.EquippedItem.ID > -1)
                        {
                            removedItems = true;
                            itemsToStore.Add(player.EquippedItem);
                            player.DequipItem();
                            heldSlots--;
                            break;
                        }
                    }
                }
                ActiveTeam.StoreItems(itemsToStore);
                if (!silent && removedItems)
                {
                    if (zone.BagRestrict > 0)
                        bagRestrictions.Add(Text.FormatKey("DLG_RESTRICT_ITEM_SLOT", zone.BagRestrict));
                    else
                        bagRestrictions.Add(Text.FormatKey("DLG_RESTRICT_ITEM_ALL"));
                }
            }
            if (bagRestrictions.Count > 0 && !silent)
            {
                string compositeList = Text.BuildList(bagRestrictions.ToArray());
                string finalMsg = Text.FormatKey("DLG_RESTRICT_BAG", (compositeList[0].ToString()).ToUpper() + compositeList.Substring(1));
                yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.SetDialogue(finalMsg));
            }
        }

        public void ClearDefeatDest()
        {
            for (int ii = 0; ii < ActiveTeam.Players.Count; ii++)
            {
                ActiveTeam.Players[ii].DefeatAt = "";
                ActiveTeam.Players[ii].DefeatLoc = ZoneLoc.Invalid;
            }
            for (int ii = 0; ii < ActiveTeam.Guests.Count; ii++)
            {
                ActiveTeam.Guests[ii].DefeatAt = "";
                ActiveTeam.Guests[ii].DefeatLoc = ZoneLoc.Invalid;
            }
            for (int ii = 0; ii < ActiveTeam.Assembly.Count; ii++)
            {
                ActiveTeam.Assembly[ii].DefeatAt = "";
                ActiveTeam.Assembly[ii].DefeatLoc = ZoneLoc.Invalid;
            }
        }

        public IEnumerator<YieldInstruction> RestrictLevel(ZoneData zone, bool silent)
        {
            if (zone.LevelCap)
            {
                StartLevel = zone.Level;
                for (int ii = 0; ii < ActiveTeam.Players.Count; ii++)
                {
                    RestrictCharLevel(ActiveTeam.Players[ii], zone.Level, true);
                    ActiveTeam.Players[ii].BackRef = new TempCharBackRef(false, ii);
                }
                for (int ii = 0; ii < ActiveTeam.Guests.Count; ii++)
                {
                    RestrictCharLevel(ActiveTeam.Guests[ii], zone.Level, true);
                    //no backref for guests
                }
                for (int ii = 0; ii < ActiveTeam.Assembly.Count; ii++)
                {
                    RestrictCharLevel(ActiveTeam.Assembly[ii], zone.Level, true);
                    ActiveTeam.Assembly[ii].BackRef = new TempCharBackRef(true, ii);
                }
                if (!silent)
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.SetDialogue(Text.FormatKey("DLG_RESTRICT_LEVEL", StartLevel)));
            }
        }

        public static void RestrictCharLevel(Character character, int level, bool capOnly)
        {
            //set level
            if (capOnly)
                character.Level = Math.Min(level, character.Level);
            else
                character.Level = level;

            if (character.Level == level)
                character.EXP = 0;
            //clear boosts
            character.MaxHPBonus = 0;
            character.AtkBonus = 0;
            character.DefBonus = 0;
            character.MAtkBonus = 0;
            character.MDefBonus = 0;
            character.SpeedBonus = 0;

            if (!character.Dead)
                character.HP = character.MaxHP;
            
            //reroll skills
            BaseMonsterForm form = DataManager.Instance.GetMonster(character.BaseForm.Species).Forms[character.BaseForm.Form];

            while (character.BaseSkills[0].SkillNum > -1)
                character.DeleteSkill(0);
            List<int> final_skills = form.RollLatestSkills(character.Level, new List<int>());
            foreach (int skill in final_skills)
                character.LearnSkill(skill, true);
            character.Relearnables = new List<bool>();
        }

        public IEnumerator<YieldInstruction> RestoreLevel()
        {
            if (StartLevel > -1)
            {
                GameState state = DataManager.Instance.LoadMainGameState(false);
                if (state == null)
                {
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.SetDialogue(Text.FormatKey("DLG_ERR_READ_SAVE")));
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.SetDialogue(Text.FormatKey("DLG_ERR_READ_SAVE_FALLBACK")));
                    yield break;
                }
                //the current save file has the level-restricted characters
                //the other save file has the original characters
                for (int ii = 0; ii < ActiveTeam.Players.Count; ii++)
                {
                    TempCharBackRef backRef = ActiveTeam.Players[ii].BackRef;
                    if (backRef.Index > -1)
                        restoreCharLevel(ActiveTeam.Players[ii], backRef.Assembly ? state.Save.ActiveTeam.Assembly[backRef.Index] : state.Save.ActiveTeam.Players[backRef.Index], StartLevel);
                }
                for (int ii = 0; ii < ActiveTeam.Assembly.Count; ii++)
                {
                    TempCharBackRef backRef = ActiveTeam.Assembly[ii].BackRef;
                    if (ActiveTeam.Assembly[ii].BackRef.Index > -1)
                        restoreCharLevel(ActiveTeam.Assembly[ii], backRef.Assembly ? state.Save.ActiveTeam.Assembly[backRef.Index] : state.Save.ActiveTeam.Players[backRef.Index], StartLevel);
                }
                StartLevel = -1;
            }
            //also, restore default hunger values
            {
                for (int ii = 0; ii < ActiveTeam.Players.Count; ii++)
                {
                    ActiveTeam.Players[ii].MaxFullness = Character.MAX_FULLNESS;
                    ActiveTeam.Players[ii].Fullness = Character.MAX_FULLNESS;
                }
                for (int ii = 0; ii < ActiveTeam.Assembly.Count; ii++)
                {
                    ActiveTeam.Assembly[ii].MaxFullness = Character.MAX_FULLNESS;
                    ActiveTeam.Assembly[ii].Fullness = Character.MAX_FULLNESS;
                }
            }
        }

        private void restoreCharLevel(Character character, Character charFrom, int level)
        {
            //compute the amount of experience removed from the original character
            int removedEXP = 0;
            MonsterData monsterData = DataManager.Instance.GetMonster(charFrom.BaseForm.Species);
            BaseMonsterForm monsterForm = monsterData.Forms[charFrom.BaseForm.Form];
            int growth = monsterData.EXPTable;
            GrowthData growthData = DataManager.Instance.GetGrowth(growth);
            if (level <= charFrom.Level)
            {
                removedEXP += charFrom.EXP;
                int origLevel = charFrom.Level;
                while (origLevel > level)
                {
                    removedEXP += growthData.GetExpToNext(origLevel - 1);
                    origLevel--;
                }
            }
            //add the EXP to the character
            if (character.Level < DataManager.Instance.MaxLevel)
            {
                character.EXP += removedEXP;

                while (character.EXP >= growthData.GetExpToNext(character.Level))
                {
                    if (character.Level >= DataManager.Instance.MaxLevel)
                    {
                        character.EXP = 0;
                        break;
                    }
                    character.EXP -= growthData.GetExpToNext(character.Level);
                    character.Level++;
                }
            }
            //add stat boosts
            character.MaxHPBonus = Math.Min(character.MaxHPBonus + charFrom.MaxHPBonus, monsterForm.GetMaxStatBonus(Stat.HP));
            character.AtkBonus = Math.Min(character.AtkBonus + charFrom.AtkBonus, monsterForm.GetMaxStatBonus(Stat.Attack));
            character.DefBonus = Math.Min(character.DefBonus + charFrom.DefBonus, monsterForm.GetMaxStatBonus(Stat.Defense));
            character.MAtkBonus = Math.Min(character.MAtkBonus + charFrom.MAtkBonus, monsterForm.GetMaxStatBonus(Stat.MAtk));
            character.MDefBonus = Math.Min(character.MDefBonus + charFrom.MDefBonus, monsterForm.GetMaxStatBonus(Stat.MDef));
            character.SpeedBonus = Math.Min(character.SpeedBonus + charFrom.SpeedBonus, monsterForm.GetMaxStatBonus(Stat.Speed));
            character.HP = character.MaxHP;

            //restore skills
            while (character.BaseSkills[0].SkillNum > -1)
                character.DeleteSkill(0);
            for(int ii = 0; ii < charFrom.BaseSkills.Count; ii++)
            {
                if (charFrom.BaseSkills[ii].SkillNum > -1)
                {
                    bool enabled = false;
                    foreach (BackReference<Skill> skill in charFrom.Skills)
                    {
                        if (skill.BackRef == ii)
                        {
                            enabled = skill.Element.Enabled;
                            break;
                        }
                    }
                    character.LearnSkill(charFrom.BaseSkills[ii].SkillNum, enabled);
                }
            }

            //restore remembered skills
            for (int ii = 0; ii < charFrom.Relearnables.Count; ii++)
            {
                if (ii >= character.Relearnables.Count)
                    character.Relearnables.Add(false);
                character.Relearnables[ii] |= charFrom.Relearnables[ii];
            }

            //restoreCharLevel the backref
            character.BackRef = new TempCharBackRef(false, -1);
        }

        /// <summary>
        /// Removes cursed and hidden values from items after leaving dungeon mode.
        /// </summary>
        protected void ClearDungeonItems()
        {
            foreach (Character character in ActiveTeam.EnumerateChars())
            {
                if (character.EquippedItem.ID > -1)
                {
                    character.EquippedItem.Cursed = false;
                    ItemData entry = DataManager.Instance.GetItem(character.EquippedItem.ID);
                    if (entry.MaxStack <= 1 && entry.UsageType != ItemData.UseType.Box)
                        character.EquippedItem.HiddenValue = 0;
                }
            }
            foreach (InvItem item in ActiveTeam.EnumerateInv())
            {
                item.Cursed = false;
                ItemData entry = DataManager.Instance.GetItem(item.ID);
                if (entry.MaxStack <= 1 && entry.UsageType != ItemData.UseType.Box)
                    item.HiddenValue = 0;
            }
        }

        public static void SaveMainData(BinaryWriter writer, GameProgress save)
        {
            using (MemoryStream classStream = new MemoryStream())
            {
                Serializer.SerializeData(classStream, save);
                writer.Write(classStream.Position);
                classStream.WriteTo(writer.BaseStream);
            }
        }

        public static GameProgress LoadMainData(BinaryReader reader)
        {
            long oldOffset = reader.BaseStream.Position;
            long length = reader.ReadInt64();
            try
            {
                using (MemoryStream classStream = new MemoryStream(reader.ReadBytes((int)length)))
                {
                    return (GameProgress)Serializer.DeserializeData(classStream);
                }
            }
            catch (Exception)
            {
                reader.BaseStream.Seek(oldOffset, SeekOrigin.Begin);
                return LegacyLoadMainData(reader);
            }

        }

        //TODO: v0.6 Delete this
        public static GameProgress LegacyLoadMainData(BinaryReader reader)
        {
            long length = reader.ReadInt64();
            using (MemoryStream classStream = new MemoryStream(reader.ReadBytes((int)length)))
            {
                IFormatter formatter = new BinaryFormatter();
                if (DiagManager.Instance.UpgradeBinder != null)
                    formatter.Binder = DiagManager.Instance.UpgradeBinder;
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                GameProgress outsave = (GameProgress)formatter.Deserialize(classStream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                return outsave;
            }

        }

        public void RestartLogs(ulong seed)
        {
            StartDate = String.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
            TotalTurns = 0;
            EndDate = "";
            Location = "";
            Trail = new List<string>();

            Rand = new ReRandom(seed);//reseed own random
            DataManager.Instance.MsgLog.Clear();
        }


        public void FullRestore()
        {
            foreach (Character character in ActiveTeam.EnumerateChars())
            {
                character.Dead = false;
                character.FullRestore();
            }
            foreach (Character character in ActiveTeam.Assembly)
            {
                character.Dead = false;
                character.FullRestore();
            }
            MidAdventure = false;
            ClearDungeonItems();
            //clear rescue status
            Rescue = null;
        }

        public List<int> MergeDexTo(GameProgress destProgress, bool completion)
        {
            //monster encounters
            List<int> newRecruits = new List<int>();
            for (int ii = 0; ii < DataManager.Instance.DataIndices[DataManager.DataType.Monster].Count; ii++)
            {
                if (GetMonsterUnlock(ii) == UnlockState.Completed && destProgress.GetMonsterUnlock(ii) != UnlockState.Completed)
                {
                    MonsterData entry = DataManager.Instance.GetMonster(ii);
                    if (entry.PromoteFrom == -1)
                    {
                        bool isOriginal = false;
                        for (int jj = 0; jj < DataManager.Instance.StartChars.Count; jj++)
                        {
                            if (ii == DataManager.Instance.StartChars[jj].mon.Species)
                                isOriginal = true;
                        }
                        if (!isOriginal)
                            newRecruits.Add(ii);
                    }
                    if (completion)
                        destProgress.RegisterMonster(ii);
                    destProgress.RogueUnlockMonster(ii);
                }
                if (GetMonsterUnlock(ii) == UnlockState.Discovered && destProgress.GetMonsterUnlock(ii) == UnlockState.None)
                {
                    if (completion)
                        destProgress.SeenMonster(ii);
                }
            }
            return newRecruits;
        }


        public List<ModDiff> GetModDiffs()
        {
            List<ModDiff> result = new List<ModDiff>();

            if (GameVersion != Versioning.GetVersion())
                result.Add(new ModDiff("[Game]", Guid.Empty, GameVersion, Versioning.GetVersion()));

            if (Quest.UUID != PathMod.Quest.UUID)
            {
                result.Add(new ModDiff(Quest.GetMenuName(), Quest.UUID, Quest.Version, null));
                result.Add(new ModDiff(PathMod.Quest.GetMenuName(), PathMod.Quest.UUID, null, PathMod.Quest.Version));
            }
            else if (Quest.Version != PathMod.Quest.Version)
                result.Add(new ModDiff(PathMod.Quest.GetMenuName(), PathMod.Quest.UUID, Quest.Version, PathMod.Quest.Version));

            bool[] foundNewMod = new bool[PathMod.Mods.Length];
            foreach (ModHeader oldMod in Mods)
            {
                bool found = false;
                for (int ii = 0; ii < PathMod.Mods.Length; ii++)
                {
                    ModHeader newMod = PathMod.Mods[ii];
                    if (oldMod.UUID == newMod.UUID)
                    {
                        found = true;
                        foundNewMod[ii] = true;
                        if (oldMod.Version != newMod.Version)
                            result.Add(new ModDiff(newMod.GetMenuName(), newMod.UUID, oldMod.Version, newMod.Version));
                        break;
                    }
                }
                if (!found)
                    result.Add(new ModDiff(oldMod.GetMenuName(), oldMod.UUID, oldMod.Version, null));
            }
            for (int ii = 0; ii < foundNewMod.Length; ii++)
            {
                if (!foundNewMod[ii])
                    result.Add(new ModDiff(PathMod.Mods[ii].GetMenuName(), PathMod.Mods[ii].UUID, null, PathMod.Mods[ii].Version));
            }
            return result;
        }

        /// <summary>
        /// Returns true if
        /// Game version is higher than save's game version
        /// Quest is different
        /// Quest version is higher than save's quest version
        /// Mods are added or removed
        /// Mod version is higher than the save's mod version
        /// </summary>
        /// <returns></returns>
        public bool IsOldVersion()
        {
            if (GameVersion < Versioning.GetVersion())
                return true;

            if (Quest.UUID != PathMod.Quest.UUID || Quest.Version < PathMod.Quest.Version)
                return true;

            bool[] foundNewMod = new bool[PathMod.Mods.Length];
            foreach (ModHeader oldMod in Mods)
            {
                bool found = false;
                for (int ii = 0; ii < PathMod.Mods.Length; ii++)
                {
                    ModHeader newMod = PathMod.Mods[ii];
                    if (oldMod.UUID == newMod.UUID)
                    {
                        found = true;
                        foundNewMod[ii] = true;
                        if (oldMod.Version != newMod.Version)
                            return true;
                        break;
                    }
                }
                if (!found)
                    return true;
            }
            for (int ii = 0; ii < foundNewMod.Length; ii++)
            {
                if (!foundNewMod[ii])
                    return true;
            }
            return false;
        }


        public void UpdateVersion()
        {
            GameVersion = Versioning.GetVersion();
            Quest = PathMod.Quest;
            Mods.Clear();
            foreach (ModHeader curMod in PathMod.Mods)
                Mods.Add(curMod);
        }


        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            //TODO: v0.6 delete this
            if (GameVersion == null)
                GameVersion = new Version();
            //TODO: v0.6 delete this
            if (!Quest.IsValid())
                Quest = ModHeader.Invalid;
            //TODO: v0.6 delete this
            if (Mods == null)
                Mods = new List<ModHeader>();
        }
    }

    [Serializable]
    public class MainProgress : GameProgress
    {
        public ulong FirstSeed;
        public int TotalAdventures;

        public List<CharData> CharsToStore;
        public List<InvItem> ItemsToStore;
        public int[] StorageToStore;
        public int MoneyToStore;

        public MainProgress()
        {
            CharsToStore = new List<CharData>();
            ItemsToStore = new List<InvItem>();
            StorageToStore = new int[DataManager.Instance.DataIndices[DataManager.DataType.Item].Count];
        }

        public MainProgress(ulong seed, string uuid)
            : base(seed, uuid)
        {
            FirstSeed = seed;

            CharsToStore = new List<CharData>();
            ItemsToStore = new List<InvItem>();
            StorageToStore = new int[DataManager.Instance.DataIndices[DataManager.DataType.Item].Count];
        }


        public static void LossPenalty(GameProgress save)
        {
            //remove money
            save.ActiveTeam.Money = 0;
            //remove bag items
            for (int ii = save.ActiveTeam.GetInvCount() - 1; ii >= 0; ii--)
            {
                ItemData entry = DataManager.Instance.GetItem(save.ActiveTeam.GetInv(ii).ID);
                if (!entry.CannotDrop)
                    save.ActiveTeam.RemoveFromInv(ii);
            }

            //remove equips
            foreach (Character player in save.ActiveTeam.EnumerateChars())
            {
                if (player.EquippedItem.ID > -1)
                {
                    ItemData entry = DataManager.Instance.GetItem(player.EquippedItem.ID);
                    if (!entry.CannotDrop)
                        player.DequipItem();
                }
            }
        }

        public override IEnumerator<YieldInstruction> BeginGame(int zoneID, ulong seed, DungeonStakes stakes, bool recorded, bool noRestrict)
        {
            ZoneData zone = DataManager.Instance.GetZone(zoneID);
            //restrict team size/bag size/etc
            if (!noRestrict)
                yield return CoroutineManager.Instance.StartCoroutine(RestrictTeam(zone, false));

            MidAdventure = true;
            Stakes = stakes;

            //reset location defeated
            ClearDefeatDest();

            //create a copy (from save and load) of the current state and mark it with loss
            DataManager.Instance.SaveMainGameState();

            GameState state = DataManager.Instance.LoadMainGameState(false);
            if (state != null)
            {
                if (Stakes == DungeonStakes.Risk)
                    LossPenalty(state.Save);

                DataManager.Instance.SaveGameState(state);
            }

            //set everyone's levels and mark them for backreferral
            if (!noRestrict)
                yield return CoroutineManager.Instance.StartCoroutine(RestrictLevel(zone, false));

            RestartLogs(seed);
            RescuesLeft = zone.Rescues;

            if (recorded)
                DataManager.Instance.BeginPlay(PathMod.ModSavePath(DataManager.SAVE_PATH, DataManager.QUICKSAVE_FILE_PATH), zoneID, false, false);
        }

        public override IEnumerator<YieldInstruction> EndGame(ResultType result, ZoneLoc nextArea, bool display, bool fanfare)
        {
            Outcome = result;
            bool recorded = DataManager.Instance.RecordingReplay;
            string recordFile = null;
            if (result == ResultType.Rescue)
            {
                Location = ZoneManager.Instance.CurrentZone.GetDisplayName();

                DataManager.Instance.MsgLog.Clear();
                //end the game with a recorded ending
                recordFile = DataManager.Instance.EndPlay(this, StartDate);

                SOSMail sos = Rescue.SOS;
                string dateRescued = String.Format("{0:yyyy-MM-dd}", DateTime.Now);
                ReplayData replay = DataManager.Instance.LoadReplay(PathMod.ModSavePath(DataManager.REPLAY_PATH, recordFile), false);
                AOKMail aok = new AOKMail(sos, DataManager.Instance.Save, dateRescued, replay);
                GeneratedAOK = DataManager.SaveRescueMail(PathMod.NoMod(DataManager.RESCUE_OUT_PATH + DataManager.AOK_FOLDER), aok, false);
                string deletePath = DataManager.FindRescueMail(PathMod.NoMod(DataManager.RESCUE_IN_PATH + DataManager.SOS_FOLDER), sos, sos.Extension);
                if (deletePath != null)
                    File.Delete(deletePath);

                if (nextArea.IsValid()) //  if an exit is specified, go to the exit.
                    NextDest = nextArea;
                else
                    NextDest = new ZoneLoc(1, new SegLoc(-1, 1));
            }
            else if (result != ResultType.Cleared)
            {
                if (GameManager.Instance.CurrentScene == GroundScene.Instance)
                    Location = ZoneManager.Instance.CurrentGround.GetColoredName();
                else if (GameManager.Instance.CurrentScene == DungeonScene.Instance)
                    Location = ZoneManager.Instance.CurrentMap.GetColoredName();

                DataManager.Instance.MsgLog.Clear();
                //end the game with a recorded ending
                recordFile = DataManager.Instance.EndPlay(this, StartDate);

                if (Outcome != ResultType.Escaped && Stakes == DungeonStakes.Risk) //remove all items
                    LossPenalty(this);

                if (nextArea.IsValid()) //  if an exit is specified, go to the exit.
                    NextDest = nextArea;
                else
                    NextDest = new ZoneLoc(1, new SegLoc(-1, 1));

            }
            else
            {
                int completedZone = ZoneManager.Instance.CurrentZoneID;
                CompleteDungeon(completedZone);

                Location = ZoneManager.Instance.CurrentZone.GetDisplayName();

                DataManager.Instance.MsgLog.Clear();
                //end the game with a recorded ending
                recordFile = DataManager.Instance.EndPlay(this, StartDate);

                if (nextArea.IsValid()) //  if an exit is specified, go to the exit.
                    NextDest = nextArea;
                else
                    NextDest = new ZoneLoc(1, new SegLoc(-1, 1));

            }

            TotalAdventures++;

            FullRestore();

            //merge back the team if the dungeon was level-limited
            yield return CoroutineManager.Instance.StartCoroutine(RestoreLevel());

            GameState state = DataManager.Instance.LoadMainGameState(false);
            MainProgress mainSave = state?.Save as MainProgress;

            //save the result to the main file
            if (Stakes != DungeonStakes.None)
            {
                if (mainSave != null)
                    mainSave.MergeDataTo(this);
                DataManager.Instance.SaveMainGameState();
            }
            else
            {
                if (mainSave != null)
                    DataManager.Instance.SetProgress(mainSave);

                DataManager.Instance.Save.NextDest = NextDest;
            }

            MenuBase.Transparent = false;

            if (recorded && display)
            {
                GameProgress ending = DataManager.Instance.GetRecord(PathMod.ModSavePath(DataManager.REPLAY_PATH, recordFile));

                if (fanfare)
                {
                    if (result != ResultType.Cleared && result != ResultType.Escaped && result != ResultType.Rescue)
                        GameManager.Instance.Fanfare("Fanfare/MissionFail");
                    else
                        GameManager.Instance.Fanfare("Fanfare/MissionClear");
                }
                else
                    GameManager.Instance.SE("Menu/Skip");

                FinalResultsMenu menu = new FinalResultsMenu(ending);
                yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.ProcessMenuCoroutine(menu));
            }

            yield return new WaitForFrames(20);
        }

        public void MergeDataTo(MainProgress destProgress)
        {
            if (this != destProgress)
                MergeDexTo(destProgress, true);

            foreach (CharData charData in CharsToStore)
            {
                Character chara = new Character(charData, ActiveTeam);
                AITactic tactic = DataManager.Instance.GetAITactic(0);
                chara.Tactic = new AITactic(tactic);
                destProgress.ActiveTeam.Assembly.Add(chara);
            }
            CharsToStore.Clear();

            destProgress.ActiveTeam.StoreItems(ItemsToStore);
            ItemsToStore.Clear();

            //just add storage values
            for (int ii = 0; ii < StorageToStore.Length; ii++)
            {
                destProgress.ActiveTeam.Storage[ii] += StorageToStore[ii];
                StorageToStore[ii] = 0;
            }

            //put the money in the bank
            destProgress.ActiveTeam.Bank += MoneyToStore;
            MoneyToStore = 0;
        }
    }


    [Serializable]
    public class RogueProgress : GameProgress
    {
        public bool Seeded { get; set; }

        public RogueProgress()
        { }
        public RogueProgress(ulong seed, string uuid, bool seeded) : base(seed, uuid)
        {
            Seeded = seeded;
        }

        public override void SeenMonster(int index)
        {
            base.SeenMonster(index);
        }
        public override void RegisterMonster(int index)
        {
            base.RegisterMonster(index);
        }

        public override IEnumerator<YieldInstruction> BeginGame(int zoneID, ulong seed, DungeonStakes stakes, bool recorded, bool noRestrict)
        {
            MidAdventure = true;
            Stakes = stakes;

            if (recorded)
            {
                if (!Directory.Exists(PathMod.ModSavePath(DataManager.ROGUE_PATH)))
                    Directory.CreateDirectory(PathMod.ModSavePath(DataManager.ROGUE_PATH));
                DataManager.Instance.BeginPlay(PathMod.ModSavePath(DataManager.ROGUE_PATH, DataManager.Instance.Save.StartDate + DataManager.QUICKSAVE_EXTENSION), zoneID, true, Seeded);
            }

            yield break;
        }

        public override IEnumerator<YieldInstruction> EndGame(ResultType result, ZoneLoc nextArea, bool display, bool fanfare)
        {
            bool recorded = DataManager.Instance.RecordingReplay;
            //if lose, end the play, display plaque, and go to title
            if (result != ResultType.Cleared)
            {
                if (GameManager.Instance.CurrentScene == GroundScene.Instance)
                    Location = ZoneManager.Instance.CurrentGround.GetColoredName();
                else if (GameManager.Instance.CurrentScene == DungeonScene.Instance)
                    Location = ZoneManager.Instance.CurrentMap.GetColoredName();

                Outcome = result;

                DataManager.Instance.MsgLog.Clear();
                //end the game with a recorded ending
                string recordFile = DataManager.Instance.EndPlay(this, null);

                MenuBase.Transparent = false;
                //save to the main file
                GameState state = DataManager.Instance.LoadMainGameState(false);
                List<int> newRecruits = new List<int>();
                if (state != null)
                {
                    newRecruits = MergeDexTo(state.Save, false);
                    DataManager.Instance.SaveGameState(state);
                }


                if (recorded && display)
                {
                    GameProgress ending = DataManager.Instance.GetRecord(PathMod.ModSavePath(DataManager.REPLAY_PATH, recordFile));

                    if (fanfare)
                    {
                        if (result != ResultType.Cleared)
                            GameManager.Instance.Fanfare("Fanfare/MissionFail");
                        else
                            GameManager.Instance.Fanfare("Fanfare/MissionClear");
                    }
                    else
                        GameManager.Instance.SE("Menu/Skip");

                    FinalResultsMenu menu = new FinalResultsMenu(ending);
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.ProcessMenuCoroutine(menu));

                    Dictionary<int, List<RecordHeaderData>> scores = RecordHeaderData.LoadHighScores();
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.ProcessMenuCoroutine(new ScoreMenu(scores, ZoneManager.Instance.CurrentZoneID, PathMod.ModSavePath(DataManager.REPLAY_PATH, recordFile))));

                }

                if (newRecruits.Count > 0)
                {
                    yield return new WaitForFrames(10);
                    GameManager.Instance.Fanfare("Fanfare/NewArea");
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.SetDialogue(Text.FormatKey("DLG_NEW_CHARS")));
                }

                yield return new WaitForFrames(20);

                GameManager.Instance.SceneOutcome = GameManager.Instance.RestartToTitle();
            }
            else
            {
                int completedZone = ZoneManager.Instance.CurrentZoneID;

                MidAdventure = true;
                ClearDungeonItems();

                //  if there isn't a next area, end the play, display the plaque, return to title screen
                //GameManager.Instance.Fanfare("Fanfare/MissionClear");
                Location = ZoneManager.Instance.CurrentZone.GetDisplayName();

                Outcome = result;

                DataManager.Instance.MsgLog.Clear();
                //end the game with a recorded ending
                string recordFile = DataManager.Instance.EndPlay(this, null);

                MenuBase.Transparent = false;

                //save to the main file
                GameState state = DataManager.Instance.LoadMainGameState(false);
                if (state != null)
                {
                    MergeDexTo(state.Save, true);
                    state.Save.CompleteDungeon(completedZone);
                    DataManager.Instance.SaveGameState(state);
                }



                if (recorded)
                {
                    GameProgress ending = DataManager.Instance.GetRecord(PathMod.ModSavePath(DataManager.REPLAY_PATH, recordFile));

                    if (fanfare)
                    {
                        if (result != ResultType.Cleared)
                            GameManager.Instance.Fanfare("Fanfare/MissionFail");
                        else
                            GameManager.Instance.Fanfare("Fanfare/MissionClear");
                    }
                    else
                        GameManager.Instance.SE("Menu/Skip");

                    FinalResultsMenu menu = new FinalResultsMenu(ending);
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.ProcessMenuCoroutine(menu));

                    Dictionary<int, List<RecordHeaderData>> scores = RecordHeaderData.LoadHighScores();
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.ProcessMenuCoroutine(new ScoreMenu(scores, ZoneManager.Instance.CurrentZoneID, PathMod.ModSavePath(DataManager.REPLAY_PATH, recordFile))));
                }

                //ask to transfer if the dungeon records progress, and it is NOT a seeded run.
                if (state != null && Stakes != DungeonStakes.None && !Seeded)
                {
                    bool allowTransfer = false;
                    DialogueBox question = MenuManager.Instance.CreateQuestion(Text.FormatKey("DLG_TRANSFER_ASK"),
                        () => { allowTransfer = true; }, () => { });
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.ProcessMenuCoroutine(question));

                    if (allowTransfer)
                    {
                        MainProgress mainSave = state.Save as MainProgress;
                        //put the new recruits into assembly
                        foreach (Character character in ActiveTeam.Players)
                        {
                            if (!(character.Dead && DataManager.Instance.GetSkin(character.BaseForm.Skin).Challenge))
                            {
                                if (character.EquippedItem.ID > -1)
                                    mainSave.ItemsToStore.Add(character.EquippedItem);
                                mainSave.CharsToStore.Add(new CharData(character));
                            }
                        }
                        foreach (Character character in ActiveTeam.Assembly)
                        {
                            if (!(character.Dead && DataManager.Instance.GetSkin(character.BaseForm.Skin).Challenge))
                                mainSave.CharsToStore.Add(new CharData(character));
                        }

                        //put the new items into the storage
                        foreach (InvItem item in ActiveTeam.EnumerateInv())
                            mainSave.ItemsToStore.Add(item);
                        foreach (InvItem item in ActiveTeam.BoxStorage)
                            mainSave.ItemsToStore.Add(item);

                        mainSave.StorageToStore = ActiveTeam.Storage;

                        mainSave.MoneyToStore = state.Save.ActiveTeam.Money + state.Save.ActiveTeam.Bank;
                    }

                    DataManager.Instance.SaveGameState(state);

                    if (allowTransfer)
                        yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.SetDialogue(Text.FormatKey("DLG_TRANSFER_COMPLETE")));
                }



                yield return new WaitForFrames(20);
            }
        }


    }

}
