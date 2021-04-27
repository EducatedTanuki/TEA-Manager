using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.Components;
using UnityEngine.SceneManagement;
using TEA.ScriptableObject;

namespace TEA {
 public class TEA_Manager : MonoBehaviour {
	public static TEA_Manager current;

	private double id = 0;
	private void Awake() {
	 current = this;
	 System.Random random = new System.Random();
	 id = random.NextDouble();
	 Debug.Log($"TEA_Manager awake [{id}]");
	}

	public event System.Action<TEA_Manager> TEAManagerEvent;

	// --- Avatar selection
	public static int INDEX = 0;
	public static List<VRCAvatarDescriptor> AvatarDescriptor = new List<VRCAvatarDescriptor>();
	public VRCAvatarDescriptor Avatar { get => INDEX >= AvatarDescriptor.Count ? null : AvatarDescriptor[INDEX]; set { } }

	public TEA_Settings Settings { get; set; }

	public static int AvatarIndex() {
	 return INDEX;
	}

	public static string GetSceneAvatarKey(Scene scene, VRCAvatarDescriptor avatar) {
	 return $"{avatar.name} |{scene.name}";
	}

	public Vector3 GetAvatarViewPortWorld() {
	 Transform head = AvatarController.GetBone(current.Avatar, HumanBodyBones.Head);
	 Vector3 world = TEA_Utility.TransformPoint(head, ViewPorts[AvatarIndex()]);
	 //Debug.Log($"viewport at [{world}]");
	 return world;
	}

	[Header("Avatar Masks")]
	public AvatarMask AvatarMaskNone;
	public AvatarMask AvatarMaskAll;
	public AvatarMask AvatarMaskArms;

	[Header("Compiled Avatar Components")]
	public List<RuntimeAnimatorController> Controllers = new List<RuntimeAnimatorController>();
	public List<TEA_PlayableLayerData> LayerInfo = new List<TEA_PlayableLayerData>();
	public List<Vector3> ViewPorts = new List<Vector3>();
	public List<string> Avatars = new List<string>();


	[Header("Default Playable Layers")]
	[SerializeField]
	public RuntimeAnimatorController Base;
	[SerializeField]
	public RuntimeAnimatorController Additive;
	[SerializeField]
	public RuntimeAnimatorController Gesture_Male;
	[SerializeField]
	public RuntimeAnimatorController Gesture_Female;
	[SerializeField]
	public RuntimeAnimatorController Action;
	[SerializeField]
	public RuntimeAnimatorController TEA_Animations;

	[Header("Camera Components")]
	public ParentConstraint CameraRig;
	public ParentConstraint ViewPort;
	public ParentConstraint FaceCamera;
	public GameObject CameraRigPointer;

	[Header("SDK Warning Components")]
	public HorizontalOrVerticalLayoutGroup SDKErrorUI;
	public GameObject SDKErrorPrefab;
	private static List<GameObject> sDKIssues = new List<GameObject>();

	[Header("Canvas Objects")]
	public GameObject Canvas;
	public GameObject Stage;
	public GameObject WorldCenter;
	public GameObject AudioListener;
	public GameObject Light;
	public Dropdown TEA_AnimationClips;

	public static void SDKError(string message) {
	 Debug.LogWarning($"VRC SDK Issue [{message}]");
	 GameObject error = Instantiate(current.SDKErrorPrefab) as GameObject;
	 error.transform.SetParent(current.SDKErrorUI.transform);
	 error.transform.Find("Text").GetComponent<Text>().text = message;
	 error.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
	 sDKIssues.Add(error);
	}

	public void Initialize(int avatarIndex) {
	 if(avatarIndex >= AvatarDescriptor.Count)
		Debug.Log($"Index [{avatarIndex}] does not exist");
	 else
		INDEX = avatarIndex;

	 foreach(GameObject obj in sDKIssues) {
		Destroy(obj);
	 }
	 sDKIssues = new List<GameObject>();

	 SetupComponents(INDEX);

	 if(null != TEAManagerEvent) {
		//Debug.Log("Calling OnTEAManagerUpdate");
		TEAManagerEvent(this);
	 }
	}

	public void SetupComponents(int avatarIndex) {
	 Avatar = AvatarDescriptor[avatarIndex];
	 if(Avatar == null) {
		Debug.Log($"Avatar at [{avatarIndex}] does not exist");
		return;
	 }

	 //Debug.Log($"setting avatar to {avatar.name}");

	 Transform rootBone = AvatarController.GetRootBone(Avatar);
	 Vector3 viewportToHead = TEA_Utility.InverseTransformPoint(AvatarController.GetBone(Avatar, HumanBodyBones.Head), TEA_Utility.TransformPoint(Avatar.transform, Avatar.ViewPosition));

	 if(null != ViewPorts && ViewPorts.Count > 0)
		viewportToHead = ViewPorts[AvatarIndex()];

	 Vector3 rootToRoot = Vector3.zero;


	 // -- Parent Contraints --
	 SetupCamera(FaceCamera, AvatarController.GetBone(Avatar, HumanBodyBones.Head), Vector3.zero, Vector3.zero, viewportToHead + Vector3.forward, new Vector3(0, 180, 0));

	 // -- Camera Placement --
	 SetupCamera(CameraRig, rootBone, Vector3.zero, rootToRoot);
	 SetupCamera(ViewPort, AvatarController.GetBone(Avatar, HumanBodyBones.Head), Vector3.zero, viewportToHead);
	}

	private void Start() {
	}

	private static void SetupCamera(ParentConstraint camera, Transform source, Vector3 translationAtRest, Vector3 rotationAtRest, Vector3 translationOffset, Vector3 rotationOffset) {
	 if(null != camera) {
		if(0 < camera.sourceCount)
		 camera.RemoveSource(0);
		camera.AddSource(
			 new ConstraintSource() {
				sourceTransform = source,
				weight = 1f
			 }
		 );
		camera.translationAtRest = translationAtRest;
		camera.rotationAtRest = rotationAtRest;
		camera.SetTranslationOffset(0, translationOffset);
		camera.SetRotationOffset(0, rotationOffset);
		camera.enabled = true;
		camera.constraintActive = true;
	 }
	}

	private static void SetupCamera(ParentConstraint camera, Transform source, Vector3 translationAtRest, Vector3 translationOffset) {
	 SetupCamera(camera, source, translationAtRest, Vector3.zero, translationOffset, Vector3.zero);
	}
 }
}