using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace TEA {
 [CustomEditor(typeof(TEA_Settings))]
 public class TEA_Settings_Editor : Editor {

  public override void OnInspectorGUI() {
   base.OnInspectorGUI();
  }

 }
}