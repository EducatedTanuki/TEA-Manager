using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using UnityEditor.Animations;

namespace TEA {
 public class TEA_EditorUtility {
	public static readonly string ASSETS = "Assets";
	public static readonly string ASSETS_CONTENT = "Assets/";

	// ----- ------ Settings ----- -----
	public static readonly string DEFAULT_SETTINGS = "TEA Manager/Default Settings.asset";

	public static TEA_Settings GetTEA_Settings() {
	 var assets = AssetDatabase.FindAssets("t:TEA_Settings");

	 string defaultGUID = null;
	 string firstGUID = null;
	 foreach(string file in assets) {
		if(AssetDatabase.GUIDToAssetPath(file).EndsWith(DEFAULT_SETTINGS))
		 defaultGUID = file;
		else if(string.IsNullOrEmpty(firstGUID))
		 firstGUID = file;
	 }
	 if(string.IsNullOrEmpty(defaultGUID)) {
		EditorUtility.DisplayDialog("TEA Settings Error", "There is no default Settings file in your Assets."
		 + $"\nPlease re-import the TEA Manager unity package and make sure [{DEFAULT_SETTINGS}] exists", "Continue");
		throw new TEA_Exception("Default Settings file is missing");
	 }

	 if(assets.Length == 1)
		return CreateSettings(defaultGUID);

	 string path = AssetDatabase.GUIDToAssetPath(firstGUID);
	 if(assets.Length > 2) {
		int delete = EditorUtility.DisplayDialogComplex("TEA Settings", $"There are multiple settings files present."
		 + $"\n'Continue' use [{path}]"
		 + $"\n'Delete Extra' delete all but [{path}]"
		 + $"\n'Delete All' will create a new settings file under {ASSETS_CONTENT}"
		 , "Delete All", "Delete Extra", "Continue");
		if(delete == 0) {
		 DeleteSettings(defaultGUID);
		 return CreateSettings(defaultGUID);
		} else if(delete == 1)
		 DeleteSettings(defaultGUID, firstGUID);
	 }
	 return AssetDatabase.LoadAssetAtPath<TEA_Settings>(path);
	}

	private static TEA_Settings CreateSettings(string defaultSettingsGUID) {
	 Debug.Log($"Creating setting file at [{ASSETS_CONTENT}]");
	 string settingsPath = GetAssetPath(ASSETS, "TEA_Settings");
	 AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(defaultSettingsGUID), settingsPath);
	 return AssetDatabase.LoadAssetAtPath<TEA_Settings>(settingsPath);
	}

	private static void DeleteSettings(params string[] exeptions) {
	 var assets = AssetDatabase.FindAssets("t:TEA_Settings");
	 for(int i = 0; i < assets.Length; i++) {
		string path = AssetDatabase.GUIDToAssetPath(assets[i]);
		if(ArrayUtility.FindIndex<string>(exeptions, guid => guid == assets[i]) < 0)
		 AssetDatabase.DeleteAsset(path);
	 }
	}
	// ----- ------ Camera ----- -----
	public static Dictionary<string, Camera> GetCameras() {
	 Dictionary<string, Camera> cameras = new Dictionary<string, Camera>();
	 for(int i = 0; i < SceneManager.sceneCount; i++) {
		Scene scene = SceneManager.GetSceneAt(i);
		if(!scene.isLoaded)
		 continue;

		GameObject[] rootObjects = scene.GetRootGameObjects();
		foreach(GameObject root in rootObjects) {
		 Camera cam = root.GetComponent<Camera>();
		 if(null != cam) {
			cameras.Add(scene.name + "| " + cam.name, cam);
		 }
		}
	 }
	 return cameras;
	}

	// ----- ------ Avatar ----- -----
	public static Dictionary<string, VRCAvatarDescriptor> GetAvatars(out bool crossScene) {
	 crossScene = false;
	 Dictionary<string, VRCAvatarDescriptor> avatars = new Dictionary<string, VRCAvatarDescriptor>();
	 for(int i = 0; i < SceneManager.sceneCount; i++) {
		Scene scene = SceneManager.GetSceneAt(i);
		if(!scene.isLoaded)
		 continue;

		GameObject[] rootObjects = scene.GetRootGameObjects();
		foreach(GameObject root in rootObjects) {
		 VRCAvatarDescriptor avatar = root.GetComponent<VRCAvatarDescriptor>();
		 if(null != avatar) {
			avatars.Add(TEA_Manager.GetSceneAvatarKey(scene, avatar), avatar);
			if(scene != SceneManager.GetActiveScene())
			 crossScene = true;
		 }
		}
	 }
	 return avatars;
	}

	public static Dictionary<string, VRCAvatarDescriptor> GetAvatars(Scene scene) {
	 Dictionary<string, VRCAvatarDescriptor> avatars = new Dictionary<string, VRCAvatarDescriptor>();
	 if(!scene.isLoaded) {
		Debug.Log("Scene is not loaded");
		return avatars;
	 }
	 GameObject[] rootObjects = scene.GetRootGameObjects();
	 foreach(GameObject root in rootObjects) {
		VRCAvatarDescriptor avatar = root.GetComponent<VRCAvatarDescriptor>();
		if(null != avatar) {
		 avatars.Add(avatar.gameObject.name, avatar);
		}
	 }

	 return avatars;
	}

	public static VRCAvatarDescriptor HasAvatars(Scene scene) {
	 if(!scene.isLoaded) {
		Debug.Log("Scene is not loaded");
		return null;
	 }
	 GameObject[] rootObjects = scene.GetRootGameObjects();
	 foreach(GameObject root in rootObjects) {
		VRCAvatarDescriptor avatar = root.GetComponent<VRCAvatarDescriptor>();
		if(null != avatar) {
		 return avatar;
		}
	 }

	 return null;
	}

	public static void CopyPlayableLayer(VRCAvatarDescriptor vrcd, TEA_Settings settings, string controller_folder, string animation_folder) {
	 CopyPlayableLayer(vrcd, 0, controller_folder, animation_folder, AssetDatabase.GetAssetPath(settings.Base), settings.BaseCopy, settings.BaseCopyAnimations);
	 CopyPlayableLayer(vrcd, 1, controller_folder, animation_folder, AssetDatabase.GetAssetPath(settings.Additive), settings.AdditiveCopy, settings.AdditiveCopyAnimations);
	 CopyPlayableLayer(vrcd, 2, controller_folder, animation_folder, AssetDatabase.GetAssetPath(settings.Gesture), settings.GestureCopy, settings.GestureCopyAnimations);
	 CopyPlayableLayer(vrcd, 3, controller_folder, animation_folder, AssetDatabase.GetAssetPath(settings.Action), settings.ActionCopy, settings.ActionCopyAnimations);
	 CopyPlayableLayer(vrcd, 4, controller_folder, animation_folder, AssetDatabase.GetAssetPath(settings.FX), settings.FXCopy, settings.FXCopyAnimations);
	}

	public static void CopyPlayableLayer(VRCAvatarDescriptor vrcd, int layer, string controller_folder, string animation_folder, string sourceController, bool copy, bool copyAnim) {
	 string controllerPath = GetPath(false, controller_folder, vrcd.baseAnimationLayers[layer].type + ".controller");
	 AnimatorController controller;
	 if(copy) {
		AssetDatabase.CopyAsset(sourceController, controllerPath);
		controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
	 } else
		controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(sourceController);

	 vrcd.baseAnimationLayers[layer].isDefault = false;
	 vrcd.baseAnimationLayers[layer].isEnabled = true;
	 vrcd.baseAnimationLayers[layer].animatorController = controller;

	 if(copy) {
		CopyMotions(controller, animation_folder, copyAnim);
	 }
	}

	// ------ ------ Transform ----- -----
	public static Vector3 TransformPoint(Transform root, Transform child) {
	 return child.position + root.position;
	}

	public static Vector3 TransformPoint(Transform root, Vector3 other) {
	 return other + root.position;
	}

	public static Vector3 InverseTransformPoint(Transform root, Transform world) {
	 return world.position - root.position;
	}

	public static Vector3 InverseTransformPoint(Transform root, Vector3 world) {
	 return world - root.position;
	}

	// ------ ------ Animator ----- -----
	public static bool HasAnimatorParameter(string name, AnimatorControllerParameter[] parameters) {
	 foreach(AnimatorControllerParameter parameter in parameters) {
		if(parameter.name == name)
		 return true;
	 }
	 return false;
	}

	public static void CopyMotions(AnimatorController controller, string path, bool copyAnim) {
	 foreach(AnimatorControllerLayer layer in controller.layers) {
		CopyMotions(layer.stateMachine, path, copyAnim);
		foreach(ChildAnimatorStateMachine cSM in layer.stateMachine.stateMachines) {
		 CopyMotions(cSM.stateMachine, path, copyAnim);
		}
	 }
	 UnityEditor.EditorUtility.SetDirty(controller);
	 AssetDatabase.SaveAssets();
	}

	public static void CopyMotions(AnimatorStateMachine stateMachine, string path, bool copyAnim) {
	 foreach(ChildAnimatorState state in stateMachine.states) {
		if(null == state.state.motion)
		 continue;
		//Debug.Log($"BlendTree[{state.state.motion is BlendTree}] isSubAsset[{!AssetDatabase.IsSubAsset(state.state.motion)}]");
		if(copyAnim && state.state.motion is AnimationClip && !AssetDatabase.IsSubAsset(state.state.motion)) {
		 string newAnimPath = GetPath(false, path, state.state.motion.name + ".anim");
		 AnimationClip animClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newAnimPath);
		 if(null == animClip) {
			AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(state.state.motion), newAnimPath);
			animClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newAnimPath);
		 }
		 state.state.motion = animClip;
		} else if(state.state.motion is BlendTree && !AssetDatabase.IsSubAsset(state.state.motion)) {
		 string newTreePath = GetAssetPath(path, state.state.motion.name);
		 BlendTree blendTree = AssetDatabase.LoadAssetAtPath<BlendTree>(newTreePath);
		 if(null == blendTree) {
			AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(state.state.motion), newTreePath);
			blendTree = AssetDatabase.LoadAssetAtPath<BlendTree>(newTreePath);
		 }
		 state.state.motion = blendTree;
		}
	 }
	}

	// ----- ------ Pathing ----- -----
	public static string GetPath(bool keepslash, params string[] pathParts) {
	 string path = GetPath(pathParts);
	 if(!keepslash)
		return Regex.Replace(path, @"/$", "");
	 else if(!path.EndsWith("/"))
		path += "/";
	 return path;
	}

	public static string GetPath(params string[] pathParts) {
	 string path = "";
	 foreach(string part in pathParts) {
		if(null == part)
		 continue;
		if(path.Length > 0 && !path.EndsWith("/"))
		 path += "/";
		path += part;
	 }
	 return path;
	}

	public static string GetAssetPath(string parent, string name) {
	 return GetPath(false, parent, name + ".asset");
	}

	public static void CreateAsset(UnityEngine.Object sibling, UnityEngine.Object newAsset, string name) {
	 AssetDatabase.CreateAsset(newAsset, GetAssetDirectory(sibling, true) + name);
	}

	public static string GetAssetDirectory(UnityEngine.Object obj, bool keepSlash) {
	 if(keepSlash)
		return Regex.Replace(AssetDatabase.GetAssetPath(obj), @"[^/]+\..*$", "");
	 else
		return Regex.Replace(AssetDatabase.GetAssetPath(obj), @"/[^/]+\..*$", "");
	}

	public static string GetParentPath(string asset, bool keepSlash) {
	 if(keepSlash)
		return Regex.Replace(asset, @"[^/]+\..*$", "");
	 else
		return Regex.Replace(asset, @"/[^/]+\..*$", "");
	}
 }
}