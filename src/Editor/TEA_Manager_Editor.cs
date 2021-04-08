using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEngine.SceneManagement;
using TEA.ScriptableObject;
using System;

namespace TEA {
 [CustomEditor(typeof(TEA_Manager))]
 public class TEA_Manager_Editor : Editor {
  // ----- ----- TAMS Editor ----- -----
  private static readonly string MENU_ITEM = "Tanuki's Avatar Management Suit";
  private static GUIStyle noElementLayout;

  //--- Avatar ---
  private static readonly string CONTROLLER_PREFIX = "TEA_Controller-";
  private static readonly string ACTION_CONTROLLER_PREFIX = "TEA_ActionController-";
  private static readonly string ASSETS_CONTENT = "Assets/";
  private static readonly string WORKING_DIR = "TEA_Temp";
  private static readonly string WORKING_DIR_PATH = ASSETS_CONTENT+WORKING_DIR;
  private static readonly string WORKING_DIR_CONTENT = WORKING_DIR_PATH+"/";

  private Dictionary<string, VRCAvatarDescriptor> avatars = new Dictionary<string, VRCAvatarDescriptor>();
  public string[] avatarKeys;
  public int avatarIndex = 0;

  // --- Validation ---
  private static readonly string ERROR_HEADER = "Issue Compiling Animators";
  private bool validationIssue = false;
  private bool validate = true;
  private Dictionary<VRCAvatarDescriptor.AnimLayerType, List<string>> errorLog = new Dictionary<VRCAvatarDescriptor.AnimLayerType, List<string>>();


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

    validate=EditorGUILayout.Toggle(new GUIContent("Validate", "Validate layers adhere to VRC 3.0 SDK"), validate, EditorStyles.toggle);

    if(validate&&!EditorApplication.isPlaying) {
     if(GUILayout.Button("Play & Compile", GUILayout.Height(30))) {
      CompileAnimators(avatars, manager);
      if(!validationIssue)
       EditorApplication.isPlaying=true;
     }
    } else if(!validate&&!EditorApplication.isPlaying) {
     if(GUILayout.Button("Play", GUILayout.Height(30))) {
      EditorApplication.isPlaying=true;
     }
    } else if(GUILayout.Button("Stop", GUILayout.Height(30))) {
     EditorApplication.isPlaying=false;
    }
    if(validate&&!EditorApplication.isPlaying) {
     if(GUILayout.Button("Compile", GUILayout.Height(30))) {
      CompileAnimators(avatars, manager);
      if(!validationIssue)
       EditorApplication.isPlaying=EditorUtility.DisplayDialog($"{avatarKeys[avatarIndex]}", "Avatar Compiled", "Play","Continue");
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
   List<string> managers = new List<string>();
   foreach(Scene scene in SceneManager.GetAllScenes()) {
    if(!scene.isLoaded)
     continue;
    foreach(GameObject obj in scene.GetRootGameObjects()) {
     Component comp = obj.GetComponentInChildren(typeof(TEA_Manager), true);
     if(null!=comp) {
      managers.Add(scene.name+"/"+comp.gameObject.name);
     }
    }
   }
   if(managers.Count > 1) {
    string list = "";
    foreach(string c in managers) {
     list+="\n[";
     list+=c;
     list+="]";
    }
    //EditorUtility.DisplayDialog("TEA Manager", $"Only one TEA Manager can be active {list}", "OK");
    output = list;
    return false;
   }
   output="";
   return true;
  }

  // ----- ----- Avatar Controllers ----- -----
  #region
  private void CompileAnimators(Dictionary<string, VRCAvatarDescriptor> avatars, TEA_Manager manager) {
   validationIssue=false;
   manager.Controllers=new List<RuntimeAnimatorController>();
   manager.LayerInfo=new List<TEA_PlayableLayerData>();
   manager.Avatars=new List<string>();

   // working folder
   //TODO possibly make folders specific to scenes to avoid overlap
   if(!AssetDatabase.IsValidFolder(WORKING_DIR_PATH)) {
    if(string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets", WORKING_DIR))) {
     EditorUtility.DisplayDialog(ERROR_HEADER, $"Could not create working folder [{WORKING_DIR_PATH}]", "ok");
     return;
    }
   }
   foreach(string path in AssetDatabase.GetSubFolders(WORKING_DIR_PATH)) {
    AssetDatabase.DeleteAsset(path);
   }

   // --- TEA  ---
   AnimatorController teaAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(manager.TEA_Animations));
   while(teaAnimContr.layers.Length >0) {
    teaAnimContr.RemoveLayer(0);
   }
   AnimatorStateMachine stateD = new AnimatorStateMachine();
   teaAnimContr.AddLayer(new AnimatorControllerLayer() {
    name=AvatarController.TEA_HAND_LAYER,
    defaultWeight=1,
    avatarMask=manager.AvatarMaskArms,
    stateMachine=stateD
   });
   AnimatorStateMachine stateM  = new AnimatorStateMachine();
   teaAnimContr.AddLayer(new AnimatorControllerLayer() {
    name = AvatarController.TEA_LAYER,
    defaultWeight = 0,
    avatarMask = manager.AvatarMaskAll,
    stateMachine = stateM
   });
   AnimationClip def_clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TEA Manager/Resources/Animation/TEA Animations/Default.anim");

   //default
   stateD.defaultState =stateD.AddState("Default");
   AnimatorStateTransition anyToDefault = stateD.AddAnyStateTransition(stateD.defaultState);
   anyToDefault.AddCondition(AnimatorConditionMode.Equals, 0, AvatarController.TEA_ANIM_PARAM);
   stateD.defaultState.motion = def_clip;

   //overriding
   stateM.defaultState=stateM.AddState("Default");
   AnimatorStateTransition anyToDefault2 = stateM.AddAnyStateTransition(stateM.defaultState);
   anyToDefault2.AddCondition(AnimatorConditionMode.Equals, 0, AvatarController.TEA_ANIM_PARAM);

   manager.TEA_AnimationClips.ClearOptions();
   List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
   options.Add(new Dropdown.OptionData("Hands Posed"));

   // dynamic
   int count = 1;
   foreach(string folder in AssetDatabase.GetSubFolders("Assets/TEA Manager/Resources/Animation/TEA Animations")){
    string name = folder.Substring(folder.LastIndexOf('/')+1);
    Dropdown.OptionData option = new Dropdown.OptionData(name);
    options.Add(option);
    AnimationClip start=null;
    AnimationClip loop=null;
    foreach (AnimationClip clip in GetSongList(folder)) { 
      if(clip.name.Contains("intro") ||clip.name.Contains("Intro")) {
       start = clip;
      }else
       loop = clip;
    }//for
    AnimatorState state = stateM.AddState(name);
    state.motion=loop;
    if(null !=start) {
     AnimatorState startState = stateM.AddState(name+"-intro");
     startState.motion = start;
     stateM.defaultState.AddTransition(startState).AddCondition(AnimatorConditionMode.Equals, count, AvatarController.TEA_ANIM_PARAM);
     startState.AddTransition(state).hasExitTime = true;
    } else {
     stateM.defaultState.AddTransition(state).AddCondition(AnimatorConditionMode.Equals, count, AvatarController.TEA_ANIM_PARAM);
    }
    state.AddExitTransition().AddCondition(AnimatorConditionMode.NotEqual, count, AvatarController.TEA_ANIM_PARAM);
    count++;
   }//for
   manager.TEA_AnimationClips.AddOptions(options);
   AssetDatabase.SaveAssets();

   // --- for all avatars ---
   foreach(KeyValuePair<string, VRCAvatarDescriptor> avatar in avatars) {
    Debug.Log($"----- Creating animator controllers for [{avatar.Key}]");
    // avatar folder
    string folderPath = WORKING_DIR_CONTENT+avatar.Key;
    if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(WORKING_DIR_PATH, avatar.Key))) {
     EditorUtility.DisplayDialog(ERROR_HEADER, $"Could not create working folder [{folderPath}]", "ok");
     return;
    }

    //--- Animator ---
    AnimatorController superAnimator = new AnimatorController() { name=CONTROLLER_PREFIX+avatar.Key };
    TEA_PlayableLayerData layerInfo = new TEA_PlayableLayerData();
    layerInfo.AvatarName=avatar.Key;
    layerInfo.name=avatar.Key+"-layerData";

    RuntimeAnimatorController baseRunContr = manager.VRC_Base;
    if(!avatar.Value.baseAnimationLayers[0].isDefault&&avatar.Value.baseAnimationLayers[0].animatorController)
     baseRunContr=avatar.Value.baseAnimationLayers[0].animatorController;
    string baseControllerPath = folderPath+"/"+"Base.controller";
    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(baseRunContr), baseControllerPath);
    AnimatorController baseAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseControllerPath);
    GetBehaviours(baseAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.Base);
    CombineAnimator(superAnimator, baseAnimContr, null);
    layerInfo.data[0].start=0;
    layerInfo.data[0].end=baseAnimContr.layers.Length;

    // Additive
    AnimatorController additiveAnimContr = null;
    if(!avatar.Value.baseAnimationLayers[1].isDefault||!avatar.Value.baseAnimationLayers[1].animatorController) {
     RuntimeAnimatorController additiveRunContr = avatar.Value.baseAnimationLayers[1].animatorController;
     string additiveControllerPath = folderPath+"/"+"Additive.controller";
     AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(additiveRunContr), additiveControllerPath);
     additiveAnimContr=AssetDatabase.LoadAssetAtPath<AnimatorController>(additiveControllerPath);
     GetBehaviours(additiveAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.Additive);
     CombineAnimator(superAnimator, additiveAnimContr, null);
     layerInfo.data[1].start=layerInfo.data[0].end;
     layerInfo.data[1].end=layerInfo.data[0].end+(additiveAnimContr.layers.Length);
    } else {
     layerInfo.data[1].start=layerInfo.data[0].end;
     layerInfo.data[1].end=layerInfo.data[0].end;
    }

    // TEA Animations
    CombineAnimator(superAnimator, teaAnimContr, null);

    // Gesture
    RuntimeAnimatorController gestureRunContr = manager.VRC_Gesture_Male;
    if(!avatar.Value.baseAnimationLayers[2].isDefault&&avatar.Value.baseAnimationLayers[2].animatorController)
     gestureRunContr=avatar.Value.baseAnimationLayers[2].animatorController;
    string gestureControllerPath = folderPath+"/"+"Gesture.controller";
    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(gestureRunContr), gestureControllerPath);
    AnimatorController gestureAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(gestureControllerPath);
    GetBehaviours(gestureAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.Gesture);
    CombineAnimator(superAnimator, gestureAnimContr, null);
    layerInfo.data[2].start=layerInfo.data[1].end+teaAnimContr.layers.Length;
    layerInfo.data[2].end=layerInfo.data[1].end+teaAnimContr.layers.Length+(gestureAnimContr.layers.Length);

    //Actions
    RuntimeAnimatorController actionRunContr = manager.VRC_Action;
    if(!avatar.Value.baseAnimationLayers[3].isDefault&&avatar.Value.baseAnimationLayers[3].animatorController)
     actionRunContr=avatar.Value.baseAnimationLayers[3].animatorController;
    string actionControllerPath = folderPath+"/"+"Action.controller";
    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(actionRunContr), actionControllerPath);
    AnimatorController actionAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(actionControllerPath);
    GetBehaviours(actionAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.Action);
    CombineAnimator(superAnimator, actionAnimContr, null);
    layerInfo.data[3].start=layerInfo.data[2].end;
    layerInfo.data[3].end=layerInfo.data[2].end+(actionAnimContr.layers.Length);

    //FX
    AnimatorController fxAnimContr = null;
    if(!avatar.Value.baseAnimationLayers[4].isDefault||!avatar.Value.baseAnimationLayers[4].animatorController) {
     RuntimeAnimatorController fxRunContr = avatar.Value.baseAnimationLayers[4].animatorController;
     string fxControllerPath = folderPath+"/"+"FX.controller";
     AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(fxRunContr), fxControllerPath);
     fxAnimContr=AssetDatabase.LoadAssetAtPath<AnimatorController>(fxControllerPath);

     SetFXDefault(superAnimator, fxAnimContr, avatar.Value.gameObject, manager.AvatarMaskNone, folderPath);
     GetBehaviours(fxAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.FX);
     CombineAnimator(superAnimator, fxAnimContr, manager.AvatarMaskNone);
     //SetFXDefault(action, fxAnimContr, avatar.Value.gameObject, manager.AvatarMaskNone, folderPath);
     //CombineAnimator(action, fxAnimContr, manager.AvatarMaskNone);
     layerInfo.data[4].start=layerInfo.data[3].end+1;
     layerInfo.data[4].end=layerInfo.data[3].end+1+(fxAnimContr.layers.Length);
    } else {
     layerInfo.data[4].start=layerInfo.data[3].end;
     layerInfo.data[4].end=layerInfo.data[3].end;
    }

    string superAnimatorPath = folderPath+"/"+superAnimator.name+".controller";
    AssetDatabase.CreateAsset(superAnimator, superAnimatorPath);
    manager.Controllers.Add(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(superAnimatorPath));

    string layerInfoPath = folderPath+"/"+layerInfo.name+".asset";
    AssetDatabase.CreateAsset(layerInfo, layerInfoPath);
    manager.LayerInfo.Add(AssetDatabase.LoadAssetAtPath<TEA_PlayableLayerData>(layerInfoPath));

    manager.Avatars.Add(avatar.Key);
    Debug.Log($"----- Created animator controllers for [{avatar.Key}]");

    // Validation
    if(validate) {
     errorLog=new Dictionary<VRCAvatarDescriptor.AnimLayerType, List<string>>();
     foreach(VRCAvatarDescriptor.CustomAnimLayer layer in avatar.Value.baseAnimationLayers) {
      if(!layer.isDefault&&!layer.animatorController)
       GetLayerIssues(layer.type).Add("No Controller specified");
      else if(VRCAvatarDescriptor.AnimLayerType.Base==layer.type)
       ValidateOnlyTransforms(true, layer.type, baseAnimContr);
      else if(VRCAvatarDescriptor.AnimLayerType.Additive==layer.type)
       ValidateOnlyTransforms(true, layer.type, additiveAnimContr);
      else if(VRCAvatarDescriptor.AnimLayerType.Gesture==layer.type)
       ValidateOnlyTransforms(true, layer.type, gestureAnimContr);
      else if(VRCAvatarDescriptor.AnimLayerType.Action==layer.type)
       ValidateOnlyTransforms(true, layer.type, actionAnimContr);
      else if(VRCAvatarDescriptor.AnimLayerType.FX==layer.type)
       ValidateOnlyTransforms(false, layer.type, fxAnimContr);
     }

     missingParam=new List<string>();
     ValidateExpressionParameters(avatar.Value,superAnimator);

     string issues = PrintValidationIssues();
     if(!string.IsNullOrEmpty(issues)) {
      validationIssue=true;
      EditorUtility.DisplayDialog($"[{avatar.Key}] Compile Issues", issues, "OK");
     }
    }
    AssetDatabase.SaveAssets();
   }// for
  }

  internal static void GetBehaviours(AnimatorController controller, TEA_PlayableLayerData layerData, VRCAvatarDescriptor.AnimLayerType type) {
   foreach(AnimatorControllerLayer layer in controller.layers) {
    foreach(ChildAnimatorState state in layer.stateMachine.states) {
     foreach(StateMachineBehaviour beh in state.state.behaviours) {
      if(beh is VRCPlayableLayerControl) {
       VRCPlayableLayerControl pc = (VRCPlayableLayerControl)beh;
       TEA_PlayableLayerControl tc = state.state.AddStateMachineBehaviour<TEA_PlayableLayerControl>();
       tc.blendDuration = pc.blendDuration;
        tc.debugString=pc.debugString;
        tc.goalWeight=pc.goalWeight;
        tc.layer=pc.layer;
       tc.state=state.state.name;
      }else if(beh is VRCAvatarParameterDriver) {
       VRCAvatarParameterDriver vd = (VRCAvatarParameterDriver)beh;
       TEA_AvatarParameterDriver td = state.state.AddStateMachineBehaviour<TEA_AvatarParameterDriver>();
       td.name = vd.name;
       td.debugString=vd.debugString;
       td.localOnly=vd.localOnly;
       td.parameters=new List<VRC.SDKBase.VRC_AvatarParameterDriver.Parameter>();
       foreach(VRCAvatarParameterDriver.Parameter param in vd.parameters) {
        td.parameters.Add(new VRCAvatarParameterDriver.Parameter() {
         chance=param.chance,
         name=param.name,
         type=param.type,
         value=param.value,
         valueMax=param.valueMax,
         valueMin=param.valueMin
        });
        td.state=state.state.name;
       }
      }
     }
    }
   }
  }

  internal static List<AnimationClip> GetSongList(string folder) {
   List<AnimationClip> clips = new List<AnimationClip>();
   var assets = AssetDatabase.FindAssets("t:AnimationClip", new[] { folder });
   foreach(var guid in assets) {
    Debug.Log(guid);
    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
    clips.Add(clip);
   }
   return clips;
  }

  private void CombineAnimator(AnimatorController superAnimator, AnimatorController animator, AvatarMask mask) {
   if(null==animator||null==superAnimator)
    return;

   foreach(AnimatorControllerParameter param in animator.parameters) {
    if(!HasAnimatorParameter(superAnimator, param.name))
     superAnimator.AddParameter(param);
   }

   foreach(AnimatorControllerLayer layer in animator.layers) {
    //Debug.Log($"combining [{superAnimator.name}] with [{animator.name}] layer[{layer.name}]");
    AnimatorControllerLayer newLayer = new AnimatorControllerLayer {
     avatarMask=null==mask ? layer.avatarMask : mask,
     name=layer.name,
     blendingMode=layer.blendingMode,
     defaultWeight=layer.defaultWeight,
     iKPass=layer.iKPass,
     stateMachine=layer.stateMachine,
     syncedLayerAffectsTiming=layer.syncedLayerAffectsTiming,
     syncedLayerIndex=layer.syncedLayerIndex
    };
    superAnimator.AddLayer(newLayer);
   }
   //Debug.Log("-----------");
  }

  private static AnimationClip SetFXDefault(AnimatorController superAnimator, AnimatorController fxAnimator, GameObject gameObject, AvatarMask mask, string folder) {
   if(null==fxAnimator)
    return null;

   AnimatorControllerLayer fxDefault = new AnimatorControllerLayer {
    name="FX Default",
    defaultWeight=1,
    avatarMask=mask,
    stateMachine=new AnimatorStateMachine()
   };

   AnimatorStateMachine stateMachine = fxDefault.stateMachine;
   AnimationClip def_clip = new AnimationClip {
    name="TEA FX Default"
   };
   AnimatorState defaultState = stateMachine.AddState("FX Default");

   foreach(AnimatorControllerLayer layer in fxAnimator.layers) {
    SetFXDefault(def_clip, layer.stateMachine.states, gameObject);
    foreach(ChildAnimatorStateMachine childMachine in layer.stateMachine.stateMachines) {
     SetFXDefault(def_clip, childMachine.stateMachine.states, gameObject);
    }
   }
   string actionPath = folder+"/"+superAnimator.name+"-def_clip.anim";
   AssetDatabase.CreateAsset(def_clip, actionPath);
   AnimationClip retClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(actionPath);
   defaultState.writeDefaultValues=true;
   defaultState.motion=retClip;
   superAnimator.AddLayer(fxDefault);
   return retClip;
  }

  private static void SetFXDefault(AnimationClip def_clip, ChildAnimatorState[] states, GameObject gameObject) {
   foreach(ChildAnimatorState state in states) {
    state.state.writeDefaultValues=true;
    if(null!=state.state.motion) {
     AnimationClip clip = (AnimationClip)state.state.motion;

     foreach(EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)) {
      if(null==binding)
       continue;

      //Debug.Log($"clip[{clip.name}] \n binding [{binding.path},{binding.type},{binding.propertyName}]");
      PropertyInfo prop = binding.type.GetProperty(binding.propertyName);
      FieldInfo field = binding.type.GetField(binding.propertyName);
      AnimationUtility.GetFloatValue(gameObject, binding, out float value);
      def_clip.SetCurve(binding.path, binding.type, binding.propertyName, AnimationCurve.Constant(0.0f, 0.0f, value));
     }
    }
   }
  }

  private bool HasAnimatorParameter(AnimatorController controller, string name) {
   foreach(AnimatorControllerParameter parameter in controller.parameters) {
    if(parameter.name==name)
     return true;
   }
   return false;
  }
  #endregion

  // ----- ----- Validation ----- -----
  #region
  private string PrintValidationIssues() {
   string text = "";

   // missing param
   if(missingParam.Count >0) { 
    text+="Missing Parameters";
   foreach(string missing in missingParam) {
    text+="\n -";
    text+=missing;
   }
   text+="\n";
   }

   //layer issues
   foreach(KeyValuePair<VRCAvatarDescriptor.AnimLayerType, List<string>> layerIssue in errorLog) {
    if(null==layerIssue.Value)
     continue;
    text+=layerIssue.Key;
    foreach(string issue in layerIssue.Value) {
     text+="\n  - ";
     text+=issue;
    }
    text+="\n";
   }
   return text;
  }

  private List<string> missingParam = new List<string>();
  private void ValidateExpressionParameters(VRCAvatarDescriptor avatar, AnimatorController superAnimator) {
   if(avatar.customExpressions) {
    foreach(VRCExpressionParameters.Parameter parameter in avatar.expressionParameters.parameters) {
     bool exists = false;
     if(string.IsNullOrEmpty(parameter.name))
      continue;
     foreach(AnimatorControllerParameter aParam in superAnimator.parameters) {
      if(aParam.name==parameter.name) {
       exists=true;
       break;
      }
     }//for aParam
     if(!exists)
      missingParam.Add(parameter.name);
    }//for parameter
   }
  }

  private List<string> GetLayerIssues(VRCAvatarDescriptor.AnimLayerType layerName) {
   if(!errorLog.TryGetValue(layerName, out List<string> list))
    errorLog.Add(layerName, (list=new List<string>()));
   return list;
  }

  private void ValidateOnlyTransforms(bool onlyTransfroms, VRCAvatarDescriptor.AnimLayerType layerName, AnimatorController controller) {
   if(!controller)
    return;
   foreach(AnimatorControllerLayer layer in controller.layers) {
    ValidateOnlyTransforms(onlyTransfroms, layerName, layer.stateMachine.states);
    foreach(ChildAnimatorStateMachine childMachine in layer.stateMachine.stateMachines) {
     ValidateOnlyTransforms(onlyTransfroms, layerName, childMachine.stateMachine.states);
    }
   }
  }

  private void ValidateOnlyTransforms(bool onlyTransfroms, VRCAvatarDescriptor.AnimLayerType layerName, ChildAnimatorState[] states) {
   foreach(ChildAnimatorState state in states) {
    Motion m = state.state.motion;
    if(!ValidateOnlyTransforms(onlyTransfroms, layerName, m)) {
     List<string> list = GetLayerIssues(layerName);
     if(onlyTransfroms)
      list.Add($"State [{state.state.name}] contains non-Transformations");
     else
      list.Add($"State [{state.state.name}] contains Transformations");
    }
   }
  }

  private bool ValidateOnlyTransforms(bool onlyTransfroms, VRCAvatarDescriptor.AnimLayerType layerName, Motion motion) {
   if(!motion)
    return true;
   else if(motion is BlendTree) {
    BlendTree bTree = (BlendTree)motion;
    bool bRetVal = true;
    foreach(ChildMotion child in bTree.children) {
     if(!ValidateOnlyTransforms(onlyTransfroms, layerName, child.motion))
      bRetVal=false;
    }
    return bRetVal;
   } else if(motion is AnimationClip) {
    AnimationClip clip = (AnimationClip)motion;
    foreach(EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)) {
     if(onlyTransfroms!=(binding.type==typeof(Transform)||binding.type==typeof(Animator))) {
      string issue = onlyTransfroms ? "Transforms" : "Non-Transforms";
      Debug.LogWarning($"[{clip.name}] in {layerName} layer contains {issue}");
      return false;
     }
    }
   }
   return true;
  }
  #endregion


  private static readonly string TEA_OBJECT_MENU = "TEA Functions";
  // ----- ----- Avatar Setup Methods ----- -----
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
   Debug.Log("creating toggle");
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
   Debug.Log("creating toggle");
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
   Debug.Log("check toggle");
   GameObject selected = Selection.activeGameObject;
   Component[] avatars = selected.GetComponentsInParent(typeof(VRCAvatarDescriptor), true);
   if(1==avatars.Length)
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