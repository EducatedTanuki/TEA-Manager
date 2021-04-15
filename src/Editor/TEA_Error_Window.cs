using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static TEA.TEA_ValidationIssues;

namespace TEA {
 public class TEA_Error_Window : EditorWindow {

  public static void Open(List<TEA_ValidationIssues> issues) {
   TEA_Error_Window window = GetWindow<TEA_Error_Window>(true, $"Validation Issues");
   window.minSize=new Vector2(400, 700);

   window.issues=issues;
   int count = 0;
   foreach(TEA_ValidationIssues issue in issues) {
    if(count==0)
     window.foldout.Add(true);
    else
     window.foldout.Add(false);

    window.serializedObjects.Add(new SerializedObject(issue));
    count++;
   }
  }

  private static readonly int FONT_SIZE = 24;
  private static readonly int FONT_SIZE1 = 20;

  List<TEA_ValidationIssues> issues;
  List<SerializedObject> serializedObjects = new List<SerializedObject>();
  List<bool> foldout = new List<bool>();
  Vector2 scrollPos;
  GUIStyle headerStyle;
  GUIStyle header1Style;

  private void OnGUI() {
   headerStyle=new GUIStyle() {
    fontSize=FONT_SIZE,
    fontStyle=FontStyle.Bold,
    fixedHeight=FONT_SIZE,
    stretchHeight=true
   };

   header1Style=new GUIStyle() {
    fontSize=FONT_SIZE1,
    fontStyle=FontStyle.Bold,
    fixedHeight=FONT_SIZE1,
    stretchHeight=true
   };

   scrollPos=EditorGUILayout.BeginScrollView(scrollPos);

   int count = 0;
   foreach(TEA_ValidationIssues avatarIssue in issues) {
    EditorGUILayout.LabelField(avatarIssue.AvatarName, headerStyle, GUILayout.Height(FONT_SIZE));
    foldout[count]=EditorGUILayout.Foldout(foldout[count], "show/hide", true, EditorStyles.boldLabel);

    if(foldout[count])
     DrawIssue(avatarIssue, serializedObjects[count]);

    count++;
    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
   }

   EditorGUILayout.EndScrollView();
  }

  private void DrawIssue(TEA_ValidationIssues avatarIssue, SerializedObject sObj) {
   EditorGUI.indentLevel++;
   EditorGUILayout.HelpBox($"Validation issues found for [{avatarIssue.AvatarName}].\nDouble click Reference to see in Inspector.", MessageType.Warning);

   foreach(FieldInfo field in typeof(TEA_ValidationIssues).GetFields()) {
    SerializedProperty currentProperty = sObj.FindProperty(field.Name);
    if(null==currentProperty) {
     Debug.Log($"{field.Name} is null");
     continue;
    }

    if(field.Name=="AvatarName")
     continue;

    if(currentProperty.isArray) {
     Look(currentProperty, true);
    }

   }
   EditorGUI.indentLevel--;
  }

  protected void Look(SerializedProperty prop, bool drawChildren) {
   string lastPropPath = string.Empty;

   int count = 0;
   foreach(SerializedProperty p in prop) {
    count++;
    bool isParent = !prop.propertyPath.Contains(".");

    if(isParent&&count>1)
     EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

    if(count==1) {
     if(!isParent) {
      EditorGUILayout.LabelField(prop.displayName, EditorStyles.label);
     } else {
      EditorGUILayout.LabelField(prop.displayName, header1Style, GUILayout.Height(FONT_SIZE1));
      EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
     }
     EditorGUI.indentLevel++;
    }

    EditorGUI.indentLevel++;
    if(p.isArray) {
     Look(p, true);
    } else if(p.type==typeof(TEA_ValidationIssues.Issue).Name) {

     SerializedProperty cause = p.FindPropertyRelative("Cause");
     if(null!=cause)
      EditorGUILayout.LabelField($"{cause.stringValue}", EditorStyles.boldLabel);

     SerializedProperty reff = p.FindPropertyRelative("Reference");
     EditorGUI.indentLevel++;
     if(null!=reff)
      Look(reff, true);
     EditorGUI.indentLevel--;

    } else {
     EditorGUILayout.PropertyField(p);
    }

    EditorGUI.indentLevel--;
   }//for

   EditorGUILayout.Space();
   if(count>0)
    EditorGUI.indentLevel--;
  }

 }//class
}//namespace
