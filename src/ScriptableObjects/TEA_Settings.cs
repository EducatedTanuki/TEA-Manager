﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace TEA {
 [System.Serializable]
 [CreateAssetMenu(fileName = "Default Settings", menuName = "ScriptableObjects/TEA_Settings", order = 1)]
 public class TEA_Settings : UnityEngine.ScriptableObject {
  [Header("Make Avatar 3.0")]
  [SerializeField] public string ExpressionsFolder = "Expressions";
  [SerializeField] public VRCExpressionsMenu ExpressionsMenu;
  [SerializeField] public VRCExpressionParameters ExpressionParameters;
  [Space]
  [SerializeField] public string PlayableLayersFolder = "Playable Layers";
  [Tooltip("Copy Layer to Avatar Folder (turn off if you want all avatars to point to the same asset)")]
  [SerializeField] public bool BaseCopy = true;
  [SerializeField] public RuntimeAnimatorController Base;
  [Tooltip("Copy Layer to Avatar Folder (turn off if you want all avatars to point to the same asset)")]
  [SerializeField] public bool AdditiveCopy = true;
  [SerializeField] public RuntimeAnimatorController Additive;
  [Tooltip("Copy Layer to Avatar Folder (turn off if you want all avatars to point to the same asset)")]
  [SerializeField] public bool GestureCopy = true;
  [SerializeField] public RuntimeAnimatorController Gesture;
  [Tooltip("Copy Layer to Avatar Folder (turn off if you want all avatars to point to the same asset)")]
  [SerializeField] public bool ActionCopy = true;
  [SerializeField] public RuntimeAnimatorController Action;
  [Tooltip("Copy Layer to Avatar Folder (not recommended to turn off)")]
  [SerializeField] public bool FXCopy = true;
  [SerializeField] public RuntimeAnimatorController FX;
  [Space]
  [SerializeField] public RuntimeAnimatorController Sitting;
  [SerializeField] public RuntimeAnimatorController TPose;
  [SerializeField] public RuntimeAnimatorController IKPose;
  [Space]
  [Tooltip("When zero the avatars calculated viewport will be used")]
  [SerializeField] public Vector3 PortraitCameraPositionOffset = Vector3.zero;
  [SerializeField] public Quaternion EyeLookDownLeft = new Quaternion(0.3f, 0f, 0f, 1f);
  [SerializeField] public Quaternion EyeLookDownRight = new Quaternion(0.3f, 0f, 0f, 1f);
  [SerializeField] public Quaternion EyeLookUpLeft = new Quaternion(-0.2f, 0f, 0f, 1f);
  [SerializeField] public Quaternion EyeLookUpRight = new Quaternion(-0.2f, 0f, 0f, 1f);
  [SerializeField] public Quaternion EyeLookRightLeft = new Quaternion(0f, 0.2f, 0f, 1f);
  [SerializeField] public Quaternion EyeLookRightRight = new Quaternion(0f, 0.2f, 0f, 1f);
  [SerializeField] public Quaternion EyeLookLeftLeft = new Quaternion(0f, -0.2f, 0f, 1f);
  [SerializeField] public Quaternion EyeLookLeftRight = new Quaternion(0f, -0.2f, 0f, 1f);

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
  [SerializeField] public float RotationAmount = 3;

  [Header("Compiler Settings")]
  public string WorkingDirectory = "TEA_Temp";
 }
}