using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace TEA {
 [System.Serializable]
 public class TEA_ValidationIssues : UnityEngine.ScriptableObject {
  [System.Serializable]
  public class Issue {
   public string Cause;
   public List<Object> Reference = new List<Object>();

   public Issue() { }

   public Issue(string cause) {
    Cause=cause;
   }

   public Issue(string cause, Object reference) {
    Cause=cause;
    Reference = new List<Object>();
    Reference.Add(reference);
   }
  }

  public string AvatarName;

  public List<Issue> BaseLayer = new List<Issue>();
  public List<Issue> AdditiveLayer = new List<Issue>();
  public List<Issue> GestureLayer = new List<Issue>();
  public List<Issue> ActionLayer = new List<Issue>();
  public List<Issue> FXLayer = new List<Issue>();

  public List<Issue> GetLayer(VRCAvatarDescriptor.AnimLayerType layer) {
   if(VRCAvatarDescriptor.AnimLayerType.Base==layer) {
    return BaseLayer;
   } else if(VRCAvatarDescriptor.AnimLayerType.Additive==layer) {
    return AdditiveLayer;
   } else if(VRCAvatarDescriptor.AnimLayerType.Gesture==layer) {
    return GestureLayer;
   } else if(VRCAvatarDescriptor.AnimLayerType.Action==layer) {
    return ActionLayer;
   } else if(VRCAvatarDescriptor.AnimLayerType.FX==layer) {
    return FXLayer;
   }
   throw new System.Exception("TEA Manager has a playable layer mapping issue, the VRChat SDK may not be compatible");
  }

  public List<string> ParametersNotInAnimators = new List<string>();

  public List<Issue> ParameterDrivers = new List<Issue>();
 }


}