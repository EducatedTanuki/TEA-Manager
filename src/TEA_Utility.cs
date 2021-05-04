using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;

namespace TEA {
 public class TEA_Utility {
	public static readonly string ASSETS = "Assets";
	public static readonly string ASSETS_CONTENT = "Assets/";

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
 }
}