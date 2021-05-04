using VRC.SDK3.Avatars.Components;
using UnityEngine;

namespace TEA.ScriptableObject {
 [CreateAssetMenu(fileName = "TEA_PlayableLayerData", menuName = "ScriptableObjects/TEA_PlayableLayerData", order = 1)]
 public class TEA_PlayableLayerData : UnityEngine.ScriptableObject {

  public TEA_PlayableLayerData() {
   data=new PlayableLayerData[5];
   data[0] = new PlayableLayerData(VRCAvatarDescriptor.AnimLayerType.Base);
   data[1]=new PlayableLayerData(VRCAvatarDescriptor.AnimLayerType.Additive);
   data[2]=new PlayableLayerData(VRCAvatarDescriptor.AnimLayerType.Gesture);
   data[3]=new PlayableLayerData(VRCAvatarDescriptor.AnimLayerType.Action);
   data[4]=new PlayableLayerData(VRCAvatarDescriptor.AnimLayerType.FX);
  }

  public PlayableLayerData FindPlayableLayerData(VRCAvatarDescriptor.AnimLayerType type) {
   foreach(PlayableLayerData item in data) {
    if(item.layer==type)
     return item;
   }
   return null;
  }

  public string AvatarName;
  [SerializeField]
  public PlayableLayerData[] data;

  [System.Serializable]
  public class PlayableLayerData {
   public VRCAvatarDescriptor.AnimLayerType layer;
   public int start;
   public int end;

   public PlayableLayerData() {
    layer =VRCAvatarDescriptor.AnimLayerType.Base;
    start = 0;
    end = 0;
   }
   public PlayableLayerData(VRCAvatarDescriptor.AnimLayerType type) {
    layer=type;
    start=0;
    end=0;
   }
  }
 }
}