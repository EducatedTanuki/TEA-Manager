using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System;
using System.Reflection;
using VRC.SDKBase;

namespace TEA {
 public class DynamicBoneCopy : MonoBehaviour {

	static VRCAvatarDescriptor source;
	static VRCAvatarDescriptor destination;

	[MenuItem("GameObject/TEA Functions/Copy Dynamic Bones and Colliders", false, 100)]
	static void Copy() {
	 GameObject selected = Selection.activeGameObject;
	 Component[] avatars = selected.GetComponentsInParent(typeof(VRCAvatarDescriptor), true);
	 if(0 == avatars.Length) {
		EditorUtility.DisplayDialog("Copy Bones", $"{selected.name} is not the child of an avatar", "Cancel");
		return;
	 } else if(1 < avatars.Length) {
		EditorUtility.DisplayDialog("Copy Bones", $"{selected.name} is child of multiple avatars", "Cancel");
		return;
	 }

	 source = (VRCAvatarDescriptor)avatars[0];
	}

	[MenuItem("GameObject/TEA Functions/Paste Dynamic Bones and Colliders", false, 101)]
	static void Paste() {
	 if(null == source) {
		EditorUtility.DisplayDialog("Paste Bones", $"Select a source first", "Cancel");
		return;
	 }

	 if(Selection.gameObjects.Length == 0) {
		EditorUtility.DisplayDialog("Paste Bones", $"Opps nothing selected", "Cancel");
		return;
	 }

	 GameObject selected = Selection.activeGameObject;
	 Component[] avatars = selected.GetComponentsInParent(typeof(VRCAvatarDescriptor), true);
	 if(0 == avatars.Length) {
		EditorUtility.DisplayDialog("Paste Bones", $"{selected.name} is not the child of an avatar", "Cancel");
		return;
	 }

	 destination = (VRCAvatarDescriptor)avatars[0];

	 Paste(source.gameObject, destination.gameObject);
	 PasteBones(source.gameObject, destination.gameObject);
	}

	static void Paste(GameObject sourceObj, GameObject destObj) {
	 CopyDymamicBoneCollider(sourceObj, destObj);

	 int childCount = destObj.transform.childCount;
	 for(int cIndex = 0; cIndex < childCount; cIndex++) {
		Transform child = destObj.transform.GetChild(cIndex);
		Transform sObj = sourceObj.transform.Find(child.name);

		if(null == sObj) {
		 Debug.Log($"{destObj.name} does not exist in source");
		 continue;
		}

		Paste(sObj.gameObject, child.gameObject);
	 }
	}

	private static void CopyDymamicBoneCollider(GameObject sourceObj, GameObject destObj) {
	 Component[] comps = sourceObj.GetComponents<DynamicBoneCollider> ();

	 if(null != destObj.GetComponent<DynamicBoneCollider>()) {
		Debug.Log($"skipping collider for {destObj.name} as one already exists");
		return;
	 }

	 for(int i = 0; i < comps.Length; i++) {
		DynamicBoneCollider dbc = destObj.AddComponent<DynamicBoneCollider>();
		foreach(FieldInfo field in typeof(DynamicBoneCollider).GetFields()) {
		 if(field.DeclaringType == typeof(Behaviour) || field.DeclaringType == typeof(Component) || field.DeclaringType == typeof(UnityEngine.Object)
										|| (!(field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), false).Length > 0))
									 )
			continue;

		 field.SetValue(dbc, field.GetValue(comps[i]));
		}

		foreach(PropertyInfo property in typeof(DynamicBoneCollider).GetProperties()) {
		 if(property.DeclaringType == typeof(Behaviour) || property.DeclaringType == typeof(Component) || property.DeclaringType == typeof(UnityEngine.Object))
			continue;
		 property.SetValue(dbc, property.GetValue(comps[i]));
		}
	 }
	}

	static void PasteBones(GameObject sourceObj, GameObject destObj) {
	 CopyDymamicBones(sourceObj, destObj);

	 int childCount = destObj.transform.childCount;
	 for(int cIndex = 0; cIndex < childCount; cIndex++) {
		Transform child = destObj.transform.GetChild(cIndex);
		Transform sObj = sourceObj.transform.Find(child.name);

		if(null == sObj) {
		 Debug.Log($"{destObj.name} does not exist in source");
		 continue;
		}

		PasteBones(sObj.gameObject, child.gameObject);
	 }
	}

	private static void CopyDymamicBones(GameObject sourceObj, GameObject destObj) {
	 DynamicBone[] comps = sourceObj.GetComponents<DynamicBone>();

	 if(null != destObj.GetComponent<DynamicBone>()) {
		Debug.Log($"skipping collider for {destObj.name} as one already exists");
		return;
	 }

	 for(int i = 0; i < comps.Length; i++) {
		DynamicBone dbc = destObj.AddComponent<DynamicBone>();
		foreach(FieldInfo field in typeof(DynamicBone).GetFields()) {
		 if(field.DeclaringType == typeof(Behaviour) || field.DeclaringType == typeof(Component) || field.DeclaringType == typeof(UnityEngine.Object)
										|| (!(field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), false).Length > 0))
									 )
			continue;

		 field.SetValue(dbc, field.GetValue(comps[i]));
		}

		foreach(PropertyInfo property in typeof(DynamicBone).GetProperties()) {
		 if(property.DeclaringType == typeof(Behaviour) || property.DeclaringType == typeof(Component) || property.DeclaringType == typeof(UnityEngine.Object))
			continue;
		 property.SetValue(dbc, property.GetValue(comps[i]));
		}

		dbc.m_Root = GetRoot(comps[i].m_Root);
		dbc.m_Colliders = GetColliders(comps[i]);
		dbc.m_Exclusions = GetExclusions(comps[i]);
		//dbc.m_ReferenceObject = GetRoot(comps[i].m_ReferenceObject);
	 }
	}

	private static Transform GetRoot(Transform root) {
	 Transform ret = destination.transform;
	 Stack<string> stack = new Stack<string>();

	 if(root.gameObject != source.gameObject) {
		Transform t = root;
		while(null == t.gameObject.GetComponent<VRCAvatarDescriptor>()) {
		 stack.Push(t.gameObject.name);
		 t = t.transform.parent;
		}
	 }

	 foreach(string oName in stack) {
		Debug.Log($"traversing: {oName}");
		ret = ret.Find(oName);
	 }

	 return ret;
	}

	private static List<Transform> GetExclusions(DynamicBone component) {
	 List<Transform> list = new List<Transform>();
	 foreach(Transform dbc in component.m_Exclusions) {
		Stack<string> stack = new Stack<string>();

		if(dbc.gameObject != source.gameObject) {
		 Transform t = dbc.transform;
		 while(null == t.gameObject.GetComponent<VRCAvatarDescriptor>()) {
			stack.Push(t.gameObject.name);
			t = t.transform.parent;
		 }
		}

		GameObject obj = destination.gameObject;
		foreach(string oName in stack) {
		 Debug.Log($"traversing: {oName}");
		 obj = obj.transform.Find(oName).gameObject;
		}

		list.Add(obj.transform);
	 }
	 return list;
	}

	private static List<DynamicBoneColliderBase> GetColliders(DynamicBone component) {
	 List<DynamicBoneColliderBase> list = new List<DynamicBoneColliderBase>();
	 foreach (DynamicBoneColliderBase dbc in component.m_Colliders) {
		Stack<string> stack = new Stack<string>();

		if(dbc.gameObject != source.gameObject) {
		 Transform t = dbc.transform;
		 while(null == t.gameObject.GetComponent<VRCAvatarDescriptor>()) {
			stack.Push(t.gameObject.name);
			t = t.transform.parent;
		 }
		}

		GameObject obj = destination.gameObject;
		foreach(string oName in stack) {
		 Debug.Log($"traversing: {oName}");
		 obj = obj.transform.Find(oName).gameObject;
		}

		list.Add(obj.GetComponent<DynamicBoneCollider>());
	 }
	 return list;
	}

	static bool Validate(GameObject destination) {
	 return true;
	}

	static void Error(String msg) {
	 EditorUtility.DisplayDialog("Error", msg, "ok");
	}

 }
}