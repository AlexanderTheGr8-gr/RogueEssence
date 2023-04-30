﻿using System.Collections.Generic;
using RogueElements;

namespace RogueEssence.Menu
{
    public class OthersMenu : TitledStripMenu
    {
        public List<MenuTextChoice> Choices { get; set; }
        public OthersMenu()
        {
            Choices = new List<MenuTextChoice>();
        }

        public static OthersMenu InitDefaultOthersMenu()
        {
            OthersMenu othersMenu = new OthersMenu();
            othersMenu.SetupChoices();
            othersMenu.InitMenu();
            return othersMenu;
        }

        public void SetupChoices()
        {
            Choices.Clear();
            Choices.Add(new MenuTextChoice(Text.FormatKey("MENU_MSG_LOG_TITLE"), () => { MenuManager.Instance.AddMenu(new MsgLogMenu(), false); }));
            Choices.Add(new MenuTextChoice(Text.FormatKey("MENU_SETTINGS_TITLE"), () => { MenuManager.Instance.AddMenu(new SettingsMenu(), false); }));
            Choices.Add(new MenuTextChoice(Text.FormatKey("MENU_KEYBOARD_TITLE"), () => { MenuManager.Instance.AddMenu(new KeyControlsMenu(), false); }));
            Choices.Add(new MenuTextChoice(Text.FormatKey("MENU_GAMEPAD_TITLE"), () => { MenuManager.Instance.AddMenu(new GamepadControlsMenu(), false); }));
        }

        public void InitMenu()
        {
            Initialize(new Loc(16, 16), CalculateChoiceLength(Choices, 72), Text.FormatKey("MENU_OTHERS_TITLE"), Choices.ToArray(), 0);
        }

    }
}
