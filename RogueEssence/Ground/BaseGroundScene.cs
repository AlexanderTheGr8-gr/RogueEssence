﻿using System;
using System.Collections.Generic;
using RogueEssence.Content;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Menu;
using RogueEssence.Dungeon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RogueEssence.Ground
{
    //The game engine for Ground Mode, in which the player has free movement
    public abstract class BaseGroundScene : BaseScene
    {
        public Loc MouseLoc;

        public IEnumerator<YieldInstruction> PendingDevEvent;


        protected List<IDrawableSprite> groundDraw;

        protected List<IDrawableSprite> objectDraw;

        protected List<IDrawableSprite> foregroundDraw;

        protected Rect viewTileRect;
        

        private RenderTarget2D gameScreen;

        public BaseGroundScene()
        {

            groundDraw = new List<IDrawableSprite>();
            objectDraw = new List<IDrawableSprite>();
            foregroundDraw = new List<IDrawableSprite>();

            gameScreen = new RenderTarget2D(
                GraphicsManager.GraphicsDevice,
                GraphicsManager.ScreenWidth,
                GraphicsManager.ScreenHeight,
                false,
                GraphicsManager.GraphicsDevice.PresentationParameters.BackBufferFormat,
                DepthFormat.Depth24);
        }

        public void ZoomChanged()
        {
            int zoomMult = Math.Min(GraphicsManager.WindowZoom, (int)Math.Max(1, 1 / GraphicsManager.Zoom.GetScale()));
            gameScreen = new RenderTarget2D(GraphicsManager.GraphicsDevice,
                GraphicsManager.ScreenWidth * zoomMult, GraphicsManager.ScreenHeight * zoomMult,
                false, GraphicsManager.GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
        }

        public override void Begin()
        {
            PendingDevEvent = null;
        }


        public override void UpdateMeta()
        {
            base.UpdateMeta();
            InputManager input = GameManager.Instance.MetaInputManager;
            MouseLoc = input.MouseLoc;
        }

        protected void UpdateCam(ref Loc focusedLoc)
        {

            //update cam
            windowScale = GraphicsManager.WindowZoom;

            scale = GraphicsManager.Zoom.GetScale();

            matrixScale = windowScale;
            drawScale = scale;
            while (matrixScale > 1 && drawScale < 1)
            {
                matrixScale /= 2;
                drawScale *= 2;
            }

            if (ZoneManager.Instance.CurrentGround.EdgeView == Map.ScrollEdge.Clamp)
                focusedLoc = new Loc(Math.Max((int)(GraphicsManager.ScreenWidth / scale / 2), Math.Min(focusedLoc.X,
                    ZoneManager.Instance.CurrentGround.GroundWidth - (int)(GraphicsManager.ScreenWidth / scale / 2))),
                    Math.Max((int)(GraphicsManager.ScreenHeight / scale / 2), Math.Min(focusedLoc.Y,
                    ZoneManager.Instance.CurrentGround.GroundHeight - (int)(GraphicsManager.ScreenHeight / scale / 2))));

            ViewRect = new Rect((int)(focusedLoc.X - GraphicsManager.ScreenWidth / scale / 2), (int)(focusedLoc.Y - GraphicsManager.ScreenHeight / scale / 2),
                (int)(GraphicsManager.ScreenWidth / scale), (int)(GraphicsManager.ScreenHeight / scale));
            viewTileRect = new Rect((int)Math.Floor((float)ViewRect.X / ZoneManager.Instance.CurrentGround.TileSize), (int)Math.Floor((float)ViewRect.Y / ZoneManager.Instance.CurrentGround.TileSize),
                (ViewRect.End.X - 1) / ZoneManager.Instance.CurrentGround.TileSize + 1 - (int)Math.Floor((float)ViewRect.X / ZoneManager.Instance.CurrentGround.TileSize), (ViewRect.End.Y - 1) / ZoneManager.Instance.CurrentGround.TileSize + 1 - (int)Math.Floor((float)ViewRect.Y / ZoneManager.Instance.CurrentGround.TileSize));

        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (ZoneManager.Instance.CurrentGround != null)
            {
                GraphicsManager.GraphicsDevice.SetRenderTarget(gameScreen);

                GraphicsManager.GraphicsDevice.Clear(Color.Transparent);

                Matrix matrix = Matrix.CreateScale(new Vector3(drawScale, drawScale, 1));
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, matrix);

                groundDraw.Clear();
                objectDraw.Clear();
                foregroundDraw.Clear();

                DrawGame(spriteBatch);

                spriteBatch.End();


                GraphicsManager.GraphicsDevice.SetRenderTarget(null);

                GraphicsManager.GraphicsDevice.Clear(Color.Black);

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, Matrix.CreateScale(new Vector3(matrixScale, matrixScale, 1)));

                spriteBatch.Draw(gameScreen, new Vector2(), Color.White);

                spriteBatch.End();

                spriteBatch.Begin();

                DrawDev(spriteBatch);

                spriteBatch.End();
            }

        }

        public virtual void DrawGame(SpriteBatch spriteBatch)
        {
            //draw the background
            ZoneManager.Instance.CurrentGround.Background.Draw(spriteBatch, Loc.Zero);

            for (int jj = viewTileRect.Y; jj < viewTileRect.End.Y; jj++)
            {
                for (int ii = viewTileRect.X; ii < viewTileRect.End.X; ii++)
                {
                    //if it's a tile on the discovery array, show it
                    bool outOfBounds = !Collision.InBounds(ZoneManager.Instance.CurrentGround.Width, ZoneManager.Instance.CurrentGround.Height, new Loc(ii, jj));

                    if (outOfBounds)
                        ZoneManager.Instance.CurrentGround.DrawDefaultTile(spriteBatch, new Loc(ii * ZoneManager.Instance.CurrentGround.TileSize, jj * ZoneManager.Instance.CurrentGround.TileSize) - ViewRect.Start, new Loc(ii, jj));
                    else
                        ZoneManager.Instance.CurrentGround.DrawLoc(spriteBatch, new Loc(ii * ZoneManager.Instance.CurrentGround.TileSize, jj * ZoneManager.Instance.CurrentGround.TileSize) - ViewRect.Start, new Loc(ii, jj), false);
                }
            }

            //draw effects laid on ground
            foreach (AnimLayer layer in ZoneManager.Instance.CurrentGround.Decorations)
            {
                if (layer.Visible)
                {
                    foreach (IDrawableSprite effect in layer.Anims)
                    {
                        if (CanSeeSprite(ViewRect, effect))
                            AddToDraw((layer.Layer == DrawLayer.Top) ? foregroundDraw : groundDraw, effect);
                    }
                }
            }
            foreach (IDrawableSprite effect in Anims[(int)DrawLayer.Bottom])
            {
                if (CanSeeSprite(ViewRect, effect))
                    AddToDraw(groundDraw, effect);
            }
            int charIndex = 0;
            while (charIndex < groundDraw.Count)
            {
                groundDraw[charIndex].Draw(spriteBatch, ViewRect.Start);
                charIndex++;
            }

            List<GroundChar> shownShadows = new List<GroundChar>();

            //draw effects in object space
            //get all back effects, see if they're in the screen, and put them in the list, sorted
            foreach (BaseAnim effect in Anims[(int)DrawLayer.Back])
            {
                if (CanSeeSprite(ViewRect, effect))
                    AddToDraw(objectDraw, effect);
            }

            if (!DataManager.Instance.HideChars)
            {
                //check if player/enemies is in the screen, put in list
                foreach (GroundChar character in ZoneManager.Instance.CurrentGround.IterateCharacters())
                {
                    if (!character.EntEnabled)
                        continue;
                    if (CanSeeSprite(ViewRect, character))
                    {
                        AddToDraw(objectDraw, character);
                        shownShadows.Add(character);
                    }
                }
            }
            //get all effects, see if they're in the screen, and put them in the list, sorted
            foreach (BaseAnim effect in Anims[(int)DrawLayer.Normal])
            {
                if (CanSeeSprite(ViewRect, effect))
                    AddToDraw(objectDraw, effect);
            }

            //draw shadows
            foreach (GroundChar shadowChar in shownShadows)
            {
                if (shadowChar.EntEnabled)
                    shadowChar.DrawShadow(spriteBatch, ViewRect.Start);
            }

            //draw items
            if (!DataManager.Instance.HideObjects)
            {
                foreach (GroundObject item in ZoneManager.Instance.CurrentGround.Entities[0].GroundObjects)
                {
                    if (!item.EntEnabled)
                        continue;
                    if (CanSeeSprite(ViewRect, item))
                        AddToDraw(objectDraw, item);
                }
            }

            //draw object
            charIndex = 0;
            for (int j = viewTileRect.Y; j < viewTileRect.End.Y; j++)
            {
                while (charIndex < objectDraw.Count)
                {
                    int charY = objectDraw[charIndex].MapLoc.Y;
                    if (charY == j * ZoneManager.Instance.CurrentGround.TileSize)
                    {
                        objectDraw[charIndex].Draw(spriteBatch, ViewRect.Start);
                        charIndex++;
                    }
                    else
                        break;
                }

                while (charIndex < objectDraw.Count)
                {
                    int charY = objectDraw[charIndex].MapLoc.Y;
                    if (charY < (j + 1) * ZoneManager.Instance.CurrentGround.TileSize)
                    {
                        objectDraw[charIndex].Draw(spriteBatch, ViewRect.Start);
                        charIndex++;
                    }
                    else
                        break;
                }
            }

            while (charIndex < objectDraw.Count)
            {
                objectDraw[charIndex].Draw(spriteBatch, ViewRect.Start);
                charIndex++;
            }

            //draw effects in top
            foreach (BaseAnim effect in Anims[(int)DrawLayer.Front])
            {
                if (CanSeeSprite(ViewRect, effect))
                    AddToDraw(objectDraw, effect);
            }
            charIndex = 0;
            while (charIndex < objectDraw.Count)
            {
                objectDraw[charIndex].Draw(spriteBatch, ViewRect.Start);
                charIndex++;
            }

            //draw tiles in front
            for (int jj = viewTileRect.Y; jj < viewTileRect.End.Y; jj++)
            {
                for (int ii = viewTileRect.X; ii < viewTileRect.End.X; ii++)
                {
                    //if it's a tile on the discovery array, show it
                    bool outOfBounds = !Collision.InBounds(ZoneManager.Instance.CurrentGround.Width, ZoneManager.Instance.CurrentGround.Height, new Loc(ii, jj));

                    if (!outOfBounds)
                        ZoneManager.Instance.CurrentGround.DrawLoc(spriteBatch, new Loc(ii * ZoneManager.Instance.CurrentGround.TileSize, jj * ZoneManager.Instance.CurrentGround.TileSize) - ViewRect.Start, new Loc(ii, jj), true);
                }
            }

            //draw effects in foreground
            foreach (BaseAnim effect in Anims[(int)DrawLayer.Top])
            {
                if (CanSeeSprite(ViewRect, effect))
                    AddToDraw(foregroundDraw, effect);
            }
            charIndex = 0;
            while (charIndex < foregroundDraw.Count)
            {
                foregroundDraw[charIndex].Draw(spriteBatch, ViewRect.Start);
                charIndex++;
            }

        }

        public virtual void DrawDev(SpriteBatch spriteBatch)
        { }

        public override void DrawDebug(SpriteBatch spriteBatch)
        {
            base.DrawDebug(spriteBatch);

            if (ZoneManager.Instance.CurrentGround != null)
            {
                Loc loc = ScreenCoordsToGroundCoords(MouseLoc);
                Loc blockLoc = ScreenCoordsToBlockCoords(MouseLoc);
                Loc tileLoc = ScreenCoordsToMapCoords(MouseLoc);
                GraphicsManager.SysFont.DrawText(spriteBatch, 2, 82, String.Format("Mouse  X:{0:D3} Y:{1:D3}", loc.X, loc.Y), null, DirV.Up, DirH.Left, Color.White);
                GraphicsManager.SysFont.DrawText(spriteBatch, 2, 92, String.Format("M Wall X:{0:D3} Y:{1:D3}", blockLoc.X, blockLoc.Y), null, DirV.Up, DirH.Left, Color.White);
                GraphicsManager.SysFont.DrawText(spriteBatch, 2, 102, String.Format("M Tile X:{0:D3} Y:{1:D3}", tileLoc.X, tileLoc.Y), null, DirV.Up, DirH.Left, Color.White);
            }
        }


        public Loc ScreenCoordsToGroundCoords(Loc loc)
        {
            loc.X = (int)(loc.X / scale / windowScale);
            loc.Y = (int)(loc.Y / scale / windowScale);
            loc += ViewRect.Start;

            return loc;
        }

        public Loc ScreenCoordsToMapCoords(Loc loc)
        {
            loc.X = (int)(loc.X / scale / windowScale);
            loc.Y = (int)(loc.Y / scale / windowScale);
            loc += ViewRect.Start;
            loc = loc - (ViewRect.Start / ZoneManager.Instance.CurrentGround.TileSize * ZoneManager.Instance.CurrentGround.TileSize) + new Loc(ZoneManager.Instance.CurrentGround.TileSize);
            loc /= ZoneManager.Instance.CurrentGround.TileSize;
            loc = loc + (ViewRect.Start / ZoneManager.Instance.CurrentGround.TileSize) - new Loc(1);

            return loc;
        }


        public Loc ScreenCoordsToBlockCoords(Loc loc)
        {
            int blockSize = GraphicsManager.TEX_SIZE;

            loc.X = (int)(loc.X / scale / windowScale);
            loc.Y = (int)(loc.Y / scale / windowScale);
            loc += ViewRect.Start;
            loc = loc - (ViewRect.Start / blockSize * blockSize) + new Loc(blockSize);
            loc /= blockSize;
            loc = loc + (ViewRect.Start / blockSize) - new Loc(1);

            return loc;
        }

        public void LogMsg(string msg)
        {
            //remove tags such as pauses
            int tabIndex = msg.IndexOf("[pause=", 0, StringComparison.OrdinalIgnoreCase);
            while (tabIndex > -1)
            {
                int endIndex = msg.IndexOf("]", tabIndex);
                if (endIndex == -1)
                    break;
                int param;
                if (Int32.TryParse(msg.Substring(tabIndex + "[pause=".Length, endIndex - (tabIndex + "[pause=".Length)), out param))
                {
                    TextPause pause = new TextPause();
                    pause.LetterIndex = tabIndex;
                    pause.Time = param;
                    msg = msg.Remove(tabIndex, endIndex - tabIndex + "]".Length);

                    tabIndex = msg.IndexOf("[pause=", tabIndex, StringComparison.OrdinalIgnoreCase);
                }
                else
                    break;
            }

            if (msg == Text.DIVIDER_STR)
            {
                if (DataManager.Instance.MsgLog.Count == 0 || DataManager.Instance.MsgLog[DataManager.Instance.MsgLog.Count - 1] == Text.DIVIDER_STR)
                    return;
            }
            else if (String.IsNullOrWhiteSpace(msg))
                return;

            DataManager.Instance.MsgLog.Add(msg);
        }


        public Rect GetViewRectangle()
        {
            return ViewRect;
        }

        public float GetWindowScale()
        {
            return windowScale;
        }

    }
}
