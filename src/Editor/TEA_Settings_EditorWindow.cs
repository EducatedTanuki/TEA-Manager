using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TEA {
 public class TEA_Settings_EditorWindow : EditorWindow {
  [MenuItem("TEA Manager/Settings", false, 1)]
  public static void OpenWindow() {
   TEA_Settings settings = TEA_EditorUtility.GetTEA_Settings();
   TEA_Settings_EditorWindow window = EditorWindow.GetWindow(typeof(TEA_Settings_EditorWindow), false, "TEA Settings", true) as TEA_Settings_EditorWindow;
   window.minSize=new Vector2(300, 300);
   window.editor=Editor.CreateEditorWithContext(new Object[] { settings }, settings);
  }

  Editor editor;
  Vector2 scrollPosition;

  private void OnGUI() {
   scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
   editor.OnInspectorGUI();
   EditorGUILayout.EndScrollView();
  }
 }
}