﻿using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AABB;
using RogueEssence.Script;
using System.Runtime.Serialization;

namespace RogueEssence.Ground
{
    [Serializable]
    public class GroundObject : BaseTaskUser, IDrawableSprite, IObstacle
    {
        public IPlaceableAnimData ObjectAnim;
        public bool Passable;

        public IPlaceableAnimData CurrentAnim;
        public FrameTick AnimTime;
        public int Cycles;

        public uint Tags
        {
            get
            {
                if (!EntEnabled)
                    return 0u;

                if (Passable)
                    return 3u; // cross response
                else
                {
                    if (TriggerType == EEntityTriggerTypes.Touch || TriggerType == EEntityTriggerTypes.TouchOnce)
                        return 2u; // touch response
                    else
                        return 1u; // slide response
                }
            }
        }
        public Loc DrawOffset;

        public override Color DevEntColoring => Color.Chartreuse;

        public override EThink ThinkType => EThink.Never;


        public GroundObject()
        {
            ObjectAnim = new ObjAnimData();
            CurrentAnim = new ObjAnimData();
            EntName = "GroundObject" + ToString(); //!#FIXME : Give a default unique name please fix this when we have editor/template names!
            SetTriggerType(EEntityTriggerTypes.Action);
        }

        public GroundObject(IPlaceableAnimData anim, Dir8 dir, Rect collider, Loc drawOffset, EEntityTriggerTypes triggerty, string entname)
        {
            ObjectAnim = anim;
            CurrentAnim = new ObjAnimData();
            Collider = collider;
            DrawOffset = drawOffset;
            Direction = dir;
            SetTriggerType(triggerty);
            EntName = entname;
        }

        public GroundObject(ObjAnimData anim, Rect collider, EEntityTriggerTypes triggerty, string entname)
            :this(anim, Dir8.Down, collider, new Loc(), triggerty, entname)
        {}

        public GroundObject(ObjAnimData anim, Rect collider, Loc drawOffset, bool contact, string entname)
            : this(anim, Dir8.Down, collider, drawOffset, contact ? EEntityTriggerTypes.Touch : EEntityTriggerTypes.Action, entname)
        { }
        public GroundObject(ObjAnimData anim, Dir8 dir, Rect collider, Loc drawOffset, bool contact, string entname)
            : this(anim, dir, collider, drawOffset, contact ? EEntityTriggerTypes.Touch : EEntityTriggerTypes.Action, entname)
        { }
        public GroundObject(ObjAnimData anim, Rect collider, bool contact, string entname)
            : this(anim, collider, new Loc(), contact, entname)
        { }

        protected GroundObject(GroundObject other) : base(other)
        {
            ObjectAnim = (IPlaceableAnimData)other.ObjectAnim.Clone();
            CurrentAnim = new ObjAnimData();
            DrawOffset = other.DrawOffset;
            Passable = other.Passable;
        }

        public override GroundEntity Clone() { return new GroundObject(this); }



        public override IEnumerator<YieldInstruction> Interact(GroundEntity activator, TriggerResult result) //PSY: Set this value to get the entity that touched us/activated us
        {
            if (!EntEnabled)
                yield break;

            //Run script events
            if (GetTriggerType() == EEntityTriggerTypes.Action)
                yield return CoroutineManager.Instance.StartCoroutine(RunEvent(LuaEngine.EEntLuaEventTypes.Action, result, activator));
            else if (GetTriggerType() == EEntityTriggerTypes.Touch || GetTriggerType() == EEntityTriggerTypes.TouchOnce)
                yield return CoroutineManager.Instance.StartCoroutine(RunEvent(LuaEngine.EEntLuaEventTypes.Touch, result, activator));

        }

        public void StartAction(ObjAnimData anim, int cycles)
        {
            CurrentAnim = anim;
            Cycles = cycles;
            AnimTime = FrameTick.Zero;
        }


        public void Update(FrameTick elapsedTime)
        {
            if (CurrentAnim.AnimIndex != "")
            {
                AnimTime += elapsedTime;

                DirSheet sheet = GraphicsManager.GetObject(CurrentAnim.AnimIndex);
                int totalTime = CurrentAnim.GetTotalFrames(sheet.TotalFrames) * CurrentAnim.FrameTime * Cycles;
                //end animation if it is finished
                if (AnimTime.ToFrames() >= totalTime)
                {
                    AnimTime = FrameTick.Zero;
                    CurrentAnim = new ObjAnimData();
                    Cycles = 0;
                }
            }
        }

        public override void DrawDebug(SpriteBatch spriteBatch, Loc offset)
        {
            if (EntEnabled)
            {
                BaseSheet blank = GraphicsManager.Pixel;
                blank.Draw(spriteBatch, new Rectangle(Collider.X - offset.X, Collider.Y - offset.Y, Collider.Width, Collider.Height), null, Color.Cyan * 0.7f);
            }
        }
        public override void Draw(SpriteBatch spriteBatch, Loc offset)
        {
            if (CurrentAnim.AnimIndex != "")
            {
                Loc drawLoc = GetDrawLoc(offset);

                DirSheet sheet = GraphicsManager.GetDirSheet(CurrentAnim.AssetType, CurrentAnim.AnimIndex);
                sheet.DrawDir(spriteBatch, drawLoc.ToVector2(), CurrentAnim.GetCurrentFrame(AnimTime, sheet.TotalFrames), CurrentAnim.GetDrawDir(Direction), Color.White * ((float)CurrentAnim.Alpha / 255), CurrentAnim.AnimFlip);
            }
            else if (ObjectAnim.AnimIndex != "")
            {
                Loc drawLoc = GetDrawLoc(offset);

                DirSheet sheet = GraphicsManager.GetDirSheet(ObjectAnim.AssetType, ObjectAnim.AnimIndex);
                sheet.DrawDir(spriteBatch, drawLoc.ToVector2(), ObjectAnim.GetCurrentFrame(GraphicsManager.TotalFrameTick, sheet.TotalFrames), ObjectAnim.GetDrawDir(Direction), Color.White * ((float)ObjectAnim.Alpha / 255), ObjectAnim.AnimFlip);
            }
        }


        public override Loc GetDrawLoc(Loc offset)
        {
            return MapLoc - offset - DrawOffset;
        }

        public override Loc GetDrawSize()
        {
            DirSheet sheet = GraphicsManager.GetObject(ObjectAnim.AnimIndex);

            return new Loc(sheet.TileWidth, sheet.TileHeight);
        }

        public override EEntTypes GetEntityType()
        {
            return EEntTypes.Object;
        }

        public override bool DevHasGraphics()
        {
            if (ObjectAnim != null && ObjectAnim.AnimIndex != "")
                return true;
            else
                return false;
        }

        public override bool IsEventSupported(LuaEngine.EEntLuaEventTypes ev)
        {
            return ev != LuaEngine.EEntLuaEventTypes.Invalid && ev != LuaEngine.EEntLuaEventTypes.Think;
        }

        public override void LuaEngineReload()
        {
            ReloadEvents();
        }


        [OnDeserialized]
        internal new void OnDeserializedMethod(StreamingContext context)
        {
            scriptEvents = new Dictionary<LuaEngine.EEntLuaEventTypes, ScriptEvent>();

            //TODO: Created v0.5.3, delete on v0.6.1
            if (ObjectAnim != null)
            {
                Dir8 dir = ObjectAnim.AnimDir;
                if (dir != Dir8.None)
                {
                    Direction = dir;
                    ObjectAnim.AnimDir = Dir8.None;
                }
            }
        }
    }
}
