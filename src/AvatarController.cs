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
   Debug.Log("Avatar Controller waking");
   current=this;
  }

  // --- --- --- Avatar Variables --- --- ---
  public VRCAvatarDescriptor Avatar;
  private Animator avatarAnim;
  public VRCExpressionsMenu mainMenu;
  public VRCExpressionParameters parameters;

  // --- --- --- Camera --- --- ---
  public CameraController cameraController;

  // --- --- --- Start --- --- ---
  void Start() {
   Avatar=TEA_Manager.current.Avatar;

   if(null==Avatar) {
    Debug.LogError("No avatar found");
    gameObject.SetActive(false);
    return;
   } else
    gameObject.SetActive(true);

   newPosition=Avatar.transform.position;

   //--- Camera ---
   cameraController=GetComponentInChildren<CameraController>();
   if(null==cameraController) {
    Debug.LogError("No Camera Controller found");
    gameObject.SetActive(false);
    return;
   } else
    gameObject.SetActive(true);

   //--- paramters ---
   mainMenu=Avatar.expressionsMenu;
   parameters=Avatar.expressionParameters;
   avatarAnim=Avatar.gameObject.GetComponent<Animator>();
   avatarAnim.runtimeAnimatorController=TEA_Manager.current.Controllers[TEA_Manager.current.Avatars.IndexOf(Avatar.name)];
   Grounded=true;
   avatarAnim.SetBool("Grounded", Grounded);

   //--- events ---
   controlEvents=new Dictionary<VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer, TEA_PlayableLayerControl>();
   if(null==TEA_PlayableLayerControl.ApplySettings)
    TEA_PlayableLayerControl.ApplySettings+=TEA_PlayableLayerEvent;

   if(!TEAManagerUpdateRegistered) {
    TEA_Manager.current.TEAManagerEvent+=OnTEAManagerUpdate;
    TEAManagerUpdateRegistered=true;
   }
  }

  private bool TEAManagerUpdateRegistered;
  public void OnTEAManagerUpdate(TEA_Manager tea_manager) {
   Start();
  }

  // --- --- --- Events --- --- ---
  [HideInInspector]
  public static TEA_ParameterSetDelegate TEA_ParameterSet;
  [HideInInspector]
  public delegate void TEA_ParameterSetDelegate(Parameter parameter);

  private Dictionary<VRCPlayableLayerControl.BlendableLayer, TEA_PlayableLayerControl> controlEvents = new Dictionary<VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer, TEA_PlayableLayerControl>();
  public void TEA_PlayableLayerEvent(TEA_PlayableLayerControl control, Animator animator) {
   control.duration=0;
   if(controlEvents.ContainsKey(control.layer))
    controlEvents.Remove(control.layer);
   controlEvents.Add(control.layer, control);
  }

  // --- --- --- Update --- --- ---
  public enum Speed {
   Walk,
   Sprint
  }

  [Header("Locomotion Settings")]
  public float MoveSpeed = 0.15f;
  public Speed speed = Speed.Walk;
  private float moveMultiplier = 1f;
  public float RunMultiplier = 3f;
  public float WalkSpeed = 1.56f;
  public float RunSpeed = 5.96f;
  private Vector3 newPosition;

  private void Update() {
   //--- controls ---
   List<VRCPlayableLayerControl.BlendableLayer> remove = new List<VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer>();
   foreach(KeyValuePair<VRCPlayableLayerControl.BlendableLayer, TEA_PlayableLayerControl> item in controlEvents) {
    TEA_PlayableLayerData layerData = TEA_Manager.current.LayerInfo[TEA_Manager.AvatarIndex()];
    TEA_PlayableLayerData.PlayableLayerData data = layerData.FindPlayableLayerData(TEA_PlayableLayerControl.AnimLayerType(item.Value.layer));
    float prevDur = item.Value.duration;
    item.Value.duration+=Time.deltaTime;
    float normalized = item.Value.duration/item.Value.blendDuration;
    for(int i = data.start; i<data.end; i++) {
     if(normalized>=1) {
      avatarAnim.SetLayerWeight(i, item.Value.goalWeight);
      remove.Add(item.Value.layer);
     } else {
      float newWeight = Mathf.Lerp(avatarAnim.GetLayerWeight(i), item.Value.goalWeight, (item.Value.duration-prevDur)/(item.Value.blendDuration-prevDur));
      avatarAnim.SetLayerWeight(i, newWeight);
     }
    }
   }

   foreach(VRCPlayableLayerControl.BlendableLayer rm in remove) {
    controlEvents.Remove(rm);
   }

   // --- input ---
   if(Input.GetKey(KeyCode.LeftShift)) {
    speed=Speed.Sprint;
    moveMultiplier=RunMultiplier;
   } else { speed=Speed.Walk; moveMultiplier=1f; }

   if(cameraController.mouseIn&&Input.GetMouseButton(1)&&!cameraController.FreeCamera) {
    newPosition=Vector3.zero;
    Avatar.transform.position=newPosition;
    VelocityX=0;
    VelocityZ=0;
   } else if(cameraController.mouseIn&&!cameraController.FreeCamera) {
    if(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)) {
     newPosition+=(cameraController.RigCamera.transform.forward*moveMultiplier*MoveSpeed);
    }
    if(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)) {
     newPosition+=(cameraController.RigCamera.transform.forward*moveMultiplier*-MoveSpeed);
    }
    if(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)) {
     newPosition+=(cameraController.RigCamera.transform.right*moveMultiplier*-MoveSpeed);
    }
    if(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)) {
     newPosition+=(cameraController.RigCamera.transform.right*moveMultiplier*MoveSpeed);
    }

    // animator velocities
    Vector3 localMove = Vector3.Normalize(Avatar.transform.InverseTransformPoint(newPosition));
    int zScale = (int)(Speed.Sprint==speed ? localMove.z*RunSpeed : localMove.z*WalkSpeed);
    VelocityZ=zScale;
    int xScale = (int)(Speed.Sprint==speed ? localMove.x*RunSpeed : localMove.x*WalkSpeed);
    VelocityX=xScale;

    // world position
    newPosition.y=0;
    Avatar.transform.position=newPosition;
   } else {
    newPosition=Avatar.transform.position;
    VelocityX=0;
    VelocityZ=0;
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
   Debug.Log($"setting param {param.name}:{param.type} [{param.fVal}] [{param.iVal}] [{param.boolean}]");
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
   Debug.Log($"setting [{control.name}]: {control.type}, sub params {values.Length}");

   if(!string.IsNullOrEmpty(control.parameter.name)) {
    SetExpressionParameter(control.parameter.name, control.value);
   }

   for(int i = 0; i<control.subParameters.Length; i++) {
    if(i>=values.Length) {
     //TEA_Manager.SDKError($"{control.name}\n the number of sub paramets does not match");
    } else if(!string.IsNullOrEmpty(control.subParameters[i].name)) {
     SetExpressionParameter(control.subParameters[i].name, values[i]);
    }
   }
  }

  internal void ExpressionParameterReset(VRCExpressionsMenu.Control control) {
   if(!string.IsNullOrEmpty(control.parameter.name))
    SetExpressionParameter(control.parameter.name, 0);

   for(int i = 0; i<control.subParameters.Length; i++) {
    SetExpressionParameter(control.subParameters[i].name, 0);
   }
  }

  public void SetExpressionParameter(string name, float value) {
   AnimatorControllerParameterType paramType = GetParameterType(name);
   Parameter param = new Parameter() {
    name=name,
    type=paramType,
    boolean=value>0,
    fVal=value,
    iVal=Mathf.RoundToInt(value)
   };
   SetAnimatorParameter(param);
  }

  //--- --- Parameters Methods --- ---
  internal AnimatorControllerParameterType GetParameterType(string name) {
   foreach(AnimatorControllerParameter param in avatarAnim.parameters) {
    if(name==param.name)
     return param.type;
   }
   throw new System.Exception($"The parameter [{name}] does not exist in the Animator");
  }

  internal float GetParameterValue(string name) {
   AnimatorControllerParameterType type = GetParameterType(name);
   if(AnimatorControllerParameterType.Float==type)
    return avatarAnim.GetFloat(name);
   if(AnimatorControllerParameterType.Int==type)
    return avatarAnim.GetInteger(name);
   throw new System.Exception($"The parameter [{name}] is not a float or int");
  }

  internal bool GetBool(string name) {
   return avatarAnim.GetBool(name);
  }

  internal bool HasAnimatorParameter(Animator animator, string name) {
   foreach(AnimatorControllerParameter parameter in animator.parameters) {
    if(parameter.name==name)
     return true;
   }
   return false;
  }

  // --- --- --- standing --- --- ---

  public void Falling(bool isFalling) {
   Grounded=!isFalling;
  }

  public static readonly string TEA_LAYER = "TEA Animations";
  public static readonly string TEA_HAND_LAYER = "TEA Hand Animations";
  public static readonly string TEA_ANIM_PARAM = "TEA_Anim";
  private bool _tea_isActive = true;
  public bool TEA_isActive {
   get { return _tea_isActive; }
   set {
    if(value) {
     int paramValue = avatarAnim.GetInteger(TEA_ANIM_PARAM);
     if(paramValue==0) {
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
    _tea_isActive=value;
   }
  }
  public int TEA_ANIM {
   get => avatarAnim.GetInteger(TEA_ANIM_PARAM); set {
    avatarAnim.SetInteger(TEA_ANIM_PARAM, value);
    TEA_isActive=TEA_isActive;
   }
  }

  public void ReverseToggleTEA(bool value) {
   TEA_isActive=!value;
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
   Debug.Log("ResetGesture");
   GestureLeft=0;
   GestureRight=0;
  }

  public void RightGesture(int gesture) {
   Debug.Log("RightGesture: "+gesture);
   GestureRight=gesture;
  }

  public void LeftGesture(int gesture) {
   Debug.Log("LeftGesture: "+gesture);
   GestureLeft=gesture;
  }

  public static VRCAvatarDescriptor GetFirstAvatar(Scene scene) {
   if(!scene.isLoaded)
    return null;

   GameObject[] rootObjects = scene.GetRootGameObjects();
   foreach(GameObject root in rootObjects) {
    VRCAvatarDescriptor avatar = root.GetComponent<VRCAvatarDescriptor>();
    if(null!=avatar) {
     return avatar;
    }
   }
   return null;
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
    if(null!=avatar) {
     avatars.Add(avatar.gameObject.name, avatar);
    }
   }

   return avatars;
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