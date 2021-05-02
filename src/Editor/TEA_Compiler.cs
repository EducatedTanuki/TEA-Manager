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
using TEA;
using static TEA.TEA_EditorUtility;
using static TEA.TEA_ValidationIssues;
using System;

namespace TEA {
 public class TEA_Compiler {
	public static readonly string CONTROLLER_PREFIX = "TEA_Controller-";
	public static readonly string ACTION_CONTROLLER_PREFIX = "TEA_ActionController-";

	public string WorkingDirPath;
	public string WorkingDirContent;

	// --- Validation ---
	public static readonly string ERROR_HEADER = "Issue Compiling Animators";
	public bool validate = true;

	// ----- ----- Compile ----- -----
	VRCAvatarDescriptor currentAvatar;
	AnimatorController superAnimator;
	TEA_ValidationIssues issues;
	bool validationIssue = false;

	public bool CompileAnimators(TEA_Manager manager, TEA_Settings settings) {
	 WorkingDirPath = ASSETS_CONTENT + settings.WorkingDirectory;
	 WorkingDirContent = WorkingDirPath + "/";

	 try {
		// working folder
		if(!AssetDatabase.IsValidFolder(WorkingDirPath)) {
		 if(string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets", settings.WorkingDirectory))) {
			EditorUtility.DisplayDialog(ERROR_HEADER, $"Could not create working folder [{WorkingDirPath}]", "ok");
			return true;
		 }
		}

		List<TEA_ValidationIssues> avatarIssues = new List<TEA_ValidationIssues>();
		validationIssue = false;
		AnimatorController teaAnimContr = GenerateTEA_Animator(manager);

		foreach(string path in AssetDatabase.GetSubFolders(WorkingDirPath)) {
		 AssetDatabase.DeleteAsset(path);
		}

		int aCount = 0;
		// --- --- --- for all avatars
		foreach(VRCAvatarDescriptor avatar in TEA_Manager.AvatarDescriptor) {
		 //Scene Folder
		 string sceneFolder = GetPath(false, WorkingDirPath, TEA_Manager.AvatarDescriptor[aCount].gameObject.scene.name);
		 if(!AssetDatabase.IsValidFolder(sceneFolder)) {
			if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(WorkingDirPath, TEA_Manager.AvatarDescriptor[aCount].gameObject.scene.name))) {
			 EditorUtility.DisplayDialog(ERROR_HEADER, $"Could not create working folder [{sceneFolder}]", "ok");
			 return true;
			}
		 }

		 drivers = new List<DriverIssue>();
		 VRCAvatarDescriptor avatarComp = TEA_Manager.AvatarDescriptor[aCount];
		 currentAvatar = avatarComp;
		 issues = TEA_ValidationIssues.CreateInstance<TEA_ValidationIssues>();
		 issues.AvatarName = avatarComp.name;
		 string avatarKey = avatarComp.gameObject.name;

		 //Debug.Log($"----- Creating animator controllers for [{avatarKey}]");

		 // avatar folder
		 string folderPath = GetPath(false, sceneFolder, avatarKey);
		 if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(sceneFolder, avatarKey))) {
			EditorUtility.DisplayDialog(ERROR_HEADER, $"Could not create working folder [{folderPath}]", "ok");
			return true;
		 }

		 //--- Animator ---
		 superAnimator = new AnimatorController() { name = CONTROLLER_PREFIX + avatarKey };
		 TEA_PlayableLayerData layerInfo = TEA_PlayableLayerData.CreateInstance<TEA_PlayableLayerData>();
		 layerInfo.AvatarName = avatarKey;
		 layerInfo.name = avatarKey + "-layerData";

		 RuntimeAnimatorController baseRunContr = manager.Base;
		 if(!avatarComp.baseAnimationLayers[0].isDefault && null != avatarComp.baseAnimationLayers[0].animatorController) {
			baseRunContr = avatarComp.baseAnimationLayers[0].animatorController;
			EditorUtility.SetDirty(baseRunContr);
			AssetDatabase.SaveAssets();
		 }
		 string baseControllerPath = GetPath(false, folderPath, "Base.controller");
		 AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(baseRunContr), baseControllerPath);
		 AnimatorController baseAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseControllerPath);
		 GetBehaviours(baseRunContr, baseAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.Base);
		 CombineAnimator(superAnimator, baseAnimContr, null);
		 layerInfo.data[0].start = 1;
		 layerInfo.data[0].end = layerInfo.data[0].start + baseAnimContr.layers.Length;

		 // Additive
		 AnimatorController additiveAnimContr = null;
		 if(!avatarComp.baseAnimationLayers[1].isDefault && null != avatarComp.baseAnimationLayers[1].animatorController) {
			RuntimeAnimatorController additiveRunContr = avatarComp.baseAnimationLayers[1].animatorController;
			EditorUtility.SetDirty(additiveRunContr);
			AssetDatabase.SaveAssets();
			string additiveControllerPath = GetPath(false, folderPath, "Additive.controller");
			AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(additiveRunContr), additiveControllerPath);
			additiveAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(additiveControllerPath);
			GetBehaviours(additiveRunContr, additiveAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.Additive);
			CombineAnimator(superAnimator, additiveAnimContr, null);
			layerInfo.data[1].start = layerInfo.data[0].end;
			layerInfo.data[1].end = layerInfo.data[0].end + (additiveAnimContr.layers.Length);
		 } else {
			layerInfo.data[1].start = layerInfo.data[0].end;
			layerInfo.data[1].end = layerInfo.data[0].end;
		 }

		 // TEA Animations
		 CombineAnimator(superAnimator, teaAnimContr, null);

		 // Gesture
		 RuntimeAnimatorController gestureRunContr = manager.Gesture_Male;
		 if(!avatarComp.baseAnimationLayers[2].isDefault && null != avatarComp.baseAnimationLayers[2].animatorController) {
			gestureRunContr = avatarComp.baseAnimationLayers[2].animatorController;
			EditorUtility.SetDirty(gestureRunContr);
			AssetDatabase.SaveAssets();
		 }
		 string gestureControllerPath = GetPath(false, folderPath, "Gesture.controller");
		 AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(gestureRunContr), gestureControllerPath);
		 AnimatorController gestureAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(gestureControllerPath);
		 GetBehaviours(gestureRunContr, gestureAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.Gesture);
		 CombineAnimator(superAnimator, gestureAnimContr, null);
		 layerInfo.data[2].start = layerInfo.data[1].end + teaAnimContr.layers.Length;
		 layerInfo.data[2].end = layerInfo.data[1].end + teaAnimContr.layers.Length + (gestureAnimContr.layers.Length);

		 //Actions
		 RuntimeAnimatorController actionRunContr = manager.Action;
		 if(!avatarComp.baseAnimationLayers[3].isDefault && null != avatarComp.baseAnimationLayers[3].animatorController) {
			actionRunContr = avatarComp.baseAnimationLayers[3].animatorController;
			EditorUtility.SetDirty(actionRunContr);
			AssetDatabase.SaveAssets();
		 }
		 string actionControllerPath = GetPath(false, folderPath, "Action.controller");
		 AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(actionRunContr), actionControllerPath);
		 AnimatorController actionAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(actionControllerPath);
		 GetBehaviours(actionRunContr, actionAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.Action);
		 CombineAnimator(superAnimator, actionAnimContr, null);
		 layerInfo.data[3].start = layerInfo.data[2].end;
		 layerInfo.data[3].end = layerInfo.data[2].end + (actionAnimContr.layers.Length);

		 //FX
		 AnimatorController fxAnimContr = null;
		 if(!avatarComp.baseAnimationLayers[4].isDefault && null != avatarComp.baseAnimationLayers[4].animatorController) {
			RuntimeAnimatorController fxRunContr = avatarComp.baseAnimationLayers[4].animatorController;
			EditorUtility.SetDirty(fxRunContr);
			AssetDatabase.SaveAssets();
			string fxControllerPath = GetPath(false, folderPath, "FX.controller");
			AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(fxRunContr), fxControllerPath);
			fxAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(fxControllerPath);

			GetBehaviours(fxRunContr, fxAnimContr, layerInfo, VRCAvatarDescriptor.AnimLayerType.FX);
			CombineAnimator(superAnimator, fxAnimContr, manager.AvatarMaskNone);
			//SetFXDefault(action, fxAnimContr, avatarComp.gameObject, manager.AvatarMaskNone, folderPath);
			//CombineAnimator(action, fxAnimContr, manager.AvatarMaskNone);
			layerInfo.data[4].start = layerInfo.data[3].end + 1;
			layerInfo.data[4].end = layerInfo.data[3].end + 1 + (fxAnimContr.layers.Length);
		 } else {
			layerInfo.data[4].start = layerInfo.data[3].end;
			layerInfo.data[4].end = layerInfo.data[3].end;
		 }
		 SetAnimationDefault(superAnimator, avatarComp.gameObject, manager.AvatarMaskNone, folderPath);

		 string superAnimatorPath = GetPath(false, folderPath, superAnimator.name + ".controller");
		 AssetDatabase.CreateAsset(superAnimator, superAnimatorPath);
		 manager.Controllers.Add(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(superAnimatorPath));

		 string layerInfoPath = GetPath(false, folderPath, layerInfo.name + ".asset");
		 AssetDatabase.CreateAsset(layerInfo, layerInfoPath);
		 manager.LayerInfo.Add(AssetDatabase.LoadAssetAtPath<TEA_PlayableLayerData>(layerInfoPath));

		 /*Debug.Log($"HEAD[{AvatarController.GetBone(avatarComp, HumanBodyBones.Head).position.ToString("F4")}] "
			+ $"ViewPort:[{avatarComp.ViewPosition.ToString("F4")}] "
			+ $"Avatar:[{avatarComp.gameObject.transform.position.ToString("F4")}] "
			+ $"World from Avatar:[{TEA_EditorUtility.TransformPoint(avatarComp.gameObject.transform, avatarComp.ViewPosition).ToString("F4")}]"
			+ $"calc[{TEA_EditorUtility.InverseTransformPoint(AvatarController.GetBone(avatarComp, HumanBodyBones.Head), TEA_EditorUtility.TransformPoint(avatarComp.gameObject.transform, avatarComp.ViewPosition)).ToString("F4")}]");*/
		 manager.ViewPorts.Add(TEA_EditorUtility.InverseTransformPoint(AvatarController.GetBone(avatarComp, HumanBodyBones.Head), TEA_EditorUtility.TransformPoint(avatarComp.gameObject.transform, avatarComp.ViewPosition)));
		 //Debug.Log($"----- Created animator controllers for [{avatarKey}]");

		 // Validation
		 if(validate) {

			//--- check layers
			string nullLayer = "Playable Layer is not default, it should be set in Descriptor";
			foreach(VRCAvatarDescriptor.CustomAnimLayer layer in avatarComp.baseAnimationLayers) {
			 if(!layer.isDefault && null == layer.animatorController) {
				Issue issue = new TEA_ValidationIssues.Issue(nullLayer, currentAvatar);
				issues.GetLayer(layer.type).Add(issue);
			 }
			 ValidateOnlyTransforms(layer.type, AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(layer.animatorController)));
			}

			// missing Expression Parameters
			ValidateCustomExpressions(avatarComp, superAnimator);

			// drivers
			if(null == avatar.expressionParameters) {
			 if(drivers.Count > 0)
				issues.ParameterDrivers.Add(new Issue("You have Parameter Drivers but no ExpressionParameters"));
			} else {
			 foreach(DriverIssue driver in drivers) {
				if(driver.driver.parameters.Count == 0) {
				 Issue issue = new TEA_ValidationIssues.Issue($"Layer[{ driver.layerName }]: no parameter set");
				 issue.Reference.Add(driver.state);
				 issue.Reference.Add(driver.driver);
				 issues.GetLayer(driver.layerType).Add(issue);
				}
				foreach(VRCAvatarParameterDriver.Parameter param in driver.driver.parameters) {
				 if(null == currentAvatar.expressionParameters.FindParameter(param.name)) {
					Issue issue = new TEA_ValidationIssues.Issue($"Layer [{driver.layerName}]: [{param.name}] is not in ExpressionParameters");
					issue.Reference.Add(driver.state);
					issue.Reference.Add(driver.driver);
					issues.GetLayer(driver.layerType).Add(issue);
				 }
				 if(!TEA_EditorUtility.HasAnimatorParameter(param.name, superAnimator.parameters)) {
					Issue issue = new TEA_ValidationIssues.Issue($"Layer [{driver.layerName}]: [{param.name}] is not a parameter in any Playable Layer");
					issue.Reference.Add(driver.state);
					issue.Reference.Add(driver.driver);
					issues.GetLayer(driver.layerType).Add(issue);
				 }
				}
			 }//for
			}

			if(issues.ValidationIssues()) {
			 avatarIssues.Add(issues);
			 validationIssue = true;
			}
		 }//validate

		 AssetDatabase.SaveAssets();
		 aCount++;
		}// for avatar

		if(validationIssue) {
		 TEA_Error_Window.Open(avatarIssues);
		}
		return !validationIssue;
	 } catch(TEA_Exception e) {
		throw e;
	 } catch(Exception e) {
		EditorUtility.DisplayDialog(ERROR_HEADER, $"TEA Manager ran into an unexpected issue while compiling [{currentAvatar.name}].\n"
		 + "If you cannot resolve the issue please raise a ticket on the GitHub and include the error log in the console.", "ok");
		Debug.LogError(new TEA_Exception("Unexpected Exception", e));
	 }
	 return false;
	}

	private static AnimatorController GenerateTEA_Animator(TEA_Manager manager) {
	 // --- TEA  ---
	 AnimatorController teaAnimContr = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(manager.TEA_Animations));
	 while(teaAnimContr.layers.Length > 0) {
		teaAnimContr.RemoveLayer(0);
	 }
	 AnimatorStateMachine stateD = new AnimatorStateMachine();
	 teaAnimContr.AddLayer(new AnimatorControllerLayer() {
		name = AvatarController.TEA_HAND_LAYER,
		defaultWeight = 1,
		avatarMask = manager.AvatarMaskArms,
		stateMachine = stateD
	 });

	 //default
	 stateD.defaultState = stateD.AddState("Default");

	 manager.TEA_AnimationClips.ClearOptions();
	 List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

	 int count = 0;
	 List<AnimationClip> armClips = GetAnimationClips("Assets/TEA Manager/Resources/Animation/TEA Animations/TEA Hand Animations");
	 foreach(AnimationClip clip in armClips) {
		AnimatorState state = stateD.AddState(clip.name);
		state.motion = clip;
		stateD.defaultState.AddTransition(state).AddCondition(AnimatorConditionMode.Equals, count, AvatarController.TEA_ANIM_PARAM);
		state.AddExitTransition().AddCondition(AnimatorConditionMode.NotEqual, count, AvatarController.TEA_ANIM_PARAM);

		Dropdown.OptionData option = new Dropdown.OptionData(clip.name);
		options.Add(option);
		count++;
	 }//for

	 manager.GetComponent<AvatarController>().TEA_HAND_LAYER_COUNT = count;

	 // --- Full Body Animations
	 AnimatorStateMachine stateM = new AnimatorStateMachine();
	 teaAnimContr.AddLayer(new AnimatorControllerLayer() {
		name = AvatarController.TEA_LAYER,
		defaultWeight = 0,
		avatarMask = manager.AvatarMaskAll,
		stateMachine = stateM
	 });

	 stateM.defaultState = stateM.AddState("Default");

	 // dynamic
	 foreach(string folder in AssetDatabase.GetSubFolders("Assets/TEA Manager/Resources/Animation/TEA Animations")) {
		string name = folder.Substring(folder.LastIndexOf('/') + 1);
		if("TEA Hand Animations" == name)
		 continue;
		Dropdown.OptionData option = new Dropdown.OptionData(name);
		options.Add(option);
		AnimationClip start = null;
		AnimationClip loop = null;
		foreach(AnimationClip clip in GetAnimationClips(folder)) {
		 if(clip.name.Contains("intro") || clip.name.Contains("Intro")) {
			start = clip;
		 } else
			loop = clip;
		}//for
		AnimatorState state = stateM.AddState(name);
		state.motion = loop;
		if(null != start) {
		 AnimatorState startState = stateM.AddState(name + "-intro");
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
	 return teaAnimContr;
	}

	#region
	private void GetBehaviours(RuntimeAnimatorController runController, AnimatorController controller, TEA_PlayableLayerData layerData, VRCAvatarDescriptor.AnimLayerType type) {
	 AnimatorController runCont = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(runController));
	 int layerC = 0;
	 foreach(AnimatorControllerLayer layer in runCont.layers) {
		AnimatorControllerLayer copyLayer = controller.layers[layerC];
		int stateC = 0;
		foreach(ChildAnimatorState state in layer.stateMachine.states) {
		 ChildAnimatorState copyState = copyLayer.stateMachine.states[stateC];
		 int behC = 0;
		 foreach(StateMachineBehaviour beh in state.state.behaviours) {
			//Debug.Log($"getting avatar[{currentAvatar.name}] copyState[{copyState.state.name}] state[{state.state.name}] behC[{behC}] count[{copyState.state.behaviours.Length}]");
			if(beh is VRCPlayableLayerControl) {
			 VRCPlayableLayerControl pc = (VRCPlayableLayerControl)beh;
			 TEA_PlayableLayerControl tc = copyState.state.AddStateMachineBehaviour<TEA_PlayableLayerControl>();
			 tc.blendDuration = pc.blendDuration;
			 tc.debugString = pc.debugString;
			 tc.goalWeight = pc.goalWeight;
			 tc.layer = pc.layer;
			 tc.state = copyState.state.name;
			} else if(beh is VRCAvatarParameterDriver) {
			 VRCAvatarParameterDriver vd = (VRCAvatarParameterDriver)beh;
			 TEA_AvatarParameterDriver td = copyState.state.AddStateMachineBehaviour<TEA_AvatarParameterDriver>();
			 td.name = vd.name;
			 td.debugString = vd.debugString;
			 td.localOnly = vd.localOnly;
			 td.parameters = new List<VRC.SDKBase.VRC_AvatarParameterDriver.Parameter>();
			 foreach(VRCAvatarParameterDriver.Parameter param in vd.parameters) {
				td.parameters.Add(new VRCAvatarParameterDriver.Parameter() {
				 chance = param.chance,
				 name = param.name,
				 type = param.type,
				 value = param.value,
				 valueMax = param.valueMax,
				 valueMin = param.valueMin
				});
				td.state = copyState.state.name;
				//--- validation ---
			 }
			 ValidateParameterDriver((VRCAvatarParameterDriver)beh, type, layer, state.state);
			}
			behC++;
		 }//for behavior
		 stateC++;
		}//for state
		layerC++;
	 }//for layer

	}

	struct DriverIssue {
	 public VRCAvatarParameterDriver driver;
	 public string layerName;
	 public VRCAvatarDescriptor.AnimLayerType layerType;
	 public AnimatorState state;
	}
	List<DriverIssue> drivers;
	private void ValidateParameterDriver(VRCAvatarParameterDriver d, VRCAvatarDescriptor.AnimLayerType type, AnimatorControllerLayer layer, AnimatorState state) {
	 drivers.Add(new DriverIssue {
		driver = d,
		layerName = layer.name,
		layerType = type,
		state = state
	 });
	}

	public static void RemoveBehaviour(AnimatorState state, StateMachineBehaviour behaviour) {

	 if(state != null) {
		StateMachineBehaviour[] theBehaviours = state.behaviours;

		ArrayUtility.Remove(ref theBehaviours, behaviour);

		Undo.RegisterCompleteObjectUndo(state, "Removed behaviour");

		Undo.DestroyObjectImmediate(behaviour);

		state.behaviours = theBehaviours;
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
	 if(null == animator || null == superAnimator)
		return;

	 foreach(AnimatorControllerParameter param in animator.parameters) {
		if(!string.IsNullOrEmpty(param.name) && !HasAnimatorParameter(superAnimator, param.name))
		 superAnimator.AddParameter(param);
	 }

	 foreach(AnimatorControllerLayer layer in animator.layers) {
		//Debug.Log($"combining [{superAnimator.name}] with [{animator.name}] layer[{layer.name}]");
		AnimatorControllerLayer newLayer = new AnimatorControllerLayer {
		 avatarMask = null == mask ? layer.avatarMask : mask,
		 name = layer.name,
		 blendingMode = layer.blendingMode,
		 defaultWeight = layer.defaultWeight,
		 iKPass = layer.iKPass,
		 stateMachine = layer.stateMachine,
		 syncedLayerAffectsTiming = layer.syncedLayerAffectsTiming,
		 syncedLayerIndex = layer.syncedLayerIndex
		};
		superAnimator.AddLayer(newLayer);
	 }
	 //Debug.Log("-----------");
	}

	// ----- ------ Default Clips ----- ------
	private static AnimationClip SetAnimationDefault(AnimatorController superAnimator, GameObject gameObject, AvatarMask mask, string folder) {
	 AnimatorControllerLayer animDefault = new AnimatorControllerLayer {
		name = "TEA Defaults",
		defaultWeight = 1,
		avatarMask = mask,
		stateMachine = new AnimatorStateMachine()
	 };

	 AnimatorStateMachine stateMachine = animDefault.stateMachine;
	 AnimationClip def_clip = new AnimationClip {
		name = "TEA Defaults"
	 };

	 AnimatorState defaultState = stateMachine.AddState("TEA Defaults");

	 foreach(AnimatorControllerLayer layer in superAnimator.layers) {
		SetAnimationDefault(def_clip, layer.stateMachine.states, gameObject);
		foreach(ChildAnimatorStateMachine childMachine in layer.stateMachine.stateMachines) {
		 SetAnimationDefault(def_clip, childMachine.stateMachine.states, gameObject);
		}
	 }

	 string actionPath = folder + "/" + superAnimator.name + "-def_clip.anim";
	 AssetDatabase.CreateAsset(def_clip, actionPath);
	 AnimationClip retClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(actionPath);
	 defaultState.writeDefaultValues = true;
	 defaultState.motion = retClip;

	 AnimatorControllerLayer[] layers = superAnimator.layers;
	 ArrayUtility.Insert<AnimatorControllerLayer>(ref layers, 0, animDefault);
	 superAnimator.layers = layers;

	 return retClip;
	}

	private static void SetAnimationDefault(AnimationClip def_clip, ChildAnimatorState[] states, GameObject gameObject) {
	 foreach(ChildAnimatorState state in states) {
		state.state.writeDefaultValues = true;
		SetAnimationDefaultMotion(state.state.motion, def_clip, gameObject);
	 }
	}

	private static void SetAnimationDefaultMotion(Motion motion, AnimationClip def_clip, GameObject gameObject) {
	 if(null != motion) {
		if(motion is BlendTree) {
		 BlendTree bTree = (BlendTree)motion;
		 foreach(ChildMotion child in bTree.children) {
			SetAnimationDefaultMotion(child.motion, def_clip, gameObject);
		 }
		} else if(motion is AnimationClip) {
		 AnimationClip clip = (AnimationClip)motion;

		 foreach(EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)) {
			if(null == binding || binding.type == typeof(Animator))
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
		if(parameter.name == name)
		 return true;
	 }
	 return false;
	}
	#endregion

	// --- --- Validation --- ---
	#region
	private void ValidateCustomExpressions(VRCAvatarDescriptor avatar, AnimatorController superAnimator) {
	 if(avatar.customExpressions) {

		if(null == avatar.expressionsMenu)
		 issues.ExpressionsMenu.Add(new Issue("CustomExpressions is set but you have no ExpressionsMenu", avatar));

		if(null == avatar.expressionParameters)
		 issues.ExpressionParameters.Add(new Issue("CustomExpressions is set but you have no ExpressionParameters", avatar));

		if(null == avatar.expressionParameters || null == avatar.expressionsMenu)
		 return;

		EMenus = new List<VRCExpressionsMenu>();
		EMenusText = new List<string>();
		ValidateExpressionsMenu(avatar, avatar.expressionsMenu);

		foreach(VRCExpressionParameters.Parameter parameter in avatar.expressionParameters.parameters) {
		 bool exists = false;
		 if(string.IsNullOrEmpty(parameter.name))
			continue;
		 foreach(AnimatorControllerParameter aParam in superAnimator.parameters) {
			if(aParam.name == parameter.name) {
			 exists = true;
			 break;
			}
		 }//for aParam
		 if(!exists)
			issues.ParametersNotInAnimators.Add(parameter.name);
		}//for parameter
	 }
	}

	List<VRCExpressionsMenu> EMenus;
	List<string> EMenusText;
	int eMenuIndent = 0;
	private void ValidateExpressionsMenu(VRCAvatarDescriptor avatar, VRCExpressionsMenu menu) {
	 foreach(VRCExpressionsMenu.Control control in menu.controls) {
		if(control.type == VRCExpressionsMenu.Control.ControlType.Button) {
		 if(string.IsNullOrEmpty(control.parameter.name))
			issues.ExpressionsMenu.Add(new Issue($"Button[{control.name}] has no parameter", menu));
		 else if(null == avatar.expressionParameters.FindParameter(control.parameter.name))
			issues.ExpressionsMenu.Add(new Issue($"Button[{control.name}] parameter[{control.parameter.name}] not in ExpressionParameters", menu));
		}
		if(control.type == VRCExpressionsMenu.Control.ControlType.Toggle) {
		 if(string.IsNullOrEmpty(control.parameter.name))
			issues.ExpressionsMenu.Add(new Issue($"Toggle[{control.name}] has no parameter", menu));
		 else if(null == avatar.expressionParameters.FindParameter(control.parameter.name))
			issues.ExpressionsMenu.Add(new Issue($"Toggle[{control.name}] parameter[{control.parameter.name}] not in ExpressionParameters", menu));
		}
		if(control.type == VRCExpressionsMenu.Control.ControlType.RadialPuppet) {
		 if(1 > control.subParameters.Length)
			issues.ExpressionsMenu.Add(new Issue($"Radial Puppet[{control.name}] has no sub parameters", menu));
		 int count = 0;
		 foreach(VRCExpressionsMenu.Control.Parameter param in control.subParameters) {
			if(!string.IsNullOrEmpty(param.name))
			 count++;
			if(null == avatar.expressionParameters.FindParameter(param.name))
			 issues.ExpressionsMenu.Add(new Issue($"Radial Puppet[{control.name}] parameter[{param.name}] not in ExpressionParameters", menu));
		 }
		 if(count < 1)
			issues.ExpressionsMenu.Add(new Issue($"Radial Puppet[{control.name}] has no sub parameters", menu));
		}
		if(control.type == VRCExpressionsMenu.Control.ControlType.FourAxisPuppet) {
		 if(4 != control.subParameters.Length)
			issues.ExpressionsMenu.Add(new Issue($"Four Axis Puppet[{control.name}] less than 4 sub parameters", menu));
		 int count = 0;
		 foreach(VRCExpressionsMenu.Control.Parameter param in control.subParameters) {
			if(!string.IsNullOrEmpty(param.name))
			 count++;
			else
			 continue;

			if(null == avatar.expressionParameters.FindParameter(param.name))
			 issues.ExpressionsMenu.Add(new Issue($"Four Axis Puppet[{control.name}] parameter[{param.name}] not in ExpressionParameters", menu));
		 }
		 if(count < 4)
			issues.ExpressionsMenu.Add(new Issue($"Four Axis Puppet[{control.name}] less than 4 sub parameters", menu));
		}
		if(control.type == VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet) {
		 if(1 > control.subParameters.Length)
			issues.ExpressionsMenu.Add(new Issue($"Two Axis Puppet[{control.name}] has no sub parameters", menu));
		 int count = 0;
		 foreach(VRCExpressionsMenu.Control.Parameter param in control.subParameters) {
			if(!string.IsNullOrEmpty(param.name))
			 count++;
			else
			 continue;

			if(null == avatar.expressionParameters.FindParameter(param.name))
			 issues.ExpressionsMenu.Add(new Issue($"Two Axis Puppet[{control.name}] parameter[{param.name}] not in ExpressionParameters", menu));
		 }
		 if(count < 1)
			issues.ExpressionsMenu.Add(new Issue($"Two Axis Puppet[{control.name}] has no sub parameters", menu));
		}
		if(control.type == VRCExpressionsMenu.Control.ControlType.SubMenu) {
		 if(null == control.subMenu)
			issues.ExpressionsMenu.Add(new Issue($"Sub Menu[{control.name}] is blank", menu));
		 else {
			EMenus.Add(menu);
			EMenusText.Add(new string('+', eMenuIndent) + menu.name);
			if(EMenus.Find(m => m == control.subMenu)) {
			 string text = $"Loop detected in Menu[{menu.name}] Sub Menu[{control.name}][{control.subMenu.name}].\n"
				+ "This will cause an infinite loop on play.\n\n";
			 foreach(string lMenu in EMenusText) {
				text += $"{lMenu}\n";
			 }
			 EditorUtility.DisplayDialog($"Expressions Menu Loop in [{currentAvatar.gameObject.name}]", text, "Cancel");
			 throw new TEA_Exception("Infinite Expressions Menu Loop");
			} else {
			 eMenuIndent++;
			 ValidateExpressionsMenu(avatar, control.subMenu);
			 eMenuIndent--;
			}
		 }
		}
	 }
	}

	private void ValidateOnlyTransforms(VRCAvatarDescriptor.AnimLayerType layerName, AnimatorController controller) {
	 bool onlyTransfroms = layerName != VRCAvatarDescriptor.AnimLayerType.FX;
	 if(!controller)
		return;
	 foreach(AnimatorControllerLayer layer in controller.layers) {
		ValidateOnlyTransforms(onlyTransfroms, layerName, layer.stateMachine.states, layer.name);
		foreach(ChildAnimatorStateMachine childMachine in layer.stateMachine.stateMachines) {
		 ValidateOnlyTransforms(onlyTransfroms, layerName, childMachine.stateMachine.states, layer.name);
		}
	 }
	}

	private void ValidateOnlyTransforms(bool onlyTransfroms, VRCAvatarDescriptor.AnimLayerType layerName, ChildAnimatorState[] states, string animLayerName) {
	 foreach(ChildAnimatorState state in states) {
		Motion m = state.state.motion;
		Issue issue = ValidateOnlyTransformsMotion(onlyTransfroms, layerName, m);
		if(null != issue) {
		 if(onlyTransfroms) {
			issue.Cause = $"Layer[{animLayerName}]: Motion contains non-Transformations";
			issue.Reference.Add(state.state);
			issues.GetLayer(layerName).Insert(0, issue);
		 } else {
			issue.Cause = $"Layer[{animLayerName}]: Motion contains Transformations";
			issue.Reference.Add(state.state);
			issues.GetLayer(layerName).Insert(0, issue);
		 }
		}
	 }
	}

	private Issue ValidateOnlyTransformsMotion(bool onlyTransfroms, VRCAvatarDescriptor.AnimLayerType layerName, Motion motion) {
	 Issue issue = new Issue();
	 if(!motion)
		return null;
	 else if(motion is BlendTree) {
		BlendTree bTree = (BlendTree)motion;
		bool hadIssue = false;
		foreach(ChildMotion child in bTree.children) {
		 Issue mIssue = ValidateOnlyTransformsMotion(onlyTransfroms, layerName, child.motion);
		 if(null != mIssue) {
			issue.Reference.AddRange(mIssue.Reference);
			hadIssue = true;
		 }
		}
		if(hadIssue)
		 return issue;
	 } else if(motion is AnimationClip) {
		AnimationClip clip = (AnimationClip)motion;
		foreach(EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)) {
		 if(onlyTransfroms != (binding.type == typeof(Transform) || binding.type == typeof(Animator))) {
			//string issue = onlyTransfroms ? "Transforms" : "Non-Transforms";
			//Debug.LogWarning($"[{clip.name}] in {layerName} layer contains {issue}");
			issue.Reference.Add(clip);
			return issue;
		 }
		}
	 }
	 return null;
	}
	#endregion
 }//class
}//namespace