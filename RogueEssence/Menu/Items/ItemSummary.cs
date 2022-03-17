﻿using RogueElements;
using RogueEssence.Content;
using RogueEssence.Dungeon;

namespace RogueEssence.Menu
{
    public class ItemSummary : SummaryMenu
    {

        DialogueText Description;
        MenuText Rarity;
        MenuText SalePrice;

        public ItemSummary(Rect bounds)
            : base(bounds)
        {
            Description = new DialogueText("", new Rect(new Loc(GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight),
                new Loc(Bounds.Width - GraphicsManager.MenuBG.TileWidth * 4, Bounds.Height - GraphicsManager.MenuBG.TileHeight * 4)), LINE_HEIGHT);
            Elements.Add(Description);
            SalePrice = new MenuText("", new Loc(Bounds.Width - GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight + 4 * LINE_HEIGHT), DirH.Right);
            Elements.Add(SalePrice);
            Rarity = new MenuText("", new Loc(GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight + 4 * LINE_HEIGHT), DirH.Left);
            Elements.Add(Rarity);
        }

        public void SetItem(InvItem item)
        {
            Data.ItemData entry = Data.DataManager.Instance.GetItem(item.ID);
            Description.SetFormattedText(entry.Desc.ToLocal());
            SalePrice.SetText(Text.FormatKey("MENU_ITEM_VALUE", Text.FormatKey("MONEY_AMOUNT", item.GetSellValue())));
            if (entry.Rarity > 0)
            {
                string rarityStr = "";
                for (int ii = 0; ii < entry.Rarity; ii++)
                    rarityStr += "\uE10C";
                Rarity.SetText(Text.FormatKey("MENU_ITEM_RARITY", rarityStr));
            }
            else
                Rarity.SetText("");
        }
    }
}
