﻿using System;
using System.Collections.Generic;

namespace RogueEssence.Data
{
    [Serializable]
    public class EntryDataIndex
    {
        public int Count { get { return Entries.Length; } }

        public EntrySummary[] Entries;

        public EntryDataIndex()
        {

        }

        public string[] GetLocalStringArray(bool verbose = false)
        {
            string[] names = new string[Entries.Length];
            for (int ii = 0; ii < Entries.Length; ii++)
                names[ii] = Entries[ii].GetLocalString(verbose);
            return names;
        }
    }


    [Serializable]
    public class EntrySummary
    {
        public LocalText Name;
        public bool Released;
        public string Comment;

        public EntrySummary()
        {
            Name = new LocalText();
            Comment = "";
        }

        public EntrySummary(LocalText name, bool released, string comment)
        {
            Name = name;
            Released = released;
            Comment = comment;
        }

        public virtual string GetColoredName()
        {
            return String.Format("[color=#00FF00]{0}[color]", Name.ToLocal());
        }

        public string GetLocalString(bool verbose)
        {
            string result = Name.ToLocal();
            if (!Released)
                result = "*" + result;
            if (verbose && Comment != "")
            {
                result += "  #";
                string[] lines = Comment.Split('\n', StringSplitOptions.None);
                result += lines[0];
            }

            return result;
        }
    }


}
