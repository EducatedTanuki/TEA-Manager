using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEngine.SceneManagement;

namespace TEA {
 [CustomEditor(typeof(AvatarController))]
 public class TEA_AvatarController_Editor : Editor {
  bool _show;

  public override void OnInspectorGUI() {
   if(GUILayout.Button("Tanukis Only"))
    _show=!_show;

   if(_show)
    base.OnInspectorGUI();
  }
 }
}