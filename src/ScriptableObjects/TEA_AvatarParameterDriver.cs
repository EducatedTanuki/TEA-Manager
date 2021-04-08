using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace TEA.ScriptableObject {
 public class TEA_AvatarParameterDriver : StateMachineBehaviour {
  public List<VRC_AvatarParameterDriver.Parameter> parameters;
  public bool localOnly;
  public string debugString;
  [HideInInspector]
  public string state;

  [HideInInspector]
  public static ApplySettingsDelegate ApplySettings;
  [HideInInspector]
  public delegate void ApplySettingsDelegate(TEA_AvatarParameterDriver driver, Animator animator);

  public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
   if(!string.IsNullOrEmpty(debugString))
    Debug.Log($"VRCPlayableLayerControl[{state}] {debugString}");
   foreach(VRC_AvatarParameterDriver.Parameter param in parameters) {
    AnimatorControllerParameterType paramType = AvatarController.current.GetParameterType(param.name);
    float value = param.value;
    bool bValue = param.value>0;
    if(VRC_AvatarParameterDriver.ChangeType.Add==param.type) {
     value += AvatarController.current.GetParameterValue(param.name);
    }else if(VRC_AvatarParameterDriver.ChangeType.Random==param.type) {
     System.Random random = new System.Random();
     //NOTE my best guess at what chance means
     if(AnimatorControllerParameterType.Bool==paramType) { 
      bool currentBool = AvatarController.current.GetBool(param.name);
       bValue = random.NextDouble()>=param.chance ? !currentBool : currentBool;
     }
     else
      value = (float)(random.NextDouble()*(param.valueMax - param.valueMin) + param.valueMin);
    }
    // --- set
    AvatarController.Parameter aParam = new AvatarController.Parameter() {
     name=param.name,
     type=paramType,
     boolean=bValue,
     fVal=value,
     iVal=Mathf.RoundToInt(value)
    };
    AvatarController.current.SetAnimatorParameter(aParam);
   }//for
  }
 }
}
