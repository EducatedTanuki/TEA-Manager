using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace TEA {
 public class TEA_Play_Tab : EditorWindow {
  // ----- ----- Static ----- -----
  // --- height ---
  private static readonly int MIN_HEIGHT = 20;
  private static readonly int SEPARATOR_WIDTH = 10;
  private static readonly int SECTION_WIDTH = 180;

  [MenuItem("TEA Manager/Play Tab")]
  static void OpenWindow() {
   EditorWindow window = EditorWindow.GetWindow(typeof(TEA_Play_Tab), false, "TEA Manager", true);
   window.minSize=new Vector2(512, MIN_HEIGHT);
  }

  // ----- ----- Instance ----- -----
  //--- Avatar ---
  private Dictionary<string, VRCAvatarDescriptor> avatars = new Dictionary<string, VRCAvatarDescriptor>();
  public string[] avatarKeys;
  public int avatarIndex = 0;

  // ----- ----- Compiler ----- -----
  private TEA_Compiler compiler = new TEA_Compiler();

  // --- gui ---
  Texture2D play;
  Texture2D stop;

  private void OnEnable() {
   play = EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/play.png") as Texture2D;
   stop=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/stop.png") as Texture2D;
  }


  private void OnGUI() {
   EditorGUILayout.BeginHorizontal(new GUIStyle {
    alignment=TextAnchor.UpperLeft,
   });
   //------

   List<TEA_Manager> managers = TEA_Manager_Editor.GetManagers();

   if(managers.Count>1) {
    EditorGUILayout.HelpBox($"Only one TEA Manager can be loaded at a time {TEA_Manager_Editor.GetManagerList(managers)}", MessageType.Error);
    EditorGUILayout.EndHorizontal();
    return;
   } else if(0==managers.Count) {
    EditorGUILayout.HelpBox($"No Tea Manager", MessageType.Warning);
    if(GUILayout.Button("Add To Scene", GUILayout.Height(MIN_HEIGHT))) {
     GameObject newManager = EditorGUIUtility.Load(TEA_Manager_Editor.PREFAB) as GameObject;
     Instantiate(newManager);
    }
    EditorGUILayout.EndHorizontal();
    return;
   }

   TEA_Manager manager = managers[0];
   if(PrefabUtility.IsAnyPrefabInstanceRoot(manager.gameObject))
    PrefabUtility.UnpackPrefabInstance(manager.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

   Dictionary<string, VRCAvatarDescriptor> newAvatars = AvatarController.GetAvatars(manager.gameObject.scene);
   if(avatars.Count!=newAvatars.Count) {
    manager.Avatar=null;
    avatarIndex=0;
   } else {
    foreach(KeyValuePair<string, VRCAvatarDescriptor> key in avatars) {
     if(!(newAvatars.TryGetValue(key.Key, out VRCAvatarDescriptor value)&&value==key.Value)) {
      manager.Avatar=null;
      avatarIndex=0;
      break;
     }
    }
   }
   avatars=newAvatars;
   avatarKeys=avatars.Keys.ToArray();
   ArrayUtility.Insert<string>(ref avatarKeys, 0, "- none -");
   if(avatars.Count>0&&(null==manager.Avatar||(avatarIndex>0&&avatars[avatarKeys[avatarIndex]]!=manager.Avatar))) {
    avatarIndex=1;
    manager.SetupComponents(avatars[avatarKeys[avatarIndex]]);
   }
   
   if(avatars.Count==0) {
    EditorGUILayout.HelpBox("No Avatars Found", MessageType.Info);
    manager.Avatar=null;
    //TODO add list of potential avatar
   } else {
    // --- Avatar Setup ---
    compiler.validate=EditorGUILayout.Toggle(new GUIContent("Validate", "Validate layers adhere to VRC 3.0 SDK"), compiler.validate, EditorStyles.toggle, GUILayout.MaxWidth(SECTION_WIDTH), GUILayout.ExpandWidth(false));
    EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH));

    EditorGUI.BeginChangeCheck();
    EditorGUILayout.LabelField("Avatar:", EditorStyles.boldLabel, GUILayout.MaxWidth(70), GUILayout.ExpandWidth(false));
    avatarIndex=EditorGUILayout.Popup("", avatarIndex, avatarKeys, EditorStyles.popup, GUILayout.MaxWidth(SECTION_WIDTH+50), GUILayout.ExpandWidth(false));
    if(EditorGUI.EndChangeCheck()) {
     if(avatarIndex>0) {
      manager.SetupComponents(avatars[avatarKeys[avatarIndex]]);
     } else
      manager.Avatar=null;
    }

    EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH));
    if(compiler.validate&&!EditorApplication.isPlaying) {
     if(GUILayout.Button(play, EditorStyles.miniButtonLeft, GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(SECTION_WIDTH), GUILayout.ExpandWidth(false))) {
      compiler.CompileAnimators(avatars, manager);
      if(!compiler.validationIssue)
       EditorApplication.isPlaying=true;
     }
    } else if(!compiler.validate&&!EditorApplication.isPlaying) {
     if(GUILayout.Button(play, GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(SECTION_WIDTH), GUILayout.ExpandWidth(false))) {
      EditorApplication.isPlaying=true;
     }
    } else if(GUILayout.Button(stop, GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(SECTION_WIDTH), GUILayout.ExpandWidth(false))) {
     EditorApplication.isPlaying=false;
    }
    if(compiler.validate&&!EditorApplication.isPlaying) {
     if(GUILayout.Button("Compile", EditorStyles.miniButtonRight, GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(SECTION_WIDTH), GUILayout.ExpandWidth(false))) {
      compiler.CompileAnimators(avatars, manager);
      if(!compiler.validationIssue)
       EditorApplication.isPlaying=EditorUtility.DisplayDialog($"{avatarKeys[avatarIndex]}", "Avatar Compiled", "Play", "Continue");
     }
    }
   }

   //----------------
   EditorGUILayout.EndHorizontal();

   TEA_Manager_Editor.CleanLeanTween();
  }
 }//class
}//namespace
