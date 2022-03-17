﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace RogueEssence.Content
{
    public class EmotionType
    {
        public string Name;
        public List<int> Fallbacks;

        public EmotionType(string name)
        {
            Name = name;
            Fallbacks = new List<int>();
        }
    }

    public struct PortraitData
    {
        public int Position;
        public bool HasReverse;

        public PortraitData(int pos, bool hasReverse)
        {
            Position = pos;
            HasReverse = hasReverse;
        }
    }

    [Serializable]
    public struct EmoteStyle
    {
        public int Emote;
        public bool Reverse;

        public EmoteStyle(int emote)
        {
            Emote = emote;
            Reverse = false;
        }
        public EmoteStyle(int emote, bool reverse)
        {
            Emote = emote;
            Reverse = reverse;
        }
        
    }


    public class PortraitSheet : TileSheet
    {
        private Dictionary<int, PortraitData> emoteMap;

        //TODO: find a way to remove this
        protected PortraitSheet(Texture2D tex, int width, int height, Dictionary<int, PortraitData> emoteMap)
            : base(tex, width, height)
        {
            this.emoteMap = emoteMap;
        }

        protected PortraitSheet(Texture2D tex, Dictionary<int, PortraitData> emoteMap)
            : base(tex, GraphicsManager.PortraitSize, GraphicsManager.PortraitSize)
        {
            this.emoteMap = emoteMap;
        }

        //this can inherit tilesheet
        //frompath (import) will take a folder containing all elements
        //fromstream (load) will take the png, the tile height/width, and the emotion maps
        //save will save as .portrait

        private static bool colorsBlank(Color[] colors)
        {
            for (int ii = 0; ii < colors.Length; ii++)
            {
                if (colors[ii].A > 0)
                    return false;
            }
            return true;
        }

        public static new PortraitSheet Import(string baseDirectory)
        {
            if (File.Exists(baseDirectory + "Sheet.png"))
            {
                Texture2D initTex = null;
                using (FileStream fileStream = new FileStream(baseDirectory + "Sheet.png", FileMode.Open, FileAccess.Read, FileShare.Read))
                    initTex = ImportTex(fileStream);
                if (initTex.Width % GraphicsManager.PortraitSize != 0 || initTex.Height % GraphicsManager.PortraitSize != 0)
                    throw new InvalidOperationException(String.Format("{0} has incorrect dimensions. Must be a multiple of {1}x{1}", baseDirectory + "Sheet.png", GraphicsManager.PortraitSize));

                int tileWidth = initTex.Width / GraphicsManager.PortraitSize;
                int tileHeight = initTex.Height / GraphicsManager.PortraitSize;

                Dictionary<int, PortraitData> animData = new Dictionary<int, PortraitData>();
                List<Texture2D> sheets = new List<Texture2D>();
                for (int ii = 0; ii < GraphicsManager.Emotions.Count; ii++)
                {
                    int xx = ii % tileWidth;
                    int yy = ii / tileWidth;
                    Color[] portraitColors = GetData(initTex, xx * GraphicsManager.PortraitSize, yy * GraphicsManager.PortraitSize, GraphicsManager.PortraitSize, GraphicsManager.PortraitSize);
                    if (!colorsBlank(portraitColors))
                    {
                        int rxx = (ii + GraphicsManager.Emotions.Count) % tileWidth;
                        int ryy = (ii + GraphicsManager.Emotions.Count) / tileWidth;
                        Color[] reverseColors = new Color[0];
                        if (ii + GraphicsManager.Emotions.Count < tileWidth * tileHeight)
                            reverseColors = GetData(initTex, rxx * GraphicsManager.PortraitSize, ryy * GraphicsManager.PortraitSize, GraphicsManager.PortraitSize, GraphicsManager.PortraitSize);
                        
                        bool hasReverse = !colorsBlank(reverseColors);
                        {
                            Texture2D newSheet = new Texture2D(device, GraphicsManager.PortraitSize, GraphicsManager.PortraitSize);
                            newSheet.SetData(portraitColors);

                            animData.Add(ii, new PortraitData(sheets.Count, hasReverse));
                            sheets.Add(newSheet);
                        }

                        if (hasReverse)
                        {
                            Texture2D newSheet = new Texture2D(device, GraphicsManager.PortraitSize, GraphicsManager.PortraitSize);
                            newSheet.SetData(reverseColors);
                            sheets.Add(newSheet);
                        }
                    }
                }
                initTex.Dispose();
                if (sheets.Count == 0)
                    return null;

                int fullWidth = (int)Math.Ceiling(Math.Sqrt(sheets.Count));

                Texture2D tex = new Texture2D(device, GraphicsManager.PortraitSize * fullWidth, GraphicsManager.PortraitSize * fullWidth);
                for (int ii = 0; ii < sheets.Count; ii++)
                {
                    BaseSheet.Blit(sheets[ii], tex, 0, 0, sheets[ii].Width, sheets[ii].Height, ii % fullWidth * GraphicsManager.PortraitSize, ii / fullWidth * GraphicsManager.PortraitSize);
                    sheets[ii].Dispose();
                }
                return new PortraitSheet(tex, animData);
            }
            else
            {
                //load all available tilesets
                //get all frames
                Dictionary<int, PortraitData> animData = new Dictionary<int, PortraitData>();
                List<Texture2D> sheets = new List<Texture2D>();
                for (int ii = 0; ii < GraphicsManager.Emotions.Count; ii++)
                {
                    string emotion = GraphicsManager.Emotions[ii].Name;
                    if (File.Exists(baseDirectory + emotion + ".png"))
                    {
                        bool hasReverse = File.Exists(baseDirectory + emotion + "^.png");
                        {
                            Texture2D newSheet = null;
                            using (FileStream fileStream = new FileStream(baseDirectory + emotion + ".png", FileMode.Open, FileAccess.Read, FileShare.Read))
                                newSheet = ImportTex(fileStream);

                            animData.Add(ii, new PortraitData(sheets.Count, hasReverse));
                            if (newSheet.Width != GraphicsManager.PortraitSize || newSheet.Height != GraphicsManager.PortraitSize)
                                throw new InvalidOperationException(String.Format("{0} has incorrect dimensions. Must be {1}x{1}", baseDirectory + emotion + ".png", GraphicsManager.PortraitSize));
                            sheets.Add(newSheet);
                        }

                        if (hasReverse)
                        {
                            Texture2D newSheet = null;
                            using (FileStream fileStream = new FileStream(baseDirectory + emotion + "^.png", FileMode.Open, FileAccess.Read, FileShare.Read))
                                newSheet = ImportTex(fileStream);

                            if (newSheet.Width != GraphicsManager.PortraitSize || newSheet.Height != GraphicsManager.PortraitSize)
                                throw new InvalidOperationException(String.Format("{0} has incorrect dimensions. Must be {1}x{1}", baseDirectory + emotion + ".png", GraphicsManager.PortraitSize));
                            sheets.Add(newSheet);
                        }
                    }
                }
                if (sheets.Count == 0)
                    return null;

                int fullWidth = (int)Math.Ceiling(Math.Sqrt(sheets.Count));

                Texture2D tex = new Texture2D(device, GraphicsManager.PortraitSize * fullWidth, GraphicsManager.PortraitSize * fullWidth);
                for (int ii = 0; ii < sheets.Count; ii++)
                {
                    BaseSheet.Blit(sheets[ii], tex, 0, 0, sheets[ii].Width, sheets[ii].Height, ii % fullWidth * GraphicsManager.PortraitSize, ii / fullWidth * GraphicsManager.PortraitSize);
                    sheets[ii].Dispose();
                }
                return new PortraitSheet(tex, animData);
            }
        }

        public static void Export(PortraitSheet sheet, string baseDirectory, bool singleFrames)
        {
            if (singleFrames)
            {
                int fullWidth = (int)Math.Ceiling(Math.Sqrt(GraphicsManager.Emotions.Count));
                int fullHeight = (GraphicsManager.Emotions.Count - 1) / fullWidth + 1;

                bool includeReverse = false;
                foreach (PortraitData data in sheet.emoteMap.Values)
                {
                    if (data.HasReverse)
                    {
                        includeReverse = true;
                        break;
                    }
                }

                Texture2D outTex = new Texture2D(device, GraphicsManager.PortraitSize * fullWidth, GraphicsManager.PortraitSize * fullHeight * (includeReverse ? 2 : 1));

                foreach (int emoteIndex in sheet.emoteMap.Keys)
                {
                    string emotion = GraphicsManager.Emotions[emoteIndex].Name;

                    int ii = sheet.emoteMap[emoteIndex].Position;
                    int xx = emoteIndex % fullWidth;
                    int yy = emoteIndex / fullWidth;
                    {
                        BaseSheet.Blit(sheet.baseTexture, outTex, ii % sheet.TotalX * GraphicsManager.PortraitSize, ii / sheet.TotalX * GraphicsManager.PortraitSize, GraphicsManager.PortraitSize, GraphicsManager.PortraitSize, xx * GraphicsManager.PortraitSize, yy * GraphicsManager.PortraitSize);
                    }

                    if (sheet.emoteMap[emoteIndex].HasReverse)
                    {
                        ii++;
                        yy += fullHeight;
                        BaseSheet.Blit(sheet.baseTexture, outTex, ii % sheet.TotalX * GraphicsManager.PortraitSize, ii / sheet.TotalX * GraphicsManager.PortraitSize, GraphicsManager.PortraitSize, GraphicsManager.PortraitSize, xx * GraphicsManager.PortraitSize, yy * GraphicsManager.PortraitSize);
                    }
                }

                using (Stream stream = new FileStream(baseDirectory + "Sheet.png", FileMode.Create, FileAccess.Write, FileShare.None))
                    ExportTex(stream, outTex);
            }
            else
            {
                foreach (int emoteIndex in sheet.emoteMap.Keys)
                {
                    string emotion = GraphicsManager.Emotions[emoteIndex].Name;

                    int ii = sheet.emoteMap[emoteIndex].Position;
                    {
                        Texture2D tex = new Texture2D(device, GraphicsManager.PortraitSize, GraphicsManager.PortraitSize);
                        BaseSheet.Blit(sheet.baseTexture, tex, ii % sheet.TotalX * GraphicsManager.PortraitSize, ii / sheet.TotalX * GraphicsManager.PortraitSize, GraphicsManager.PortraitSize, GraphicsManager.PortraitSize, 0, 0);
                        using (Stream stream = new FileStream(baseDirectory + emotion + ".png", FileMode.Create, FileAccess.Write, FileShare.None))
                            ExportTex(stream, tex);
                        tex.Dispose();
                    }

                    if (sheet.emoteMap[emoteIndex].HasReverse)
                    {
                        ii++;
                        Texture2D tex2 = new Texture2D(device, GraphicsManager.PortraitSize, GraphicsManager.PortraitSize);
                        BaseSheet.Blit(sheet.baseTexture, tex2, ii % sheet.TotalX * GraphicsManager.PortraitSize, ii / sheet.TotalX * GraphicsManager.PortraitSize, GraphicsManager.PortraitSize, GraphicsManager.PortraitSize, 0, 0);
                        using (Stream stream = new FileStream(baseDirectory + emotion + "^.png", FileMode.Create, FileAccess.Write, FileShare.None))
                            ExportTex(stream, tex2);
                        tex2.Dispose();
                    }
                }
            }
        }

        public static new PortraitSheet Load(BinaryReader reader)
        {
            long length = reader.ReadInt64();
            Texture2D tex = null;
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] data = reader.ReadBytes((int)length);
                ms.Write(data, 0, (int)length);
                ms.Position = 0;
                tex = Texture2D.FromStream(device, ms);
            }

            Dictionary<int, PortraitData> animData = new Dictionary<int, PortraitData>();

            reader.ReadInt32();//don't need these
            reader.ReadInt32();//don't need these

            int keyCount = reader.ReadInt32();
            for (int ii = 0; ii < keyCount; ii++)
            {
                int emoteIndex = reader.ReadInt32();
                int index = reader.ReadInt32();
                bool reverse = reader.ReadBoolean();
                animData.Add(emoteIndex, new PortraitData(index, reverse));
            }

            return new PortraitSheet(tex, animData);
        }

        public static new PortraitSheet LoadError()
        {
            return new PortraitSheet(defaultTex, defaultTex.Width, defaultTex.Height, new Dictionary<int, PortraitData>());
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(emoteMap.Keys.Count);
            foreach (int emotion in emoteMap.Keys)
            {
                writer.Write(emotion);
                writer.Write(emoteMap[emotion].Position);
                writer.Write(emoteMap[emotion].HasReverse);
            }
        }

        public int GetReferencedEmoteIndex(int type)
        {
            int fallbackIndex = -1;
            EmotionType emoteData = GraphicsManager.Emotions[type];
            while (!emoteMap.ContainsKey(type))
            {
                fallbackIndex++;
                if (fallbackIndex < emoteData.Fallbacks.Count)
                    type = emoteData.Fallbacks[fallbackIndex];
                else
                    return -1;
            }

            return type;
        }

        //need a way to determine frame the old fashioned way,
        //however, also need a way to determine frame for an animation playing at the true specified speed
        public void DrawPortrait(SpriteBatch spriteBatch, Vector2 pos, EmoteStyle type)
        {
            type.Emote = GetReferencedEmoteIndex(type.Emote);

            if (emoteMap.ContainsKey(type.Emote))
            {
                int index = emoteMap[type.Emote].Position;
                bool flip = false;
                if (type.Reverse)
                {
                    if (emoteMap[type.Emote].HasReverse)
                        index++;
                    else
                        flip = true;
                }
                DrawTile(spriteBatch, pos, index % TotalX, index / TotalX, Color.White, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            }
            else
                DrawDefault(spriteBatch, new Rectangle((int)pos.X, (int)pos.Y, GraphicsManager.PortraitSize, GraphicsManager.PortraitSize));
        }

        public bool HasEmotion(int type)
        {
            return emoteMap.ContainsKey(type);
        }

    }
}
