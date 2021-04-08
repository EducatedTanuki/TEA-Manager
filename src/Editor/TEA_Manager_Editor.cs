﻿using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEngine.SceneManagement;

namespace TEA {
 [CustomEditor(typeof(TEA_Manager))]
 public class TEA_Manager_Editor : Editor {
  // ----- ----- TAMS Editor ----- -----
  public static readonly string MENU_ITEM = "Tanuki's Avatar Management Suit";
  public static readonly string PREFAB = "Assets/TEA Manager/TEA Manager.prefab";
  private static GUIStyle noElementLayout;

  // ----- ----- Avatars ----- -----
  private Dictionary<string, VRCAvatarDescriptor> avatars = new Dictionary<string, VRCAvatarDescriptor>();
  public string[] avatarKeys;
  public int avatarIndex = 0;

  // ----- ----- Compiler ----- -----
  private TEA_Compiler compiler = new TEA_Compiler();

  // --- Banner ---
  private static GUIContent bannerContent = new GUIContent {
   image=null
  };

  public override void OnInspectorGUI() {
   // -- Window --
   noElementLayout=new GUIStyle() {
    alignment=TextAnchor.MiddleCenter,
    fixedWidth=532,
    stretchWidth=false
   };

   // -- Banner --
   var assets = AssetDatabase.FindAssets("TEA_Manager_Banner");
   if(null!=assets&&0<assets.Length)
    bannerContent.image=AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(assets[0]));
   else
    Debug.Log("banner is not found");

   EditorGUILayout.BeginVertical(new GUIStyle {
    padding=new RectOffset(10, 10, 5, 5),
    fixedWidth=532,
    alignment=TextAnchor.MiddleCenter
   });
   //----------------
   GUIStyle bannerSytel = new GUIStyle {
    border=GUI.skin.box.border,
    fixedWidth=512,
    fixedHeight=85,
    alignment=TextAnchor.MiddleCenter,
   };
   Rect bannerV = EditorGUILayout.BeginVertical(bannerSytel);
   EditorGUILayout.LabelField(MENU_ITEM);
   EditorGUI.DrawPreviewTexture(bannerV, bannerContent.image);
   EditorGUILayout.EndVertical();

   EditorGUILayout.Space();

   EditorGUILayout.BeginVertical();
   //------

   TEA_Manager manager = (TEA_Manager)target;
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

   if(!OneManagerLoaded(out string managerText)) {
    EditorGUILayout.HelpBox($"Only one TEA Manager can be loaded at a time {managerText}", MessageType.Warning);
   } else if(avatars.Count==0) {
    EditorGUILayout.LabelField("No Avatars Found", EditorStyles.boldLabel);
    manager.Avatar=null;
    //TODO add list of potential avatar
   } else {
    // --- Avatar Setup ---
    EditorGUI.BeginChangeCheck();
    avatarIndex=EditorGUILayout.Popup("Avatar", avatarIndex, avatarKeys, EditorStyles.popup);
    if(EditorGUI.EndChangeCheck()) {
     if(avatarIndex>0) {
      manager.SetupComponents(avatars[avatarKeys[avatarIndex]]);
     } else
      manager.Avatar=null;
    }

    compiler.validate=EditorGUILayout.Toggle(new GUIContent("Validate", "Validate layers adhere to VRC 3.0 SDK"), compiler.validate, EditorStyles.toggle);

    if(compiler.validate&&!EditorApplication.isPlaying) {
     if(GUILayout.Button("Play & Compile", GUILayout.Height(30))) {
      compiler.CompileAnimators(avatars, manager);
      if(!compiler.validationIssue)
       EditorApplication.isPlaying=true;
     }
    } else if(!compiler.validate&&!EditorApplication.isPlaying) {
     if(GUILayout.Button("Play", GUILayout.Height(30))) {
      EditorApplication.isPlaying=true;
     }
    } else if(GUILayout.Button("Stop", GUILayout.Height(30))) {
     EditorApplication.isPlaying=false;
    }
    if(compiler.validate&&!EditorApplication.isPlaying) {
     if(GUILayout.Button("Compile", GUILayout.Height(30))) {
      compiler.CompileAnimators(avatars, manager);
      if(!compiler.validationIssue)
       EditorApplication.isPlaying=EditorUtility.DisplayDialog($"{avatarKeys[avatarIndex]}", "Avatar Compiled", "Play", "Continue");
     }
    }
   }
   EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

   //------
   EditorGUILayout.EndVertical();

   //----------------
   EditorGUILayout.EndVertical();

   base.OnInspectorGUI();
  }

  public static bool OneManagerLoaded(out string output) {
   //Scene scene = Selection.activeGameObject.scene;
   List<TEA_Manager> managers = GetManagers();
   if(managers.Count>1) {
    string list = "";
    list=GetManagerList(managers);
    //EditorUtility.DisplayDialog("TEA Manager", $"Only one TEA Manager can be active {list}", "OK");
    output=list;
    return false;
   }
   output="";
   return true;
  }

  // ----- ----- Utility ----- -----
  public static string GetManagerList(List<TEA_Manager> managers) {
   string ret = "";
   foreach(TEA_Manager c in managers) {
    ret+="\n[";
    ret+=c.gameObject.scene.name+"/"+c.gameObject.name;
    ret+="]";
   }
   return ret;
  }

  public static List<TEA_Manager> GetManagers() {
   List<TEA_Manager> managers = new List<TEA_Manager>();
   for(int i = 0; i<SceneManager.sceneCount; i++) {
    Scene scene = SceneManager.GetSceneAt(i);
    if(!scene.isLoaded)
     continue;
    foreach(GameObject obj in scene.GetRootGameObjects()) {
     Component comp = obj.GetComponentInChildren(typeof(TEA_Manager), true);
     if(null!=comp) {
      managers.Add((TEA_Manager)comp);
     }
    }
   }
   return managers;
  }

  // ----- ----- Avatar Setup Methods ----- -----
  private static readonly string TEA_OBJECT_MENU = "TEA Functions";
  private static string EXPRESSION_FOLDER = "Expression Menus";
  private static string EXPRESSION_MENU = "Assets/TEA Manager/Resources/Expression Menu/ExpressionsMenu.asset";
  private static string EXPRESSION_PARAMETER = "Assets/TEA Manager/Resources/Expression Menu/ExpressionParameters.asset";

  private static string Animation_Folder = "Animation Controllers";

  private static string Base_Layer = "Assets/VRCSDK/Examples3/Animation/Controllers/vrc_AvatarV3LocomotionLayer.controller";
  private static string Additive_Layer = "Assets/TEA Manager/Resources/Animation/Controllers/Additive.controller";
  private static string Gesture_Layer = "Assets/VRCSDK/Examples3/Animation/Controllers/vrc_AvatarV3HandsLayer.controller";
  private static string Action_Layer = "Assets/TEA Manager/Resources/Animation/Controllers/Actions.controller";
  private static string FX_Layer = "Assets/TEA Manager/Resources/Animation/Controllers/FX.controller";

  // ----- Make Avatar Menu -----
  #region
  [MenuItem("GameObject/TEA Functions/Make Avatar 3.0", false, 0)]
  public static void MakeAvatar() {
   GameObject newAvatar = Selection.activeGameObject;
   VRCAvatarDescriptor vrcd = newAvatar.AddComponent<VRCAvatarDescriptor>();

   // ViewPort
   Transform leftEye = AvatarController.GetBone(vrcd, HumanBodyBones.LeftEye);
   Transform rightEye = AvatarController.GetBone(vrcd, HumanBodyBones.RightEye);
   Transform head = AvatarController.GetBone(vrcd, HumanBodyBones.Head);
   if(null!=leftEye&&null!=rightEye) {
    vrcd.ViewPosition=Vector3.Lerp(rightEye.position, leftEye.position, 0.5f);
    Debug.Log($"{leftEye.position.x} - {rightEye.position.x}");
   } else if(null!=leftEye)
    vrcd.ViewPosition=leftEye.position;
   else if(null!=rightEye)
    vrcd.ViewPosition=rightEye.position;
   else if(null!=head) {
    vrcd.ViewPosition=head.position;
   }

   // Eye Look
   if(leftEye&&rightEye) {
    vrcd.enableEyeLook=true;
    vrcd.customEyeLookSettings.leftEye=leftEye;
    vrcd.customEyeLookSettings.rightEye=rightEye;
    vrcd.customEyeLookSettings.eyesLookingDown=new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations();
    vrcd.customEyeLookSettings.eyesLookingDown.left=new Quaternion(0.3f, 0f, 0f, 1f);
    vrcd.customEyeLookSettings.eyesLookingDown.right=new Quaternion(0.3f, 0f, 0f, 1f);
    vrcd.customEyeLookSettings.eyesLookingDown.linked=false;
    vrcd.customEyeLookSettings.eyesLookingRight=new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations();
    vrcd.customEyeLookSettings.eyesLookingRight.left=new Quaternion(0f, 0.2f, 0f, 1f);
    vrcd.customEyeLookSettings.eyesLookingRight.right=new Quaternion(0f, 0.2f, 0f, 1f);
    vrcd.customEyeLookSettings.eyesLookingRight.linked=false;
    vrcd.customEyeLookSettings.eyesLookingLeft=new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations();
    vrcd.customEyeLookSettings.eyesLookingLeft.left=new Quaternion(0f, -0.2f, 0f, 1f);
    vrcd.customEyeLookSettings.eyesLookingLeft.right=new Quaternion(0f, -0.2f, 0f, 1f);
    vrcd.customEyeLookSettings.eyesLookingLeft.linked=false;
    vrcd.customEyeLookSettings.eyesLookingUp=new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations();
    vrcd.customEyeLookSettings.eyesLookingUp.left=new Quaternion(-0.2f, 0f, 0f, 1f);
    vrcd.customEyeLookSettings.eyesLookingUp.right=new Quaternion(-0.2f, 0f, 0f, 1f);
    vrcd.customEyeLookSettings.eyesLookingUp.linked=false;
   }

   // Lip Sync
   //AvatarDescriptorEditor3 editor = (AvatarDescriptorEditor3)Editor.CreateEditor(vrcd, typeof(AvatarDescriptorEditor3));
   //AutoDetectVisemes(vrcd);

   // Locomotion
   vrcd.autoLocomotion=false;

   // Folders
   string parentFolder = GetParent(newAvatar.scene.path, false);

   // Playable Layers
   string animation_folder = CreatePath(parentFolder, Animation_Folder);
   if(!AssetDatabase.IsValidFolder(animation_folder))
    AssetDatabase.CreateFolder(parentFolder, Animation_Folder);

   string bLayer = CreatePath(animation_folder, "BaseLayer.controller");
   AssetDatabase.CopyAsset(Base_Layer, bLayer);
   string addLayer = CreatePath(animation_folder, "AdditiveLayer.controller");
   AssetDatabase.CopyAsset(Additive_Layer, addLayer);
   string gLayer = CreatePath(animation_folder, "GestureLayer.controller");
   AssetDatabase.CopyAsset(Gesture_Layer, gLayer);
   string acLayer = CreatePath(animation_folder, "ActionLayer.controller");
   AssetDatabase.CopyAsset(Action_Layer, acLayer);
   string fxLayer = CreatePath(animation_folder, "FX.controller");
   AssetDatabase.CopyAsset(FX_Layer, fxLayer);

   vrcd.baseAnimationLayers[0].isDefault=false;
   vrcd.baseAnimationLayers[0].isEnabled=true;
   vrcd.baseAnimationLayers[0].animatorController=AssetDatabase.LoadAssetAtPath<AnimatorController>(bLayer);

   vrcd.baseAnimationLayers[1].isDefault=false;
   vrcd.baseAnimationLayers[1].isEnabled=true;
   vrcd.baseAnimationLayers[1].animatorController=AssetDatabase.LoadAssetAtPath<AnimatorController>(addLayer);

   vrcd.baseAnimationLayers[2].animatorController=AssetDatabase.LoadAssetAtPath<AnimatorController>(gLayer);
   vrcd.baseAnimationLayers[2].isDefault=false;
   vrcd.baseAnimationLayers[2].isEnabled=true;

   vrcd.baseAnimationLayers[3].animatorController=AssetDatabase.LoadAssetAtPath<AnimatorController>(acLayer);
   vrcd.baseAnimationLayers[3].isDefault=false;
   vrcd.baseAnimationLayers[3].isEnabled=true;

   vrcd.baseAnimationLayers[4].animatorController=AssetDatabase.LoadAssetAtPath<AnimatorController>(fxLayer);
   vrcd.baseAnimationLayers[4].isDefault=false;
   vrcd.baseAnimationLayers[4].isEnabled=true;
   vrcd.customizeAnimationLayers=true;

   // Expressions
   vrcd.customExpressions=true;
   string expression_folder = CreatePath(parentFolder, EXPRESSION_FOLDER);
   if(!AssetDatabase.IsValidFolder(expression_folder))
    AssetDatabase.CreateFolder(parentFolder, EXPRESSION_FOLDER);
   string em = CreatePath(expression_folder, "ExpressionsMenu.asset");
   string ep = CreatePath(expression_folder, "ExpressionParameters.asset");
   AssetDatabase.CopyAsset(EXPRESSION_MENU, em);
   vrcd.expressionsMenu=AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(em);
   AssetDatabase.CopyAsset(EXPRESSION_PARAMETER, ep);
   vrcd.expressionParameters=AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>(ep);
  }

  [MenuItem("GameObject/TEA Functions/Make Avatar 3.0", true, 0)]
  public static bool MakeAvatarCheck() {
   GameObject newAvatar = Selection.activeGameObject;
   VRCAvatarDescriptor descriptor = newAvatar.GetComponent<VRCAvatarDescriptor>();
   if(null!=descriptor) {
    EditorUtility.DisplayDialog("Make Avatar 3.0", $"{newAvatar.name} is already an avatar", "Cancel");
    return false;
   }

   Animator animator = newAvatar.GetComponent<Animator>();
   if(null==animator) {
    EditorUtility.DisplayDialog("Make Avatar 3.0", $"{newAvatar.name} does not have an Animator", "Cancel");
    return false;
   }
   string parentFolder = GetParent(newAvatar.scene.path, false);
   if(!AssetDatabase.IsValidFolder(parentFolder)) {
    EditorUtility.DisplayDialog("Make Avatar 3.0", $"The scene needs to be saved before you can use 'Make Avatar 3.0'", "OK");
    return false;
   }
   bool accept = false;
   if(null==animator.avatar) {
    accept=EditorUtility.DisplayDialog("Make Avatar 3.0", $"{newAvatar.name} does not have an Animator.Avatar", "Continue", "Cancel");
   }
   accept=EditorUtility.DisplayDialog("Make Avatar 3.0", $"This operation will create folders in\n[{parentFolder}]\nSome files may be overridden!", "Continue", "Cancel");
   return accept;
  }

  private static List<string> DetermineBlendShapeNames(VRCAvatarDescriptor avatarDescriptor) {
   List<string> blendShapeNames = new List<string>();
   avatarDescriptor.VisemeSkinnedMesh=avatarDescriptor.GetComponentInChildren<SkinnedMeshRenderer>();
   if(avatarDescriptor.VisemeSkinnedMesh!=null) {
    blendShapeNames.Add("-none-");
    for(int i = 0; i<avatarDescriptor.VisemeSkinnedMesh.sharedMesh.blendShapeCount; ++i)
     blendShapeNames.Add(avatarDescriptor.VisemeSkinnedMesh.sharedMesh.GetBlendShapeName(i));
   }
   return blendShapeNames;
  }

  private static void AutoDetectVisemes(VRCAvatarDescriptor avatarDescriptor) {
   // prioritize strict - but fallback to looser - naming and don't touch user-overrides

   List<string> blendShapes = DetermineBlendShapeNames(avatarDescriptor);
   blendShapes.Remove("-none-");

   for(int v = 0; v<avatarDescriptor.VisemeBlendShapes.Length; v++) {
    if(string.IsNullOrEmpty(avatarDescriptor.VisemeBlendShapes[v])) {
     string viseme = ((VRC.SDKBase.VRC_AvatarDescriptor.Viseme)v).ToString().ToLowerInvariant();

     foreach(string s in blendShapes) {
      if(s.ToLowerInvariant()=="vrc.v_"+viseme) {
       avatarDescriptor.VisemeBlendShapes[v]=s;
       goto next;
      }
     }
     foreach(string s in blendShapes) {
      if(s.ToLowerInvariant()=="v_"+viseme) {
       avatarDescriptor.VisemeBlendShapes[v]=s;
       goto next;
      }
     }
     foreach(string s in blendShapes) {
      if(s.ToLowerInvariant().EndsWith(viseme)) {
       avatarDescriptor.VisemeBlendShapes[v]=s;
       goto next;
      }
     }
     foreach(string s in blendShapes) {
      if(s.ToLowerInvariant()==viseme) {
       avatarDescriptor.VisemeBlendShapes[v]=s;
       goto next;
      }
     }
     foreach(string s in blendShapes) {
      if(s.ToLowerInvariant().Contains(viseme)) {
       avatarDescriptor.VisemeBlendShapes[v]=s;
       goto next;
      }
     }
     next:
     { }
    }

   }
   avatarDescriptor.lipSync=VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;
   //shouldRefreshVisemes = false;
  }
  #endregion

  // ----- ----- Utility Methods ----- -----
  [MenuItem("GameObject/TEA Functions/Create Toggle", false, 0)]
  public static void CreateToggle() {
   GameObject selected = Selection.activeGameObject;
   VRCAvatarDescriptor avatar = (VRCAvatarDescriptor)selected.GetComponentsInParent(typeof(VRCAvatarDescriptor), true)[0];
   int isActive = EditorUtility.DisplayDialogComplex($"Create Toggle for [{selected.name}]", "", "OFF", "ON", "BOTH");
   if(2==isActive) {
    CreateToggle(selected, avatar.transform, 0);
    CreateToggle(selected, avatar.transform, 1);
   } else
    CreateToggle(selected, avatar.transform, isActive);
  }

  private static void CreateToggle(GameObject gameObject, Transform parent, float value) {
   string parentFolder = GetParent(parent.gameObject.scene.path, false);
   string toggle_folder = CreatePath(parentFolder, "Toggles");
   if(!AssetDatabase.IsValidFolder(toggle_folder))
    AssetDatabase.CreateFolder(parentFolder, "Toggles");

   AnimationClip clip = new AnimationClip() {
    name=gameObject.name
   };
   clip.SetCurve(AnimationUtility.CalculateTransformPath(gameObject.transform, parent), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0.0f, 0.0f, value));
   string path = CreatePath(toggle_folder, gameObject.name+(value==1 ? "-ON.anim" : "-OFF.anim"));
   if(!AssetDatabase.LoadAssetAtPath<AnimationClip>(path))
    AssetDatabase.CreateAsset(clip, path);
   else
    EditorUtility.DisplayDialog($"Toggle Already Exists", $"[{path}]", "OK");
  }

  [MenuItem("GameObject/TEA Functions/Create Toggle", true, 0)]
  public static bool CreateToggleCheck() {
   GameObject selected = Selection.activeGameObject;
   Component[] avatars = selected.GetComponentsInParent(typeof(VRCAvatarDescriptor), true);
   if(0==avatars.Length)
    EditorUtility.DisplayDialog("Create Toggle", $"{selected.name} is not the child of an avatar", "Cancel");
   else if(1<avatars.Length)
    EditorUtility.DisplayDialog("Create Toggle", $"{selected.name} is child of multiple avatars", "Cancel");
   return avatars.Length==1;
  }

  // ----- ------ Utility ----- -----
  #region
  internal static string CreatePath(params string[] pathParts) {
   string path = "";
   foreach(string part in pathParts) {
    if(path.Length>0&&!path.EndsWith("/"))
     path+="/";
    path+=part;
   }
   return path;
  }

  internal static void CreateAsset(UnityEngine.Object sibling, UnityEngine.Object newAsset, string name) {
   AssetDatabase.CreateAsset(newAsset, GetAssetDirectory(sibling, true)+name);
  }

  internal static string GetAssetDirectory(UnityEngine.Object obj, bool keepSlash) {
   if(keepSlash)
    return Regex.Replace(AssetDatabase.GetAssetPath(obj), @"[^/]+\..*$", "");
   else
    return Regex.Replace(AssetDatabase.GetAssetPath(obj), @"/[^/]+\..*$", "");
  }

  internal static string GetParent(string asset, bool keepSlash) {
   if(keepSlash)
    return Regex.Replace(asset, @"[^/]+\..*$", "");
   else
    return Regex.Replace(asset, @"/[^/]+\..*$", "");
  }
 }
 #endregion
}