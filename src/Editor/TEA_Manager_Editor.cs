using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static TEA.TEA_EditorUtility;
using System;
using VRC.SDKBase;

namespace TEA {
 [CustomEditor(typeof(TEA_Manager))]
 public class TEA_Manager_Editor : Editor {
	// ----- ----- TAMS Editor ----- -----
	public static readonly string MENU_ITEM = "Tanuki's Educated Avatar Manager";

	bool _show;

	// --- Banner ---
	private static GUIContent bannerContent = new GUIContent {
	 image = null
	};

	public override void OnInspectorGUI() {
	 // -- Banner --
	 var assets = AssetDatabase.FindAssets("TEA_Manager_Banner");
	 if(null != assets && 0 < assets.Length)
		bannerContent.image = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(assets[0]));
	 else
		Debug.LogError("banner is not found");

	 //----------------
	 EditorGUILayout.BeginVertical();
	 EditorGUILayout.LabelField(MENU_ITEM, EditorStyles.boldLabel);
	 GUILayout.Box(bannerContent.image, GUILayout.Width(2048 / 4), GUILayout.Height(256 / 3.7f));
	 EditorGUILayout.EndVertical();
	 //------
	 if(GUILayout.Button("Tanukis Only"))
		_show = !_show;

	 if(_show)
		base.OnInspectorGUI();
	}

	[MenuItem("GameObject/TEA Functions", false, 0)]
	public static void TeaFunctions() {

	}

	// ----- ----- Avatar Setup Methods ----- 
	private static readonly string SET_EYE_LOOK = "Set Eye Look as default";

	[MenuItem("GameObject/TEA Functions/Set Eye Look as default", false, 2)]
	public static void SetEyeLook() {
	 if(!SetEyeLookCheck())
		return;

	 GameObject newAvatar = Selection.activeGameObject;
	 VRCAvatarDescriptor descriptor = newAvatar.GetComponent<VRCAvatarDescriptor>();
	 TEA_Settings settings = GetTEA_Settings();
	 settings.EyeLookUpLeft = descriptor.customEyeLookSettings.eyesLookingUp.left;
	 settings.EyeLookUpRight = descriptor.customEyeLookSettings.eyesLookingUp.right;
	 settings.EyeLookDownLeft = descriptor.customEyeLookSettings.eyesLookingDown.left;
	 settings.EyeLookDownRight = descriptor.customEyeLookSettings.eyesLookingDown.right;
	 settings.EyeLookLeftLeft = descriptor.customEyeLookSettings.eyesLookingLeft.left;
	 settings.EyeLookLeftRight = descriptor.customEyeLookSettings.eyesLookingLeft.right;
	 settings.EyeLookRightLeft = descriptor.customEyeLookSettings.eyesLookingRight.left;
	 settings.EyeLookRightRight = descriptor.customEyeLookSettings.eyesLookingRight.right;
	 EditorUtility.SetDirty(settings);
	 AssetDatabase.SaveAssets();
	}

	public static bool SetEyeLookCheck() {
	 GameObject newAvatar = Selection.activeGameObject;
	 if(null == newAvatar) {
		EditorUtility.DisplayDialog(SET_EYE_LOOK, $"Nothing Selected (probably unity donking up)", "Cancel");
		return false;
	 }
	 VRCAvatarDescriptor descriptor = newAvatar.GetComponent<VRCAvatarDescriptor>();
	 if(null == descriptor) {
		EditorUtility.DisplayDialog(SET_EYE_LOOK, $"[{newAvatar.name}] is has no Avatar Descriptor", "Cancel");
		return false;
	 }

	 if(!descriptor.enableEyeLook) {
		EditorUtility.DisplayDialog(SET_EYE_LOOK, $"[{newAvatar.name}] does not custom eye look", "Cancel");
		return false;
	 }
	 bool accept = accept = EditorUtility.DisplayDialog(SET_EYE_LOOK, $"Use [{newAvatar.name}]'s Eye Look settings as the default when using '{MAKE_AVATAR}'", "Accept", "Cancel");
	 return accept;
	}


	// ----- Make Avatar Menu -----
	private static readonly string MAKE_AVATAR = "Make Avatar 3.0";
	#region
	[MenuItem("GameObject/TEA Functions/Make Avatar 3.0", false, 1)]
	public static void MakeAvatar() {
	 if(!MakeAvatarCheck())
		return;

	 GameObject newAvatar = Selection.activeGameObject;
	 TEA_Settings settings = GetTEA_Settings();

	 // Folders
	 string scenePath = GetParentPath(newAvatar.gameObject.scene.path, false);
	 string parentFolder = GetPath(false, scenePath, newAvatar.gameObject.name);
	 string controller_folder = GetPath(false, parentFolder, settings.PlayableLayersFolder);
	 string animation_folder = GetPath(false, parentFolder, settings.AnimationsFolder);
	 string expression_folder = GetPath(false, parentFolder, settings.ExpressionsFolder);

	 if(!EditorUtility.DisplayDialog(MAKE_AVATAR,
		$"Avatar assets will be saved in root folder [{parentFolder}]"
		+ "\n"
		+ "\nThis operation may overridden files!"
		+ $"\nExpression assets at [{expression_folder}]"
		+ $"\nPlayable Layer assets at [{controller_folder}]"
		+ $"\nAnimation assets at [{animation_folder}]"
		, "Continue", "Cancel"))
		return;

	 VRCAvatarDescriptor vrcd = newAvatar.AddComponent<VRCAvatarDescriptor>();

	 // Folders
	 if(!AssetDatabase.IsValidFolder(parentFolder))
		AssetDatabase.CreateFolder(scenePath, newAvatar.gameObject.name);
	 if(!AssetDatabase.IsValidFolder(controller_folder))
		AssetDatabase.CreateFolder(parentFolder, settings.PlayableLayersFolder);
	 if(!AssetDatabase.IsValidFolder(animation_folder))
		AssetDatabase.CreateFolder(parentFolder, settings.AnimationsFolder);
	 if(!AssetDatabase.IsValidFolder(expression_folder))
		AssetDatabase.CreateFolder(parentFolder, settings.ExpressionsFolder);

	 // ViewPort
	 Transform leftEye = AvatarController.GetBone(vrcd, HumanBodyBones.LeftEye);
	 Transform rightEye = AvatarController.GetBone(vrcd, HumanBodyBones.RightEye);
	 Transform head = AvatarController.GetBone(vrcd, HumanBodyBones.Head);
	 if(null != leftEye && null != rightEye) {
		vrcd.ViewPosition = TEA_EditorUtility.InverseTransformPoint(newAvatar.transform, Vector3.Lerp(rightEye.position, leftEye.position, 0.5f));
		//Debug.Log($"{leftEye.position.x} - {rightEye.position.x}");
	 } else if(null != leftEye)
		vrcd.ViewPosition = TEA_EditorUtility.InverseTransformPoint(newAvatar.transform, leftEye.position);
	 else if(null != rightEye)
		vrcd.ViewPosition = TEA_EditorUtility.InverseTransformPoint(newAvatar.transform, rightEye.position);
	 else if(null != head) {
		vrcd.ViewPosition = TEA_EditorUtility.InverseTransformPoint(newAvatar.transform, head.position);
	 }

	 // Eye Look
	 if(leftEye && rightEye) {
		vrcd.enableEyeLook = true;
		vrcd.customEyeLookSettings.leftEye = leftEye;
		vrcd.customEyeLookSettings.rightEye = rightEye;
		vrcd.customEyeLookSettings.eyesLookingDown = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations();
		vrcd.customEyeLookSettings.eyesLookingDown.left = settings.EyeLookDownLeft;
		vrcd.customEyeLookSettings.eyesLookingDown.right = settings.EyeLookDownRight;
		vrcd.customEyeLookSettings.eyesLookingDown.linked = false;
		vrcd.customEyeLookSettings.eyesLookingRight = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations();
		vrcd.customEyeLookSettings.eyesLookingRight.left = settings.EyeLookRightLeft;
		vrcd.customEyeLookSettings.eyesLookingRight.right = settings.EyeLookRightRight;
		vrcd.customEyeLookSettings.eyesLookingRight.linked = false;
		vrcd.customEyeLookSettings.eyesLookingLeft = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations();
		vrcd.customEyeLookSettings.eyesLookingLeft.left = settings.EyeLookLeftLeft;
		vrcd.customEyeLookSettings.eyesLookingLeft.right = settings.EyeLookLeftRight;
		vrcd.customEyeLookSettings.eyesLookingLeft.linked = false;
		vrcd.customEyeLookSettings.eyesLookingUp = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations();
		vrcd.customEyeLookSettings.eyesLookingUp.left = settings.EyeLookUpLeft;
		vrcd.customEyeLookSettings.eyesLookingUp.right = settings.EyeLookUpRight;
		vrcd.customEyeLookSettings.eyesLookingUp.linked = false;
	 }

	 // Lip Sync
	 AutoDetectLipSync(vrcd);

	 //portraitCameraPositionOffset
	 if(settings.SetCameraPosition) {
		if(settings.PortraitCameraPositionOffset == Vector3.zero)
		 vrcd.portraitCameraPositionOffset = vrcd.ViewPosition + new Vector3(0, 0, 0.492f);
		else
		 vrcd.portraitCameraPositionOffset = settings.PortraitCameraPositionOffset;
	 }

	 if(settings.SetCameraRotation)
		vrcd.portraitCameraRotationOffset = Quaternion.Euler(settings.PortraitCameraRotationOffset);

	 // Locomotion
	 vrcd.autoLocomotion = false;

	 // Playable Layers
	 //Debug.Log($"[{vrcd.baseAnimationLayers[0]}]");
	 CopyPlayableLayer(vrcd, settings, controller_folder, animation_folder);
	 vrcd.customizeAnimationLayers = true;

	 // Custome Layers
	 if(null != settings.Sitting) {
		vrcd.specialAnimationLayers[0].isDefault = false;
		vrcd.specialAnimationLayers[0].isEnabled = true;
		vrcd.specialAnimationLayers[0].animatorController = settings.Sitting;
	 }
	 if(null != settings.TPose) {
		vrcd.specialAnimationLayers[1].isDefault = false;
		vrcd.specialAnimationLayers[1].isEnabled = true;
		vrcd.specialAnimationLayers[1].animatorController = settings.TPose;
	 }
	 if(null != settings.IKPose) {
		vrcd.specialAnimationLayers[2].isDefault = false;
		vrcd.specialAnimationLayers[2].isEnabled = true;
		vrcd.specialAnimationLayers[2].animatorController = settings.IKPose;
	 }

	 // Expressions
	 vrcd.customExpressions = true;

	 string em = GetPath(expression_folder, "ExpressionsMenu.asset");
	 string ep = GetPath(expression_folder, "ExpressionParameters.asset");
	 AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(settings.ExpressionsMenu), em);
	 vrcd.expressionsMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(em);
	 AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(settings.ExpressionParameters), ep);
	 vrcd.expressionParameters = AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>(ep);
	}

	//[MenuItem("GameObject/TEA Functions/Make Avatar 3.0", true, 1)]
	public static bool MakeAvatarCheck() {
	 GameObject newAvatar = Selection.activeGameObject;
	 if(null == newAvatar) {
		EditorUtility.DisplayDialog(MAKE_AVATAR, $"Nothing Selected (probably unity donking up)", "Cancel");
		return false;
	 }
	 VRCAvatarDescriptor descriptor = newAvatar.GetComponent<VRCAvatarDescriptor>();
	 if(null != descriptor) {
		EditorUtility.DisplayDialog(MAKE_AVATAR, $"{newAvatar.name} is already an avatar", "Cancel");
		return false;
	 }

	 Animator animator = newAvatar.GetComponent<Animator>();
	 if(null == animator) {
		EditorUtility.DisplayDialog(MAKE_AVATAR, $"{newAvatar.name} does not have an Animator", "Cancel");
		return false;
	 }
	 string parentFolder = GetParentPath(newAvatar.scene.path, false);
	 if(!AssetDatabase.IsValidFolder(parentFolder)) {
		EditorUtility.DisplayDialog(MAKE_AVATAR, $"The scene needs to be saved before you can use 'Make Avatar 3.0'", "OK");
		return false;
	 }
	 bool accept = true;
	 if(null == animator.avatar)
		accept = EditorUtility.DisplayDialog(MAKE_AVATAR, $"{newAvatar.name} does not have an Animator.Avatar", "Continue", "Cancel");

	 return accept;
	}

	private static void AutoDetectLipSync(VRCAvatarDescriptor avatarDescriptor) {
	 SkinnedMeshRenderer[] renderers = avatarDescriptor.GetComponentsInChildren<SkinnedMeshRenderer>();

	 string[] baseVisemeNames = Enum.GetNames(typeof(VRC_AvatarDescriptor.Viseme));
	 int visemeCount = baseVisemeNames.Length - 1;
	 string[] reversedVisemeNames = new string[visemeCount];
	 string[] reversedVVisemeNames = new string[visemeCount];
	 for(int i = 0; i < visemeCount; i++) {
		string visemeName = baseVisemeNames[i];
		char[] tmpArray = visemeName.ToLowerInvariant().ToCharArray();
		Array.Reverse(tmpArray);
		reversedVisemeNames[i] = new string(tmpArray);
		reversedVVisemeNames[i] = $"{reversedVisemeNames[i]}_v";
	 }

	 foreach(SkinnedMeshRenderer renderer in renderers) {
		if(renderer.sharedMesh.blendShapeCount <= 0)
		 continue;

		if(renderer.sharedMesh.blendShapeCount >= visemeCount) {
		 string[] rendererBlendShapeNames = new string[renderer.sharedMesh.blendShapeCount];
		 for(int i = 0; i < renderer.sharedMesh.blendShapeCount; i++) {
			rendererBlendShapeNames[i] = renderer.sharedMesh.GetBlendShapeName(i);
		 }

		 string[] visemeStrings = new string[visemeCount];
		 int foundVisemes = 0;

		 string[] reversedRendererNames = new string[rendererBlendShapeNames.Length];
		 Dictionary<string, string> reverseMap = new Dictionary<string, string>();

		 for(int i = 0; i < rendererBlendShapeNames.Length; i++) {
			string rendererBlendShapeName = rendererBlendShapeNames[i];
			char[] tmpArray = rendererBlendShapeName.ToLowerInvariant().ToCharArray();
			Array.Reverse(tmpArray);
			reversedRendererNames[i] = new string(tmpArray);
			if(reverseMap.ContainsKey(reversedRendererNames[i])) {
			 continue;
			}
			reverseMap.Add(reversedRendererNames[i], rendererBlendShapeName);
		 }

		 for(int i = 0; i < reversedVisemeNames.Length; i++) {
			string visemeName = reversedVisemeNames[i];
			string vVisemeName = reversedVVisemeNames[i];

			List<string> matchingStrings = new List<string>();
			foreach(string reversedRendererName in reversedRendererNames) {
			 if(reversedRendererName.Contains(vVisemeName)) {
				matchingStrings.Add(reversedRendererName);
			 }
			}
			if(matchingStrings.Count == 0) {
			 foreach(string reversedRendererName in reversedRendererNames) {
				if(reversedRendererName.Contains(visemeName)) {
				 matchingStrings.Add(reversedRendererName);
				}
			 }
			}

			matchingStrings.Sort(new SearchComparer(visemeName));

			if(matchingStrings.Count <= 0)
			 continue;

			visemeStrings[i] = reverseMap[matchingStrings[0]];
			foundVisemes++;
		 }

		 //Threshold to see if we did a good enough job to bother showing the user
		 if(foundVisemes > 2) {
			avatarDescriptor.lipSync = VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;
			avatarDescriptor.VisemeSkinnedMesh = renderer;
			avatarDescriptor.VisemeBlendShapes = visemeStrings;
			avatarDescriptor.lipSyncJawBone = null;
			return;
		 }
		}

		avatarDescriptor.lipSync = VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape;
		avatarDescriptor.VisemeSkinnedMesh = renderer;
		avatarDescriptor.lipSyncJawBone = null;
		return;
	 }


	 if(avatarDescriptor.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Jaw) == null)
		return;
	 avatarDescriptor.lipSync = VRC_AvatarDescriptor.LipSyncStyle.JawFlapBone;
	 avatarDescriptor.lipSyncJawBone = avatarDescriptor.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Jaw);
	 avatarDescriptor.VisemeSkinnedMesh = null;
	}

	class SearchComparer : IComparer<string> {
	 string ReplaceFirst(string text, string search, string replace) {
		int pos = text.IndexOf(search, StringComparison.Ordinal);
		if(pos < 0) {
		 return text;
		}
		return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
	 }

	 public SearchComparer(string searchString) {
		_searchString = searchString;
	 }
	 private readonly string _searchString;
	 public int Compare(string x, string y) {
		if(x == null || y == null) {
		 return 0;
		}
		//-1 is they're out of order, 0 is order doesn't matter, 1 is they're in order

		x = ReplaceFirst(x, "const ", "");
		y = ReplaceFirst(y, "const ", "");

		int xIndex = x.IndexOf(_searchString, StringComparison.InvariantCultureIgnoreCase);
		int yIndex = y.IndexOf(_searchString, StringComparison.InvariantCultureIgnoreCase);
		int compareIndex = xIndex.CompareTo(yIndex);
		if(compareIndex != 0)
		 return compareIndex;

		string xDiff = ReplaceFirst(x, _searchString, "");
		string yDiff = ReplaceFirst(y, _searchString, "");
		return string.Compare(xDiff, yDiff, StringComparison.InvariantCultureIgnoreCase);
	 }
	}
	#endregion

	// ----- ----- Utility Methods ----- -----
	[MenuItem("GameObject/TEA Functions/Create Toggle", false, 0)]
	public static void CreateToggle() {
	 if(!CreateToggleCheck())
		return;

	 GameObject selected = Selection.activeGameObject;
	 VRCAvatarDescriptor avatar = (VRCAvatarDescriptor)selected.GetComponentsInParent(typeof(VRCAvatarDescriptor), true)[0];

	 TEA_Settings settings = GetTEA_Settings();
	 string toggle_folder = SubFolder(avatar.transform, settings.ToggleFolder, false);

	 int isActive = EditorUtility.DisplayDialogComplex(
		$"Create Toggle for [{selected.name}]",
		$"AnimationClips will be created at [{toggle_folder}]",
		"OFF", "ON", "BOTH");

	 if(2 == isActive) {
		CreateToggle(selected, avatar.transform, 0);
		CreateToggle(selected, avatar.transform, 1);
	 } else
		CreateToggle(selected, avatar.transform, isActive);
	}

	private static void CreateToggle(GameObject gameObject, Transform parent, float value) {
	 TEA_Settings settings = GetTEA_Settings();
	 string toggle_folder = SubFolder(parent, settings.ToggleFolder, true);

	 AnimationClip clip = new AnimationClip() {
		name = gameObject.name
	 };
	 clip.SetCurve(AnimationUtility.CalculateTransformPath(gameObject.transform, parent), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0.0f, 0.0f, value));
	 string path = GetPath(toggle_folder, gameObject.name + (value == 1 ? "-ON.anim" : "-OFF.anim"));
	 if(!AssetDatabase.LoadAssetAtPath<AnimationClip>(path))
		AssetDatabase.CreateAsset(clip, path);
	 else
		EditorUtility.DisplayDialog($"Toggle Already Exists", $"[{path}]", "OK");
	}

	private static string SubFolder(Transform parent, string subFolder, bool create) {
	 string scenePath = GetParentPath(parent.gameObject.scene.path, false);
	 string parentFolder = GetPath(false, scenePath, parent.gameObject.name);
	 string subFolderPath = GetPath(false, parentFolder, subFolder);

	 if(!create)
		return subFolderPath;

	 if(!AssetDatabase.IsValidFolder(parentFolder))
		AssetDatabase.CreateFolder(scenePath, parent.gameObject.name);

	 if(!AssetDatabase.IsValidFolder(subFolderPath))
		AssetDatabase.CreateFolder(parentFolder, subFolder);
	 return subFolderPath;
	}

	//[MenuItem("GameObject/TEA Functions/Create Toggle", true, 0)]
	public static bool CreateToggleCheck() {
	 GameObject selected = Selection.activeGameObject;
	 Component[] avatars = selected.GetComponentsInParent(typeof(VRCAvatarDescriptor), true);
	 if(0 == avatars.Length)
		EditorUtility.DisplayDialog("Create Toggle", $"{selected.name} is not the child of an avatar", "Cancel");
	 else if(1 < avatars.Length)
		EditorUtility.DisplayDialog("Create Toggle", $"{selected.name} is child of multiple avatars", "Cancel");
	 return avatars.Length == 1;
	}
 }
}