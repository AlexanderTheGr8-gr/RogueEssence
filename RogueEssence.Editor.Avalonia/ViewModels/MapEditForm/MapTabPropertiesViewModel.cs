﻿using Avalonia.Controls;
using ReactiveUI;
using RogueElements;
using RogueEssence.Content;
using RogueEssence.Data;
using RogueEssence.Dev.Views;
using RogueEssence.Dungeon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace RogueEssence.Dev.ViewModels
{
    public class MapTabPropertiesViewModel : ViewModelBase
    {
        public MapTabPropertiesViewModel()
        {
            Sights = new ObservableCollection<string>();
            for (int ii = 0; ii <= (int)Map.SightRange.Blind; ii++)
                Sights.Add(((Map.SightRange)ii).ToLocal());

            Elements = new ObservableCollection<string>();
            Dictionary<string, string> element_names = DataManager.Instance.DataIndices[DataManager.DataType.Element].GetLocalStringArray(true);
            keys = new List<string>();

            foreach (string key in element_names.Keys)
            {
                keys.Add(key);
                Elements.Add(key + ": " + element_names[key]);
            }

            ScrollEdges = new ObservableCollection<string>();
            for (int ii = 0; ii <= (int)Map.ScrollEdge.Wrap; ii++)
                ScrollEdges.Add(((Map.ScrollEdge)ii).ToLocal());

            BG = new ClassBoxViewModel(new StringConv(typeof(IBackgroundSprite), new object[0]));
            BG.OnMemberChanged += BG_Changed;
            BG.OnEditItem += MapBG_Edit;
            BlankBG = new TileBoxViewModel();
            BlankBG.OnMemberChanged += BlankBG_Changed;
            BlankBG.OnEditItem += AutoTile_Edit;

            DevForm form = (DevForm)DiagManager.Instance.DevEditor;
            TextureMap = new DictionaryBoxViewModel(form.MapEditForm, new StringConv(typeof(AutoTile), new object[0]));
            TextureMap.OnMemberChanged += TextureMap_Changed;
            TextureMap.OnEditKey += TextureMap_EditKey;
            TextureMap.OnEditItem += TextureMap_EditItem;

            Music = new ObservableCollection<string>();
            reloadMusic();
        }



        public string MapName
        {
            get { return ZoneManager.Instance.CurrentMap.Name.DefaultText; }
            set
            {
                lock (GameBase.lockObj)
                    this.RaiseAndSet(ref ZoneManager.Instance.CurrentMap.Name.DefaultText, value);
            }
        }

        public ObservableCollection<string> Sights { get; }

        public int ChosenTileSight
        {
            get { return (int)ZoneManager.Instance.CurrentMap.TileSight; }
            set
            {
                ZoneManager.Instance.CurrentMap.TileSight = (Map.SightRange)value;
                this.RaisePropertyChanged();
            }
        }
        public int ChosenCharSight
        {
            get { return (int)ZoneManager.Instance.CurrentMap.CharSight; }
            set
            {
                ZoneManager.Instance.CurrentMap.CharSight = (Map.SightRange)value;
                this.RaisePropertyChanged();
            }
        }


        private List<string> keys;

        public ObservableCollection<string> Elements { get; }

        public int ChosenElement
        {
            get { return keys.IndexOf(ZoneManager.Instance.CurrentMap.Element); }
            set
            {
                ZoneManager.Instance.CurrentMap.Element = keys[value];
                this.RaisePropertyChanged();
            }
        }


        public ObservableCollection<string> ScrollEdges { get; }

        public int ChosenScroll
        {
            get { return (int)ZoneManager.Instance.CurrentMap.EdgeView; }
            set
            {
                lock (GameBase.lockObj)
                {
                    ZoneManager.Instance.CurrentMap.EdgeView = (Map.ScrollEdge)value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public ClassBoxViewModel BG { get; set; }
        public TileBoxViewModel BlankBG { get; set; }

        public DictionaryBoxViewModel TextureMap { get; set; }

        public ObservableCollection<string> Music { get; }

        private int chosenMusic;
        public int ChosenMusic
        {
            get { return chosenMusic; }
            set
            {
                this.SetIfChanged(ref chosenMusic, value);
                musicChanged();
            }
        }

        public void BG_Changed()
        {
            ZoneManager.Instance.CurrentMap.Background = BG.GetObject<IBackgroundSprite>();
        }

        public void MapBG_Edit(object element, bool advancedEdit, ClassBoxViewModel.EditElementOp op)
        {
            Type type = typeof(IBackgroundSprite);
            string elementName = type.Name;
            DataEditForm frmData = new DataEditRootForm();
            frmData.Title = DataEditor.GetWindowTitle(ZoneManager.Instance.CurrentMap.AssetName, elementName, element, type, new object[0]);

            DataEditor.LoadClassControls(frmData.ControlPanel, ZoneManager.Instance.CurrentMap.AssetName, null, elementName, type, new object[0], element, true, new Type[0], advancedEdit);
            DataEditor.TrackTypeSize(frmData, type);

            frmData.SelectedOKEvent += async () =>
            {
                element = DataEditor.SaveClassControls(frmData.ControlPanel, elementName, type, new object[0], true, new Type[0], advancedEdit);
                op(element);
                return true;
            };

            DevForm form = (DevForm)DiagManager.Instance.DevEditor;
            form.MapEditForm.RegisterChild(frmData);
            frmData.Show();
        }

        public void BlankBG_Changed()
        {
            ZoneManager.Instance.CurrentMap.BlankBG = BlankBG.Tile;
        }

        public void AutoTile_Edit(AutoTile element, TileBoxViewModel.EditElementOp op)
        {
            TileEditForm frmData = new TileEditForm();
            TileEditViewModel tmv = new TileEditViewModel();
            frmData.DataContext = tmv;
            tmv.Name = element.ToString();

            //load as if eyedropping
            tmv.TileBrowser.TileSize = GraphicsManager.TileSize;
            tmv.AutotileBrowser.TileSize = GraphicsManager.TileSize;
            tmv.LoadTile(element);

            tmv.SelectedOKEvent += async () =>
            {
                element = tmv.GetTile();
                op(element);
                frmData.Close();
            };
            tmv.SelectedCancelEvent += () =>
            {
                frmData.Close();
            };

            DevForm form = (DevForm)DiagManager.Instance.DevEditor;
            form.MapEditForm.RegisterChild(frmData);
            frmData.Show();
        }


        public void TextureMap_Changed()
        {
            ZoneManager.Instance.CurrentMap.TextureMap = TextureMap.GetDict<Dictionary<string, AutoTile>>();
            ZoneManager.Instance.CurrentMap.CalculateTerrainAutotiles(Loc.Zero, new Loc(ZoneManager.Instance.CurrentMap.Width, ZoneManager.Instance.CurrentMap.Height));
        }

        public void TextureMap_EditKey(object key, object element, bool advancedEdit, DictionaryBoxViewModel.EditElementOp op)
        {
            string elementName = "TextureMap<Key>";
            DataEditForm frmKey = new DataEditRootForm();
            frmKey.Title = DataEditor.GetWindowTitle(ZoneManager.Instance.CurrentMap.AssetName, elementName, element, typeof(string), new object[0]);

            DataTypeAttribute attr = new DataTypeAttribute(1, DataManager.DataType.Terrain, false);
            DataEditor.LoadClassControls(frmKey.ControlPanel, ZoneManager.Instance.CurrentMap.AssetName, null, elementName, typeof(string), new object[1] { attr }, key, true, new Type[0], advancedEdit);
            DataEditor.TrackTypeSize(frmKey, typeof(int));

            frmKey.SelectedOKEvent += async () =>
            {
                object newKey = DataEditor.SaveClassControls(frmKey.ControlPanel, elementName, typeof(string), new object[1] { attr }, true, new Type[0], advancedEdit);
                op(key, newKey, element);
                return true;
            };

            DevForm form = (DevForm)DiagManager.Instance.DevEditor;
            form.MapEditForm.RegisterChild(frmKey);
            frmKey.Show();
        }

        public void TextureMap_EditItem(object key, object element, bool advancedEdit, DictionaryBoxViewModel.EditElementOp op)
        {
            string elementName = "TextureMap[" + key.ToString() + "]";
            DataEditForm frmData = new DataEditRootForm();
            frmData.Title = DataEditor.GetWindowTitle(ZoneManager.Instance.CurrentMap.AssetName, elementName, element, typeof(AutoTile), new object[0]);

            DataEditor.LoadClassControls(frmData.ControlPanel, ZoneManager.Instance.CurrentMap.AssetName, null, elementName, typeof(AutoTile), new object[0], element, true, new Type[0], advancedEdit);
            DataEditor.TrackTypeSize(frmData, typeof(AutoTile));

            frmData.SelectedOKEvent += async () =>
            {
                element = DataEditor.SaveClassControls(frmData.ControlPanel, elementName, typeof(AutoTile), new object[0], true, new Type[0], advancedEdit);
                op(key, key, element);
                return true;
            };

            DevForm form = (DevForm)DiagManager.Instance.DevEditor;
            form.MapEditForm.RegisterChild(frmData);
            frmData.Show();
        }


        public void btnReloadMusic_Click()
        {
            reloadMusic();
        }

        private void reloadMusic()
        {
            if (Design.IsDesignMode)
                return;
            lock (GameBase.lockObj)
            {
                Music.Clear();

                string[] files = PathMod.GetModFiles(GraphicsManager.MUSIC_PATH, "*.ogg");

                Music.Add("None");
                for (int ii = 0; ii < files.Length; ii++)
                {
                    string song = Path.GetFileName(files[ii]);
                    Music.Add(song);
                    if (song == ZoneManager.Instance.CurrentMap.Music)
                        ChosenMusic = ii+1;
                }
            }
        }

        private void musicChanged()
        {
            lock (GameBase.lockObj)
            {
                if (chosenMusic > 0)
                {
                    string fileName = (string)Music[chosenMusic];
                    ZoneManager.Instance.CurrentMap.Music = fileName;
                }
                else
                    ZoneManager.Instance.CurrentMap.Music = "";

                GameManager.Instance.BGM(ZoneManager.Instance.CurrentMap.Music, false);
            }
        }

        public void LoadMapProperties()
        {
            MapName = MapName;
            ChosenTileSight = ChosenTileSight;
            ChosenCharSight = ChosenCharSight;
            ChosenElement = ChosenElement;
            ChosenScroll = ChosenScroll;

            BG.LoadFromSource(ZoneManager.Instance.CurrentMap.Background);
            BlankBG.LoadFromSource(ZoneManager.Instance.CurrentMap.BlankBG);
            TextureMap.LoadFromDict(ZoneManager.Instance.CurrentMap.TextureMap);

            bool foundSong = false;
            for (int ii = 0; ii < Music.Count; ii++)
            {
                string song = Music[ii];
                if (song == ZoneManager.Instance.CurrentMap.Music)
                {
                    ChosenMusic = ii;
                    foundSong = true;
                    break;
                }
            }
            if (!foundSong)
                ChosenMusic = -1;
        }


    }
}
