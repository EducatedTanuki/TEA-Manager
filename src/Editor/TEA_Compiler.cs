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
using static TEA.TEA_Utility;

namespace TEA {
 public class TEA_Compiler {
  public static readonly string CONTROLLER_PREFIX = "TEA_Controller-";
  public static readonly string ACTION_CONTROLLER_PREFIX = "TEA_ActionController-";
  public static readonly string ASSETS_CONTENT = "Assets/";
  public static readonly string WORKING_DIR = "TEA_Temp";
  public static readonly string WORKING_DIR_PATH = ASSETS_CONTENT+WORKING_DIR;
  public static readonly string WORKING_DIR_CONTENT = WORKING_DIR_PATH+"/";

  // --- Validation ---
  public static readonly string ERROR_HEADER = "Issue Compiling Animators";
  public bool validate = true;
  public Dictionary<VRCAvatarDescriptor.AnimLayerType, List<string>> errorLog = new Dictionary<VRCAvatarDescriptor.AnimLayerType, List<string>>();

  // ----- ----- Compile ----- -----

  public bool CompileAnimators(TEA_Manager manager) {
   bool validationIssue = false;

   // working folder
   if(!AssetDatabase.IsValidFolder(WORKING_DIR_PATH)) {
    if(string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets", WORKING_DIR))) {
     EditorUtility.DisplayDialog(ERROR_HEADER, $"Could not create working folder [{WORKING_DIR_PATH}]", "ok");
     return true;
    }
   }

   AnimatorController teaAnimContr = GenerateTEA_Animator(manager);

   foreach(string path in AssetDatabase.GetSubFolders(WORKING_DIR_PATH)) {
    AssetDatabase.DeleteAsset(path);
   }

   int aCount = 0;
   // --- --- --- for all avatars
   foreach(VRCAvatarDescriptor avatar in TEA_Manager.AvatarDescriptor) {

    //Scene Folder
    string sceneFolder = CreatePath(false, WORKING_DIR_PATH, TEA_Manager.AvatarDescriptor[aCount].gameObject.scene.name);
    if(!AssetDatabase.IsValidFolder(sceneFolder)) {
     if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(WORKING_DIR_PATH, TEA_Manager.AvatarDescriptor[aCount].gameObject.scene.name))) {
      EditorUtility.DisplayDialog(ERROR_HEADER, $"Could not create working folder [{sceneFolder}]", "ok");
      return true;
     }
    }

    VRCAvatarDescriptor avatarComp = TEA_Manager.AvatarDescriptor[aCount];
    string avatarKey = avatarComp.gameObject.name;
    Debug.Log($"----- Creating animator controllers for [{avatarKey}]");

    // avatar folder
    string folderPath = CreatePath(false, sceneFolder, avatarKey);
    if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(sceneFolder, avatarKey))) {
     EditorUtility.DisplayDialog(ERROR_HEADER, $"Could not create working folder [{folderPath}]", "ok");
     return true;
    }

    //--- Animator ---
    AnimatorController superAnimator = new AnimatorController() { name=CONTROLLER_PREFIX+avatarKey };
    TEA_PlayableLayerData layerInfo = TEA_PlayableLayerData.CreateInstance<TEA_PlayableLayerData>();
    layerInfo.AvatarName=avatarKey;
    layerInfo.name=avatarKey+"-layerData";

    RuntimeAnimatorController baseRunContr = manager.Base;
    if(!avatarComp.baseAnimationLayers[0].isDefault&&null!=avatarComp.baseAnimationLayers[0].animatorController)
     baseRunContr=avatarComp.baseAnimationLayers[0].animatorController;
    string baseControllerPath = CreatePath(false, folderPath, "Base.controller");
    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(baseRunContr), baseControllerPath);
    AnimatorController baseAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseControllerPath);
    GetBehaviours(baseAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.Base);
    CombineAnimator(superAnimator, baseAnimContr, null);
    layerInfo.data[0].start=0;
    layerInfo.data[0].end=baseAnimContr.layers.Length;

    // Additive
    AnimatorController additiveAnimContr = null;
    if(!avatarComp.baseAnimationLayers[1].isDefault&&null!=avatarComp.baseAnimationLayers[1].animatorController) {
     RuntimeAnimatorController additiveRunContr = avatarComp.baseAnimationLayers[1].animatorController;
     string additiveControllerPath = CreatePath(false, folderPath, "Additive.controller");
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
    RuntimeAnimatorController gestureRunContr = manager.Gesture_Male;
    if(!avatarComp.baseAnimationLayers[2].isDefault&&null!=avatarComp.baseAnimationLayers[2].animatorController)
     gestureRunContr=avatarComp.baseAnimationLayers[2].animatorController;
    string gestureControllerPath = CreatePath(false, folderPath, "Gesture.controller");
    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(gestureRunContr), gestureControllerPath);
    AnimatorController gestureAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(gestureControllerPath);
    GetBehaviours(gestureAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.Gesture);
    CombineAnimator(superAnimator, gestureAnimContr, null);
    layerInfo.data[2].start=layerInfo.data[1].end+teaAnimContr.layers.Length;
    layerInfo.data[2].end=layerInfo.data[1].end+teaAnimContr.layers.Length+(gestureAnimContr.layers.Length);

    //Actions
    RuntimeAnimatorController actionRunContr = manager.Action;
    if(!avatarComp.baseAnimationLayers[3].isDefault&&null!=avatarComp.baseAnimationLayers[3].animatorController)
     actionRunContr=avatarComp.baseAnimationLayers[3].animatorController;
    string actionControllerPath = CreatePath(false, folderPath, "Action.controller");
    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(actionRunContr), actionControllerPath);
    AnimatorController actionAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(actionControllerPath);
    GetBehaviours(actionAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.Action);
    CombineAnimator(superAnimator, actionAnimContr, null);
    layerInfo.data[3].start=layerInfo.data[2].end;
    layerInfo.data[3].end=layerInfo.data[2].end+(actionAnimContr.layers.Length);

    //FX
    AnimatorController fxAnimContr = null;
    if(!avatarComp.baseAnimationLayers[4].isDefault&&null!=avatarComp.baseAnimationLayers[4].animatorController) {
     RuntimeAnimatorController fxRunContr = avatarComp.baseAnimationLayers[4].animatorController;
     string fxControllerPath = CreatePath(false, folderPath, "FX.controller");
     AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(fxRunContr), fxControllerPath);
     fxAnimContr=AssetDatabase.LoadAssetAtPath<AnimatorController>(fxControllerPath);

     SetFXDefault(superAnimator, fxAnimContr, avatarComp.gameObject, manager.AvatarMaskNone, folderPath);
     GetBehaviours(fxAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.FX);
     CombineAnimator(superAnimator, fxAnimContr, manager.AvatarMaskNone);
     //SetFXDefault(action, fxAnimContr, avatarComp.gameObject, manager.AvatarMaskNone, folderPath);
     //CombineAnimator(action, fxAnimContr, manager.AvatarMaskNone);
     layerInfo.data[4].start=layerInfo.data[3].end+1;
     layerInfo.data[4].end=layerInfo.data[3].end+1+(fxAnimContr.layers.Length);
    } else {
     layerInfo.data[4].start=layerInfo.data[3].end;
     layerInfo.data[4].end=layerInfo.data[3].end;
    }

    string superAnimatorPath = CreatePath(false, folderPath, superAnimator.name+".controller");
    AssetDatabase.CreateAsset(superAnimator, superAnimatorPath);
    manager.Controllers.Add(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(superAnimatorPath));

    string layerInfoPath = CreatePath(false, folderPath, layerInfo.name+".asset");
    AssetDatabase.CreateAsset(layerInfo, layerInfoPath);
    manager.LayerInfo.Add(AssetDatabase.LoadAssetAtPath<TEA_PlayableLayerData>(layerInfoPath));

    //Debug.Log($"HEAD[{AvatarController.GetBone(avatarComp, HumanBodyBones.Head).position.ToString("F4")}] ViewPort:[{avatarComp.ViewPosition.ToString("F4")}] Transform[{AvatarController.GetBone(avatarComp, HumanBodyBones.Head).InverseTransformPoint(avatarComp.ViewPosition).ToString("F4")}]");
    manager.ViewPorts.Add(AvatarController.GetBone(avatarComp, HumanBodyBones.Head).InverseTransformPoint(avatarComp.ViewPosition));
    Debug.Log($"----- Created animator controllers for [{avatarKey}]");

    // Validation
    if(validate) {
     errorLog=new Dictionary<VRCAvatarDescriptor.AnimLayerType, List<string>>();
     foreach(VRCAvatarDescriptor.CustomAnimLayer layer in avatarComp.baseAnimationLayers) {
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
     ValidateExpressionParameters(avatarComp, superAnimator);

     string issues = PrintValidationIssues();
     if(!string.IsNullOrEmpty(issues)) {
      validationIssue=true;
      EditorUtility.DisplayDialog($"[{avatarKey}] Compile Issues", issues, "OK");
     }
    }
    AssetDatabase.SaveAssets();
    aCount++;
   }// for avatar
   return validationIssue;
  }

  private static AnimatorController GenerateTEA_Animator(TEA_Manager manager) {
   // --- TEA  ---
   AnimatorController teaAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(manager.TEA_Animations));
   while(teaAnimContr.layers.Length>0) {
    teaAnimContr.RemoveLayer(0);
   }
   AnimatorStateMachine stateD = new AnimatorStateMachine();
   teaAnimContr.AddLayer(new AnimatorControllerLayer() {
    name=AvatarController.TEA_HAND_LAYER,
    defaultWeight=1,
    avatarMask=manager.AvatarMaskArms,
    stateMachine=stateD
   });

   //default
   stateD.defaultState=stateD.AddState("Default");

   manager.TEA_AnimationClips.ClearOptions();
   List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

   int count = 0;
   List<AnimationClip> armClips = GetAnimationClips("Assets/TEA Manager/Resources/Animation/TEA Animations/TEA Hand Animations");
   foreach(AnimationClip clip in armClips) {
    AnimatorState state = stateD.AddState(clip.name);
    state.motion=clip;
    stateD.defaultState.AddTransition(state).AddCondition(AnimatorConditionMode.Equals, count, AvatarController.TEA_ANIM_PARAM);
    state.AddExitTransition().AddCondition(AnimatorConditionMode.NotEqual, count, AvatarController.TEA_ANIM_PARAM);

    Dropdown.OptionData option = new Dropdown.OptionData(clip.name);
    options.Add(option);
    count++;
   }//for

   manager.GetComponent<AvatarController>().TEA_HAND_LAYER_COUNT=count;

   // --- Full Body Animations
   AnimatorStateMachine stateM = new AnimatorStateMachine();
   teaAnimContr.AddLayer(new AnimatorControllerLayer() {
    name=AvatarController.TEA_LAYER,
    defaultWeight=0,
    avatarMask=manager.AvatarMaskAll,
    stateMachine=stateM
   });

   stateM.defaultState=stateM.AddState("Default");

   // dynamic
   foreach(string folder in AssetDatabase.GetSubFolders("Assets/TEA Manager/Resources/Animation/TEA Animations")) {
    string name = folder.Substring(folder.LastIndexOf('/')+1);
    if("TEA Hand Animations"==name)
     continue;
    Dropdown.OptionData option = new Dropdown.OptionData(name);
    options.Add(option);
    AnimationClip start = null;
    AnimationClip loop = null;
    foreach(AnimationClip clip in GetAnimationClips(folder)) {
     if(clip.name.Contains("intro")||clip.name.Contains("Intro")) {
      start=clip;
     } else
      loop=clip;
    }//for
    AnimatorState state = stateM.AddState(name);
    state.motion=loop;
    if(null!=start) {
     AnimatorState startState = stateM.AddState(name+"-intro");
     startState.motion=start;
     stateM.defaultState.AddTransition(startState).AddCondition(AnimatorConditionMode.Equals, count, AvatarController.TEA_ANIM_PARAM);
     startState.AddTransition(state).hasExitTime=true;
    } else {
     stateM.defaultState.AddTransition(state).AddCondition(AnimatorConditionMode.Equals, count, AvatarController.TEA_ANIM_PARAM);
    }
    state.AddExitTransition().AddCondition(AnimatorConditionMode.NotEqual, count, AvatarController.TEA_ANIM_PARAM);
    count++;
   }//for
   manager.TEA_AnimationClips.AddOptions(options);
   AssetDatabase.SaveAssets();
   return teaAnimContr;
  }

  #region
  internal static void GetBehaviours(AnimatorController controller, TEA_PlayableLayerData layerData, VRCAvatarDescriptor.AnimLayerType type) {
   foreach(AnimatorControllerLayer layer in controller.layers) {
    foreach(ChildAnimatorState state in layer.stateMachine.states) {
     foreach(StateMachineBehaviour beh in state.state.behaviours) {
      if(beh is VRCPlayableLayerControl) {
       VRCPlayableLayerControl pc = (VRCPlayableLayerControl)beh;
       TEA_PlayableLayerControl tc = state.state.AddStateMachineBehaviour<TEA_PlayableLayerControl>();
       tc.blendDuration=pc.blendDuration;
       tc.debugString=pc.debugString;
       tc.goalWeight=pc.goalWeight;
       tc.layer=pc.layer;
       tc.state=state.state.name;
      } else if(beh is VRCAvatarParameterDriver) {
       VRCAvatarParameterDriver vd = (VRCAvatarParameterDriver)beh;
       TEA_AvatarParameterDriver td = state.state.AddStateMachineBehaviour<TEA_AvatarParameterDriver>();
       td.name=vd.name;
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

  //Utility
  internal static List<AnimationClip> GetAnimationClips(string folder) {
   List<AnimationClip> clips = new List<AnimationClip>();
   var assets = AssetDatabase.FindAssets("t:AnimationClip", new[] { folder });
   foreach(var guid in assets) {
    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
    clips.Add(clip);
   }
   return clips;
  }

  private void CombineAnimator(AnimatorController superAnimator, AnimatorController animator, AvatarMask mask) {
   if(null==animator||null==superAnimator)
    return;

   foreach(AnimatorControllerParameter param in animator.parameters) {
    if(!string.IsNullOrEmpty(param.name)&&!HasAnimatorParameter(superAnimator, param.name))
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
    SetFXDefaultMotion(state.state.motion, def_clip, gameObject);
   }
  }

  private static void SetFXDefaultMotion(Motion motion, AnimationClip def_clip, GameObject gameObject) {
   if(null!=motion) {
    if(motion is BlendTree) {
     BlendTree bTree = (BlendTree)motion;
     foreach(ChildMotion child in bTree.children) {
      SetFXDefaultMotion(child.motion, def_clip, gameObject);
     }
    } else if(motion is AnimationClip) {
     AnimationClip clip = (AnimationClip)motion;

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
   }//if
  }

  private bool HasAnimatorParameter(AnimatorController controller, string name) {
   foreach(AnimatorControllerParameter parameter in controller.parameters) {
    if(parameter.name==name)
     return true;
   }
   return false;
  }
  #endregion

  // --- --- Validation --- ---
  #region
  private List<string> GetLayerIssues(VRCAvatarDescriptor.AnimLayerType layerName) {
   if(!errorLog.TryGetValue(layerName, out List<string> list))
    errorLog.Add(layerName, (list=new List<string>()));
   return list;
  }

  private string PrintValidationIssues() {
   string text = "";

   // missing param
   if(missingParam.Count>0) {
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
 }//class
}//namespace