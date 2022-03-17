﻿using RogueEssence.Content;
using System;

namespace RogueEssence.Menu
{
    public class TimedDialog : DialogueBox
    {
        private int time;
        private Action action;

        protected FrameTick FinishedTextTime;

        public TimedDialog(string message, bool sound, bool centerH, bool centerV, int time, Action action)
            : base(message, sound, centerH, centerV)
        {
            this.time = time;
            this.action = action;
        }

        public override void ProcessActions(FrameTick elapsedTime)
        {
            base.ProcessActions(elapsedTime);
            if (CurrentText.Finished)
                FinishedTextTime += elapsedTime;
        }

        public override void ProcessTextDone(InputManager input)
        {
            if (FinishedTextTime >= time)
            {
                //close this
                MenuManager.Instance.RemoveMenu();

                //do what it wants
                action();
            }
        }
    }
}
