using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using TEA.ScriptableObject;

namespace TEA {
 public class AvatarController : MonoBehaviour {
	// --- --- --- Awake --- --- ---
	public static AvatarController current;
	private void Awake() {
	 //Debug.Log("Avatar Controller waking");
	 current = this;
	}

	// --- --- --- Variables --- --- ---
	bool _initialized = false;
	private VRCAvatarDescriptor Avatar;
	private Animator avatarAnim;
	public VRCExpressionsMenu mainMenu;
	public VRCExpressionParameters parameters;

	// --- Camera
	public CameraController CameraController;

	// --- --- --- Start --- --- ---
	void Start() {
	 _initialized = Initialized();
	 if(!TEAManagerUpdateRegistered) {
		TEA_Manager.current.TEAManagerEvent += OnTEAManagerUpdate;
		TEAManagerUpdateRegistered = true;
	 }
	}

	private bool TEAManagerUpdateRegistered;
	public void OnTEAManagerUpdate(TEA_Manager tea_manager) {
	 _initialized = false;
	 Start();
	}

	private bool Initialized() {
	 if(_initialized)
		return true;

	 Avatar = TEA_Manager.current.Avatar;

	 if(null == Avatar)
		return false;

	 newPosition = Avatar.transform.position;
	 horizontalRotation = Avatar.transform.rotation;

	 //--- paramters ---
	 mainMenu = Avatar.expressionsMenu;
	 parameters = Avatar.expressionParameters;
	 avatarAnim = Avatar.gameObject.GetComponent<Animator>();
	 avatarAnim.runtimeAnimatorController = TEA_Manager.current.Controllers[TEA_Manager.AvatarIndex()];
	 Grounded = true;
	 avatarAnim.SetBool("Grounded", Grounded);

	 //--- events ---
	 controlEvents = new Dictionary<VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer, TEA_PlayableLayerControl>();
	 if(null == TEA_PlayableLayerControl.ApplySettings)
		TEA_PlayableLayerControl.ApplySettings += TEA_PlayableLayerEvent;

	 _initialized = true;
	 return true;
	}

	// --- --- --- Events --- --- ---
	[HideInInspector]
	public static TEA_ParameterSetDelegate TEA_ParameterSet;
	[HideInInspector]
	public delegate void TEA_ParameterSetDelegate(Parameter parameter);

	private Dictionary<VRCPlayableLayerControl.BlendableLayer, TEA_PlayableLayerControl> controlEvents = new Dictionary<VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer, TEA_PlayableLayerControl>();
	public void TEA_PlayableLayerEvent(TEA_PlayableLayerControl control, Animator animator) {
	 control.duration = 0;
	 if(controlEvents.ContainsKey(control.layer))
		controlEvents.Remove(control.layer);
	 controlEvents.Add(control.layer, control);
	}

	// --- --- --- Update --- --- ---
	public TEA_Settings Locomotion { get; set; }
	private Vector3 newPosition;
	private Quaternion horizontalRotation;
	private float velocity = 1;
	private int speed = 0;

	private void Update() {
	 if(!Initialized())
		return;

	 //--- controls ---
	 List<VRCPlayableLayerControl.BlendableLayer> remove = new List<VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer>();
	 foreach(KeyValuePair<VRCPlayableLayerControl.BlendableLayer, TEA_PlayableLayerControl> item in controlEvents) {
		TEA_PlayableLayerData layerData = TEA_Manager.current.LayerInfo[TEA_Manager.AvatarIndex()];
		TEA_PlayableLayerData.PlayableLayerData data = layerData.FindPlayableLayerData(TEA_PlayableLayerControl.AnimLayerType(item.Value.layer));
		float prevDur = item.Value.duration;
		item.Value.duration += Time.deltaTime;
		float normalized = item.Value.duration / item.Value.blendDuration;
		for(int i = data.start; i < data.end; i++) {
		 if(normalized >= 1) {
			avatarAnim.SetLayerWeight(i, item.Value.goalWeight);
			remove.Add(item.Value.layer);
		 } else {
			float newWeight = Mathf.Lerp(avatarAnim.GetLayerWeight(i), item.Value.goalWeight, (item.Value.duration - prevDur) / (item.Value.blendDuration - prevDur));
			avatarAnim.SetLayerWeight(i, newWeight);
		 }
		}
	 }

	 foreach(VRCPlayableLayerControl.BlendableLayer rm in remove) {
		controlEvents.Remove(rm);
	 }

	 if(CameraController.mouseIn && Input.GetMouseButton(1) && !CameraController.FreeCamera) {
		newPosition = Vector3.zero;
		Avatar.transform.position = newPosition;
		VelocityX = 0;
		VelocityZ = 0;
	 } else if(null != Locomotion && CameraController.mouseIn && !CameraController.FreeCamera) {
		//Rotate
		if(Input.GetKey(KeyCode.E)) {
		 horizontalRotation *= Quaternion.Euler(Vector3.up * Locomotion.RotationAmount);
		}
		if(Input.GetKey(KeyCode.Q)) {
		 horizontalRotation *= Quaternion.Euler(Vector3.up * -Locomotion.RotationAmount);
		}
		Avatar.transform.rotation = horizontalRotation;

		// --- input ---
		if(Input.GetKeyUp(KeyCode.LeftShift))
		 speed++;
		if(3 <= speed)
		 speed = 0;

		if(0 == speed) {
		 Locomotion.MoveType = TEA_Settings.MoveTypes.Walk;
		 velocity = Locomotion.WalkVelocity;
		} else if(1 == speed) {
		 Locomotion.MoveType = TEA_Settings.MoveTypes.Run;
		 velocity = Locomotion.RunVelocity;
		} else if(2 == speed) {
		 Locomotion.MoveType = TEA_Settings.MoveTypes.Sprint;
		 velocity = Locomotion.SprintVelocity;
		}

		// Walk
		int forward = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
		int backward = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
		int right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
		int left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
		int z = forward + backward;
		int x = right + left;

		if(x != 0 || z != 0) {
		 float distance = Time.deltaTime * velocity;
		 float theta = Mathf.Atan2(z, x);
		 float distanceX = Mathf.Cos(theta) * (distance);
		 float distanceZ = Mathf.Sin(theta) * (distance);
		 Vector3 cameraDirection = CameraController.RigCamera.transform.forward;

		 // animator velocities
		 if(TEA_Manager.current.ViewPort.gameObject.activeSelf) {
			newPosition = TEA_Utility.TransformPoint(Avatar.transform, new Vector3(distanceX, 0, distanceZ));
			VelocityZ = distanceZ / Time.deltaTime;
		 } else {
			newPosition += TEA_Manager.current.CameraRigPointer.transform.right * distanceX;
			newPosition += TEA_Manager.current.CameraRigPointer.transform.forward * distanceZ;
			Vector3 movePoint = TEA_Utility.InverseTransformPoint(Avatar.transform, newPosition);
			float aTheta = Mathf.Deg2Rad * Avatar.transform.rotation.eulerAngles.y;
			Vector3 movePointR = new Vector3();
			movePointR.x = movePoint.x * Mathf.Cos(aTheta) - movePoint.z * Mathf.Sin(aTheta);
			movePointR.z = movePoint.z * Mathf.Cos(aTheta) + movePoint.x * Mathf.Sin(aTheta);
			VelocityX = 0 + movePointR.x / Time.deltaTime;
			VelocityZ = 0 + movePointR.z / Time.deltaTime;
		 }

		 //Debug.Log($"[{x},{z}] distance[{distance}] float[{distanceX}, {distanceZ}] velocity[{VelocityX}, {VelocityZ}]");

		 // world position
		 Avatar.transform.position = newPosition;
		} else {
		 newPosition = Avatar.transform.position;
		 VelocityX = 0;
		 VelocityZ = 0;
		}
	 } else {
		newPosition = Avatar.transform.position;
		VelocityX = 0;
		VelocityZ = 0;
	 }
	}

	// --- --- --- Avatar Controls --- --- ---
	public struct Parameter {
	 public string name;
	 public AnimatorControllerParameterType type;
	 public bool boolean;
	 public int iVal;
	 public float fVal;
	}

	public void SetAnimatorParameter(Parameter param) {
	 //Debug.Log($"setting param {param.name}:{param.type} [{param.fVal}] [{param.iVal}] [{param.boolean}]");
	 switch(param.type) {
		case AnimatorControllerParameterType.Bool: {
		 avatarAnim.SetBool(param.name, param.boolean);
		 break;
		}
		case AnimatorControllerParameterType.Float: {
		 avatarAnim.SetFloat(param.name, param.fVal);
		 break;
		}
		case AnimatorControllerParameterType.Int: {
		 avatarAnim.SetInteger(param.name, param.iVal);
		 break;
		}
		case AnimatorControllerParameterType.Trigger: {
		 if(param.boolean)
			avatarAnim.SetTrigger(param.name);
		 else
			avatarAnim.ResetTrigger(param.name);
		 break;
		}
		default:
		 break;
	 }
	 TEA_ParameterSet(param);
	}

	internal void ExpressionParameterSet(VRCExpressionsMenu.Control control, params float[] values) {
	 //Debug.Log($"setting [{control.name}]: {control.type}, sub params {values.Length}");

	 if(!string.IsNullOrEmpty(control.parameter.name)) {
		SetExpressionParameter(control.parameter.name, control.value);
	 }

	 for(int i = 0; i < control.subParameters.Length; i++) {
		if(i >= values.Length) {
		 //TEA_Manager.SDKError($"{control.name}\n the number of sub paramets does not match");
		} else if(!string.IsNullOrEmpty(control.subParameters[i].name)) {
		 SetExpressionParameter(control.subParameters[i].name, values[i]);
		}
	 }
	}

	internal void ExpressionParameterReset(VRCExpressionsMenu.Control control) {
	 if(!string.IsNullOrEmpty(control.parameter.name))
		SetExpressionParameter(control.parameter.name, 0);
	}

	public void SetExpressionParameter(string name, float value) {
	 AnimatorControllerParameterType paramType = GetParameterType(name);
	 Parameter param = new Parameter() {
		name = name,
		type = paramType,
		boolean = value > 0,
		fVal = value,
		iVal = Mathf.RoundToInt(value)
	 };
	 SetAnimatorParameter(param);
	}

	//--- --- Parameters Methods --- ---
	internal AnimatorControllerParameterType GetParameterType(string name) {
	 foreach(AnimatorControllerParameter param in avatarAnim.parameters) {
		if(name == param.name)
		 return param.type;
	 }
	 throw new System.Exception($"The parameter [{name}] does not exist in the Animator");
	}

	internal float GetParameterValue(string name) {
	 AnimatorControllerParameterType type = GetParameterType(name);
	 if(AnimatorControllerParameterType.Float == type)
		return avatarAnim.GetFloat(name);
	 if(AnimatorControllerParameterType.Int == type)
		return avatarAnim.GetInteger(name);
	 throw new System.Exception($"The parameter [{name}] is not a float or int");
	}

	internal bool GetBool(string name) {
	 return avatarAnim.GetBool(name);
	}

	// --- --- --- standing --- --- ---

	public void Falling(bool isFalling) {
	 Grounded = !isFalling;
	}

	public static readonly string TEA_LAYER = "TEA Animations";
	public static readonly string TEA_HAND_LAYER = "TEA Hand Animations";
	public static readonly string TEA_ANIM_PARAM = "TEA_Anim";
	public int TEA_HAND_LAYER_COUNT = 0;
	private bool _tea_isActive = true;
	public bool TEA_isActive {
	 get { return _tea_isActive; }
	 set {
		if(value) {
		 int paramValue = avatarAnim.GetInteger(TEA_ANIM_PARAM);
		 if(paramValue < TEA_HAND_LAYER_COUNT) {
			avatarAnim.SetLayerWeight(avatarAnim.GetLayerIndex(TEA_LAYER), 0);
			avatarAnim.SetLayerWeight(avatarAnim.GetLayerIndex(TEA_HAND_LAYER), 1);
		 } else {
			avatarAnim.SetLayerWeight(avatarAnim.GetLayerIndex(TEA_LAYER), 1);
			avatarAnim.SetLayerWeight(avatarAnim.GetLayerIndex(TEA_HAND_LAYER), 0);
		 }
		} else {
		 avatarAnim.SetLayerWeight(avatarAnim.GetLayerIndex(TEA_HAND_LAYER), 0);
		 avatarAnim.SetLayerWeight(avatarAnim.GetLayerIndex(TEA_LAYER), 0);
		}
		_tea_isActive = value;
	 }
	}
	public int TEA_ANIM {
	 get => avatarAnim.GetInteger(TEA_ANIM_PARAM); set {
		avatarAnim.SetInteger(TEA_ANIM_PARAM, value);
		TEA_isActive = TEA_isActive;
	 }
	}

	public void ReverseToggleTEA(bool value) {
	 TEA_isActive = !value;
	}

	// --- --- --- Gestures --- --- ---
	internal static readonly string GESTURE_RIGHT = "GestureRight";
	internal static readonly string GESTURE_LEFT = "GestureLeft";

	public int GestureRight { get => avatarAnim.GetInteger(GESTURE_RIGHT); set { avatarAnim.SetInteger(GESTURE_RIGHT, value); } }
	public int GestureLeft { get => avatarAnim.GetInteger(GESTURE_LEFT); set { avatarAnim.SetInteger(GESTURE_LEFT, value); } }
	public bool AFK {
	 get => avatarAnim.GetBool("AFK");
	 set {
		if(value) {
		 //PlayAction();
		 avatarAnim.SetBool("AFK", value);
		} else {
		 avatarAnim.SetBool("AFK", value);
		}
	 }
	}
	public float VelocityX { get => avatarAnim.GetFloat("VelocityX"); set { avatarAnim.SetFloat("VelocityX", value); } }
	public float VelocityY { get => avatarAnim.GetFloat("VelocityY"); set { avatarAnim.SetFloat("VelocityY", value); } }
	public float VelocityZ { get => avatarAnim.GetFloat("VelocityZ"); set { avatarAnim.SetFloat("VelocityZ", value); } }
	public float AngularY { get => avatarAnim.GetFloat("AngularY"); set { avatarAnim.SetFloat("AngularY", value); } }
	public bool Grounded { get => avatarAnim.GetBool("Grounded"); set { avatarAnim.SetBool("Grounded", value); } }
	public bool Supine { get => avatarAnim.GetBool("Supine"); set { avatarAnim.SetBool("Supine", value); } }
	public float Upright { get => avatarAnim.GetFloat("Upright"); set { avatarAnim.SetFloat("Upright", value); } }

	public void ResetGesture() {
	 //Debug.Log("ResetGesture");
	 GestureLeft = 0;
	 GestureRight = 0;
	}

	public void RightGesture(int gesture) {
	 //Debug.Log("RightGesture: "+gesture);
	 GestureRight = gesture;
	}

	public void LeftGesture(int gesture) {
	 //Debug.Log("LeftGesture: "+gesture);
	 GestureLeft = gesture;
	}

	public static VRCAvatarDescriptor GetFirstAvatar(Scene scene) {
	 if(!scene.isLoaded)
		return null;

	 GameObject[] rootObjects = scene.GetRootGameObjects();
	 foreach(GameObject root in rootObjects) {
		VRCAvatarDescriptor avatar = root.GetComponent<VRCAvatarDescriptor>();
		if(null != avatar) {
		 return avatar;
		}
	 }
	 return null;
	}

	public static Transform GetRootBone(VRCAvatarDescriptor avatar) {
	 return avatar.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips);
	}

	public static Transform GetBone(VRCAvatarDescriptor avatar, HumanBodyBones bone) {
	 return avatar.GetComponent<Animator>().GetBoneTransform(bone);
	}

	internal static Transform GetRootBone() {
	 return TEA_Manager.current.Avatar.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips);
	}
 }
}