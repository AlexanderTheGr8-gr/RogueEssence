﻿using System.Collections.Generic;
using RogueElements;
using RogueEssence.Content;
using RogueEssence.Data;
using Microsoft.Xna.Framework;

namespace RogueEssence.Menu
{
    public class TradeSummary : SummaryMenu
    {

        MenuText Title;
        MenuDivider MenuDiv;

        public TradeSummary(Rect bounds)
            : base(bounds)
        {
            Title = new MenuText(Text.FormatKey("MENU_SWAP_NEEDED"), new Loc(GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight));
            Elements.Add(Title);
            MenuDiv = new MenuDivider(new Loc(GraphicsManager.MenuBG.TileWidth, GraphicsManager.MenuBG.TileHeight + LINE_HEIGHT),
                Bounds.Width - GraphicsManager.MenuBG.TileWidth * 2);
            Elements.Add(MenuDiv);
        }

        public void SetTrade(int[] tradeIns, int price, bool[] itemPresence, int presenceCount)
        {
            while (Elements.Count > 2)
                Elements.RemoveAt(2);

            List<MenuText> reqs = new List<MenuText>();

            int wildcards = 0;
            foreach (int reqItem in tradeIns)
            {
                if (reqItem == -1)
                    wildcards++;
                else
                {
                    ItemData entry = DataManager.Instance.GetItem(reqItem);
                    reqs.Add(new MenuText(entry.GetIconName(), Loc.Zero, itemPresence[reqItem] ? Color.White : Color.Red));
                }
            }
            if (wildcards > 0)
            {
                reqs.Add(new MenuText(Text.FormatKey("MENU_SWAP_ANY", wildcards), Loc.Zero, presenceCount >= wildcards ? Color.White : Color.Red));
            }

            reqs.Add(new MenuText(Text.FormatKey("MONEY_AMOUNT", price), Loc.Zero, (DataManager.Instance.Save.ActiveTeam.Money >= price) ? Color.White : Color.Red));

            for (int ii = 0; ii < reqs.Count; ii++)
            {
                reqs[ii].Loc = new Loc(GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight + TitledStripMenu.TITLE_OFFSET + VERT_SPACE * ii);
                Elements.Add(reqs[ii]);
            }
        }
    }
}
