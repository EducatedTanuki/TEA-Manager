using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.Components;
using TEA.ScriptableObject;
using System;

namespace TEA {
 public class TEA_Manager : MonoBehaviour {
  public static TEA_Manager current;

  private void Awake() {
   current=this;
   if(null==Avatar) {
    Initialize(AvatarController.GetFirstAvatar(gameObject.scene));
   }
  }

  public event System.Action<TEA_Manager> TEAManagerEvent;

  public VRCAvatarDescriptor Avatar;
  public static int AvatarIndex() {
   return current.Avatars.IndexOf(current.Avatar.name);
  }

  [Header("Avatar Masks")]
  public AvatarMask AvatarMaskNone;
  public AvatarMask AvatarMaskAll;
  public AvatarMask AvatarMaskArms;

  [Header("Compiled Avatar Components")]
  public List<RuntimeAnimatorController> Controllers = new List<RuntimeAnimatorController>();
  public List<TEA_PlayableLayerData> LayerInfo = new List<TEA_PlayableLayerData>();
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

  [Header("SDK Warning Components")]
  public HorizontalOrVerticalLayoutGroup SDKErrorUI;
  public GameObject SDKErrorPrefab;
  private static List<GameObject> sDKIssues = new List<GameObject>();

  [Header("Canvas Objects")]
  public GameObject Stage;
  public GameObject WorldCenter;
  public GameObject AudioListener;
  public GameObject Light;
  public Dropdown TEA_AnimationClips;

  public static void SDKError(string message) {
   Debug.LogWarning($"VRC SDK Issue [{message}]");
   GameObject error = Instantiate(current.SDKErrorPrefab) as GameObject;
   error.transform.SetParent(current.SDKErrorUI.transform);
   error.transform.Find("Text").GetComponent<Text>().text=message;
   error.GetComponent<RectTransform>().localScale=new Vector3(1, 1, 1);
   sDKIssues.Add(error);
  }

  public void Initialize(VRCAvatarDescriptor avatar) {
   foreach(GameObject obj in sDKIssues) {
    Destroy(obj);
   }
   sDKIssues=new List<GameObject>();

   SetupComponents(avatar);

   if(null!=TEAManagerEvent) {
    Debug.Log("Calling OnTEAManagerUpdate");
    TEAManagerEvent(this);
   }
  }

  public void SetupComponents(VRCAvatarDescriptor avatar) {
   if(avatar==null) {
    Debug.LogError("No avatar found");
    return;
   }

   Debug.Log($"setting avatar to {avatar.name}");

   // -- Avatar Selection --
   Avatar=avatar;

   Transform rootBone = AvatarController.GetRootBone(avatar);
   Vector3 viewportToHead = avatar.ViewPosition-AvatarController.GetBone(avatar, HumanBodyBones.Head).position;
   Vector3 rootToRoot = Vector3.zero;

   // -- Parent Contraints --
   SetupCamera(FaceCamera, AvatarController.GetBone(avatar, HumanBodyBones.Head), Vector3.zero, Vector3.zero, viewportToHead+Vector3.forward, new Vector3(0, 180, 0));

   // -- Camera Placement --
   SetupCamera(CameraRig, rootBone, Vector3.zero, rootToRoot);
   SetupCamera(ViewPort, AvatarController.GetBone(avatar, HumanBodyBones.Head), Vector3.zero, viewportToHead);
  }

  private void Start() {
   if(null==Avatar) {
    Debug.Log("No avatar found");
    gameObject.SetActive(false);
   }
  }

  private static void SetupCamera(ParentConstraint camera, Transform source, Vector3 translationAtRest, Vector3 rotationAtRest, Vector3 translationOffset, Vector3 rotationOffset) {
   if(null!=camera) {
    if(0<camera.sourceCount)
     camera.RemoveSource(0);
    camera.AddSource(
       new ConstraintSource() {
        sourceTransform=source,
        weight=1f
       }
     );
    camera.translationAtRest=translationAtRest;
    camera.rotationAtRest=rotationAtRest;
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