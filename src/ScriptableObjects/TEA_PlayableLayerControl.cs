using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace TEA.ScriptableObject {
 public class TEA_PlayableLayerControl : StateMachineBehaviour {
 [Tooltip("Layer to affect")]
 public VRC_PlayableLayerControl.BlendableLayer layer;
 [Range(0, 1)]
 [Tooltip("Goal weight 0-1")]
 public float goalWeight;
 [Tooltip("Time to reach goal weight")]
 public float blendDuration;
 [Tooltip("Message for debugging")]
 public string debugString;
 [HideInInspector]
 public float duration = 0f;
 [HideInInspector]
 public string state;

  [HideInInspector]
  public static ApplySettingsDelegate ApplySettings;
  [HideInInspector]
  public delegate void ApplySettingsDelegate(TEA_PlayableLayerControl control, Animator animator);

  public override void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) {
   if(!string.IsNullOrEmpty(debugString))
    Debug.Log($"VRCPlayableLayerControl[{state}] {debugString}");
   ApplySettings(this, animator);
  }

  public static  VRCAvatarDescriptor.AnimLayerType AnimLayerType(VRC_PlayableLayerControl.BlendableLayer layer) {
   if(layer == VRC_PlayableLayerControl.BlendableLayer.Action)
    return VRCAvatarDescriptor.AnimLayerType.Action;
   if(layer==VRC_PlayableLayerControl.BlendableLayer.Additive)
    return VRCAvatarDescriptor.AnimLayerType.Additive;
   if(layer==VRC_PlayableLayerControl.BlendableLayer.FX)
    return VRCAvatarDescriptor.AnimLayerType.FX;
   if(layer==VRC_PlayableLayerControl.BlendableLayer.Gesture)
    return VRCAvatarDescriptor.AnimLayerType.Gesture;
   return VRCAvatarDescriptor.AnimLayerType.Action;
  }
 }
}