﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RogueEssence.Content;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using System.Drawing;
using RogueElements;
using Avalonia.Controls;
using RogueEssence.Dev.Views;
using RogueEssence.Dev.ViewModels;

namespace RogueEssence.Dev
{
    public class NoDupeListEditor : Editor<IList>
    {
        public override bool DefaultSubgroup => true;
        public override bool DefaultDecoration => false;
        public override bool DefaultType => true;


        public override Type GetAttributeType() { return typeof(NoDupeAttribute); }

        public override void LoadWindowControls(StackPanel control, string parent, string name, Type type, object[] attributes, IList member, Type[] subGroupStack)
        {
            Type elementType = ReflectionExt.GetBaseTypeArg(typeof(IList<>), member.GetType(), 0);

            CollectionBox lbxValue = new CollectionBox();

            EditorHeightAttribute heightAtt = ReflectionExt.FindAttribute<EditorHeightAttribute>(attributes);
            if (heightAtt != null)
                lbxValue.MaxHeight = heightAtt.Height;
            else
                lbxValue.MaxHeight = 180;

            CollectionBoxViewModel mv = new CollectionBoxViewModel(new StringConv(elementType, ReflectionExt.GetPassableAttributes(1, attributes)));
            lbxValue.DataContext = mv;

            //add lambda expression for editing a single element
            mv.OnEditItem += (int index, object element, CollectionBoxViewModel.EditElementOp op) =>
            {
                string elementName = name + "[" + index + "]";
                DataEditForm frmData = new DataEditForm();
                frmData.Title = DataEditor.GetWindowTitle(parent, elementName, element, elementType, ReflectionExt.GetPassableAttributes(1, attributes));

                //TODO: make this a member and reference it that way
                DataEditor.LoadClassControls(frmData.ControlPanel, parent, elementName, elementType, ReflectionExt.GetPassableAttributes(1, attributes), element, true, new Type[0]);

                frmData.SelectedOKEvent += async () =>
                {
                    object newElement = DataEditor.SaveClassControls(frmData.ControlPanel, elementName, elementType, ReflectionExt.GetPassableAttributes(1, attributes), true, new Type[0]);

                    bool itemExists = false;

                    List<object> states = (List<object>)mv.GetList(typeof(List<object>));
                    for (int ii = 0; ii < states.Count; ii++)
                    {
                        //ignore the current index being edited
                        //if the element is null, then we are editing a new object, so skip
                        if (ii != index || element == null)
                        {
                            if (states[ii].Equals(newElement))
                                itemExists = true;
                        }
                    }

                    if (itemExists)
                    {
                        await MessageBox.Show(control.GetOwningForm(), "Cannot add duplicate items.", "Entry already exists.", MessageBox.MessageBoxButtons.Ok);
                    }
                    else
                    {
                        op(index, newElement);
                        frmData.Close();
                    }
                };
                frmData.SelectedCancelEvent += () =>
                {
                    frmData.Close();
                };

                control.GetOwningForm().RegisterChild(frmData);
                frmData.Show();
            };

            List<object> states = new List<object>();
            foreach (object state in member)
                states.Add(state);
            mv.LoadFromList(states);
            control.Children.Add(lbxValue);
        }


        public override IList SaveWindowControls(StackPanel control, string name, Type type, object[] attributes, Type[] subGroupStack)
        {
            int controlIndex = 0;

            IControl lbxValue = control.Children[controlIndex];
            CollectionBoxViewModel mv = (CollectionBoxViewModel)lbxValue.DataContext;
            return mv.GetList(type);
        }
    }
}
