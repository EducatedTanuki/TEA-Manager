using UnityEditor;
using UnityEngine;

namespace TEA {
 public class ExtendedEditorWindow : EditorWindow{
  protected SerializedObject serializedObject;
  protected SerializedProperty currentProperty;

  protected void DrawProperties(SerializedProperty prop, bool drawChildren) {
   string lastPropPath = string.Empty;

   foreach(SerializedProperty p in prop) {
    if(p.isArray&&p.propertyType==SerializedPropertyType.Generic) {
     EditorGUILayout.BeginHorizontal();
     p.isExpanded=EditorGUILayout.Foldout(p.isExpanded, p.displayName);
     EditorGUILayout.EndHorizontal();

     if(p.isExpanded) {
      EditorGUI.indentLevel++;
      DrawProperties(p, drawChildren);
      EditorGUI.indentLevel--;
     } else {
      if(!string.IsNullOrEmpty(lastPropPath)&&p.propertyPath.Contains(lastPropPath)) { continue; }
      lastPropPath=p.propertyPath;
      EditorGUILayout.PropertyField(p, drawChildren);
     }
    }
   }
  }
 }
}