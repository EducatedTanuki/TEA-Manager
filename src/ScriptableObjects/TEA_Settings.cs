using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace TEA {
 [System.Serializable]
 [CreateAssetMenu(fileName = "Default Settings", menuName = "ScriptableObjects/TEA_Settings", order = 1)]
 public class TEA_Settings : UnityEngine.ScriptableObject {
	[Header("Make Avatar 3.0")]
	[Tooltip("Setting empty will place assets in the Avatars root folder")]
	[SerializeField] public string ExpressionsFolder = "Expressions";
	[SerializeField] public VRCExpressionsMenu ExpressionsMenu;
	[SerializeField] public VRCExpressionParameters ExpressionParameters;
	[Space]
	[Tooltip("Setting empty will place assets in the Avatars root folder")]
	[SerializeField] public string PlayableLayersFolder = "Playable Layers";
	[Tooltip("Setting empty will place assets in the Avatars root folder")]
	[SerializeField] public string AnimationsFolder = "Animations";
	[Tooltip("Copy Layer to the Playable Layers Folder (turn off if you want all avatars to point to the same asset)")]
	[SerializeField] public bool BaseCopy = true;
	[Tooltip("Copy Animations to Animations Folder (turn off if you want all avatars to point to the same animations)")]
	[SerializeField] public bool BaseCopyAnimations = false;
	[SerializeField] public RuntimeAnimatorController Base;
	[Tooltip("Copy Layer to the Playable Layers Folder (turn off if you want all avatars to point to the same asset)")]
	[SerializeField] public bool AdditiveCopy = true;
	[Tooltip("Copy Animations to Animations Folder (turn off if you want all avatars to point to the same animations)")]
	[SerializeField] public bool AdditiveCopyAnimations = false;
	[SerializeField] public RuntimeAnimatorController Additive;
	[Tooltip("Copy Layer to the Playable Layers Folder (turn off if you want all avatars to point to the same asset)")]
	[SerializeField] public bool GestureCopy = true;
	[Tooltip("Copy Animations to Animations Folder (turn off if you want all avatars to point to the same animations)")]
	[SerializeField] public bool GestureCopyAnimations = false;
	[SerializeField] public RuntimeAnimatorController Gesture;
	[Tooltip("Copy Layer to the Playable Layers Folder (turn off if you want all avatars to point to the same asset)")]
	[SerializeField] public bool ActionCopy = true;
	[Tooltip("Copy Animations to Animations Folder (turn off if you want all avatars to point to the same animations)")]
	[SerializeField] public bool ActionCopyAnimations = false;
	[SerializeField] public RuntimeAnimatorController Action;
	[Tooltip("Copy Layer to the Playable Layers Folder (not recommended to turn off)")]
	[SerializeField] public bool FXCopy = true;
	[Tooltip("Copy Animations to Animations Folder (not recommended to turn off)")]
	[SerializeField] public bool FXCopyAnimations = true;
	[SerializeField] public RuntimeAnimatorController FX;
	[Space]
	[SerializeField] public RuntimeAnimatorController Sitting;
	[SerializeField] public RuntimeAnimatorController TPose;
	[SerializeField] public RuntimeAnimatorController IKPose;
	[Space]
	[Tooltip("Set the VRC Portrait Camara Position with the values below")]
	[SerializeField] public bool SetCameraPosition = true;
	[Tooltip("When zero the avatars calculated viewport will be used")]
	[SerializeField] public Vector3 PortraitCameraPositionOffset = Vector3.zero;
	[Tooltip("Set the VRC Portrait Camara Rotation with the values below")]
	[SerializeField] public bool SetCameraRotation = true;
	[SerializeField] public Vector3 PortraitCameraRotationOffset = new Vector3(0, 180, 0);
	[Tooltip("Use 'TEA Functions/Set Eye Look as default' to change this")]
	[SerializeField] public Quaternion EyeLookDownLeft = new Quaternion(0.3f, 0f, 0f, 1f);
	[SerializeField] public Quaternion EyeLookDownRight = new Quaternion(0.3f, 0f, 0f, 1f);
	[SerializeField] public Quaternion EyeLookUpLeft = new Quaternion(-0.2f, 0f, 0f, 1f);
	[SerializeField] public Quaternion EyeLookUpRight = new Quaternion(-0.2f, 0f, 0f, 1f);
	[SerializeField] public Quaternion EyeLookRightLeft = new Quaternion(0f, 0.2f, 0f, 1f);
	[SerializeField] public Quaternion EyeLookRightRight = new Quaternion(0f, 0.2f, 0f, 1f);
	[SerializeField] public Quaternion EyeLookLeftLeft = new Quaternion(0f, -0.2f, 0f, 1f);
	[SerializeField] public Quaternion EyeLookLeftRight = new Quaternion(0f, -0.2f, 0f, 1f);

	[Header("Create Toggle")]
	[Tooltip("Setting empty will place assets in the Avatars root folder")]
	[SerializeField] public string ToggleFolder = "Toggles";

	[Header("Camera Controls")]
	[Tooltip("Distance the camera will move when zooming")]
	public Vector3 zoomAmount = Vector3.forward * .2f;
	[Tooltip("Multiplier for fast zooming")]
	public float zoomQuickly = 3.5f;
	[Tooltip("Multiplier for slow zooming")]
	public float zoomSlowly = 0.35f;
	[Tooltip("Multiplies base distance the camera will move (see slow and fast zoom)")]
	public float zoomMultiplier = 1;
	[Tooltip("Closest distance the camera will be from the armature")]
	public float ClosestCameraZoom = .2f;
	[Tooltip("(Orthographic) Distance the camera will move when zooming")]	
	public float zoomAmountOrtho = .1f;
	[Tooltip("Rotation amount in FreeCamera mode")]
	public float rotationAmount = 2f;
	[Tooltip("Time multiplier for camera transforms")]
	public float movementTime = 10f;
	[Tooltip("Movement speed of the camera rig when in Free camera mode")]
	public float MoveSpeed = 0.03f;
	[Tooltip("Vertical move speed when panning")]
	public float VerticalPanSpeed = .05f;
	[Tooltip("Vertical move speed multiplier when not in FreeCamera")]
	public float VerticalLockMultiplier = 8;
	[Tooltip("Vertical move speed of when panning and not in FreeCamera")]
	public float VerticalMoveSpeedLocked = .1f;
	[Tooltip("Multiplier for vertical movement")]
	public float VerticalMoveMultiplier = .003f;
	[Tooltip("Multiplier for vertical movement when not in FreeCamera")]
	public float VerticalLockMoveMultiplier = .01f;

	[System.Serializable]
	public enum MoveTypes {
	 Walk,
	 Sprint,
	 Run
	}

	[Header("Avatar Locomotion Settings")]
	[SerializeField] public MoveTypes MoveType = MoveTypes.Walk;
	[SerializeField] public float WalkVelocity = 1.56f;
	[SerializeField] public float RunVelocity = 3.4f;
	[SerializeField] public float SprintVelocity = 5.96f;
	[Tooltip("Degrees/Second")]
	[SerializeField] public float RotationAmount = 100;

	[Header("Compiler Settings")]
	public string WorkingDirectory = "TEA_Temp";

	[Header("Play Tab Toggles Settings")]
	[SerializeField] public string keepInSceneTooltip = "Keep the TEA Manager prefab in your Scene while not in play mode";
	[SerializeField] public bool keepInScene = true;
	[SerializeField] public string CanvasTooltip = "TEA Canvas ON-OFF, will activate when you play";
	[SerializeField] public bool CanvasActive = false;
	[SerializeField] public string worldCenterTooltip = "World Center ON-OFF";
	[SerializeField] public bool WorldCenterActive = true;
	[SerializeField] public string AudioListenerTooltip = "Audio Listener ON-OFF";
	[SerializeField] public bool AudioListenerActive = true;
	[SerializeField] public string LighTooltipt = "Directional Light ON-OFF";
	[SerializeField] public bool LightActive = true;
	[SerializeField] public string StageTooltip = "Stage ON-OFF";
	[SerializeField] public bool StageActive = true;
	[SerializeField] public string ValidateTooltip = "Validate SDK Compliance and All Parameters used";
	[SerializeField] public bool ValidateActive = true;

	[Header("Play Tab Layout Settings")]
	[SerializeField] public bool AllLayout = true;
	[SerializeField] public bool BeforeToggles = true;
	[SerializeField] public bool AfterToggles = true;
	[SerializeField] public bool BeforeButtons = true;
	[SerializeField] public bool AfterButtons = true;
	[SerializeField] public bool BeforeInfo = true;
	[SerializeField] public bool AfterInfo = true;
 }
}