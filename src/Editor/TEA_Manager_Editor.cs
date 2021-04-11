using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEngine.SceneManagement;
using static TEA.TEA_Utility;

namespace TEA {
 [CustomEditor(typeof(TEA_Manager))]
 public class TEA_Manager_Editor : Editor {
  // ----- ----- TAMS Editor ----- -----
  public static readonly string MENU_ITEM = "Tanuki's Educated Avatar Manager";

  bool _show;

  // --- Banner ---
  private static GUIContent bannerContent = new GUIContent {
   image=null
  };

  public override void OnInspectorGUI() {
   // -- Banner --
   var assets = AssetDatabase.FindAssets("TEA_Manager_Banner");
   if(null!=assets&&0<assets.Length)
    bannerContent.image=AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(assets[0]));
   else
    Debug.LogError("banner is not found");

   //----------------
   EditorGUILayout.BeginVertical();
   EditorGUILayout.LabelField(MENU_ITEM, EditorStyles.boldLabel);
   GUILayout.Box(bannerContent.image, GUILayout.Width(2048/4), GUILayout.Height(256/3.7f));
   EditorGUILayout.EndVertical();
   //------
   if(GUILayout.Button("Tanukis Only"))
    _show=!_show;

   if(_show)
    base.OnInspectorGUI();
  }
  // ----- ----- Utility ----- -----


  // ----- ----- Avatar Setup Methods ----- -----
  private static readonly string TEA_OBJECT_MENU = "TEA Functions";
  private static string EXPRESSION_FOLDER = "Expressions";
  private static string EXPRESSION_MENU = $"Assets/TEA Manager/Resources/{EXPRESSION_FOLDER}/ExpressionsMenu.asset";
  private static string EXPRESSION_PARAMETER = $"Assets/TEA Manager/Resources/{EXPRESSION_FOLDER}/ExpressionParameters.asset";

  private static readonly string PLAYABLE_LAYERS_FOLDER = "Playable Layers";

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
   string avatarFolder=CreatePath(parentFolder, newAvatar.gameObject.name);
   if(!AssetDatabase.IsValidFolder(avatarFolder)) {
    if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(parentFolder, newAvatar.gameObject.name))) {
     EditorUtility.DisplayDialog("Make Avatar 3.0", $"Could not create folder [{avatarFolder}]", "ok");
     return;
    }
   }
   parentFolder=avatarFolder;

   // Playable Layers
   string animation_folder = CreatePath(parentFolder, PLAYABLE_LAYERS_FOLDER);
   if(!AssetDatabase.IsValidFolder(animation_folder))
    AssetDatabase.CreateFolder(parentFolder, PLAYABLE_LAYERS_FOLDER);

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
   parentFolder=CreatePath(parentFolder, newAvatar.gameObject.name);
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
   string toggle_folder = CreatePath(parentFolder, parent.gameObject.name, "Toggles");
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
 }
}