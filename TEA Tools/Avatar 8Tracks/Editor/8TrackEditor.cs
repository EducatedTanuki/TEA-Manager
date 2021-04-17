using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;

namespace TEA.Tools {
 public class Avatar8TrackEditor : EditorWindow {
  private AnimatorController fxAnimator;
  public AudioClip AudioClip { get; private set; }
  public GameObject _8TrackObject;

  Avatar8TrackSettings settings;
  public Texture2D FolderIcon { get; private set; }
  public Texture2D StopIcon { get; private set; }
  public Texture2D VolumeIcon { get; private set; }
  public Texture2D DiskIcon { get; private set; }
  public Texture2D TrackIcon { get; private set; }

  public string VolumeEPName { get; set; }
  public string TrackEPName { get; set; }
  public bool PlayerLoops = false;

  public VRCAvatarDescriptor Avatar;

  public Avatar8TrackEditor() {
   var assets = AssetDatabase.FindAssets("t:Avatar8TrackSettings");
   if(null!=assets||assets.Length!=0) {
    settings=AssetDatabase.LoadAssetAtPath<Avatar8TrackSettings>(AssetDatabase.GUIDToAssetPath(assets[0]));
    FolderIcon=settings.FolderIcon;
    StopIcon=settings.StopIcon;
    VolumeIcon=settings.VolumeIcon;
    DiskIcon=settings.DiskIcon;
    TrackIcon=settings.TrackIcon;
   }
  }

  public Avatar8TrackEditor(VRCAvatarDescriptor avatar) {
   var assets = AssetDatabase.FindAssets("t:Avatar8TrackSettings");
   if(null!=assets||assets.Length!=0) { 
    settings=AssetDatabase.LoadAssetAtPath<Avatar8TrackSettings>(AssetDatabase.GUIDToAssetPath(assets[0]));
    FolderIcon=settings.FolderIcon;
    StopIcon=settings.StopIcon;
    VolumeIcon=settings.VolumeIcon;
    DiskIcon=settings.DiskIcon;
    TrackIcon=settings.TrackIcon;
   }
   SetFromAvatar(avatar);
  }

  public bool IsValid() {
   return null!=fxAnimator&&null!=AudioClip;
  }

  public void OnGUI() {
   if(null!=fxAnimator&&fxAnimator!=Avatar.baseAnimationLayers[4].animatorController)
    EditorGUILayout.HelpBox($"The FX Layer is not assigned to the chosen avatar[{Avatar.name}]", MessageType.Warning, true);
   if(null==fxAnimator)
    EditorGUILayout.HelpBox("FX Layer is required", MessageType.Error, true);
   EditorGUILayout.LabelField("Your FX Animator Controller");
   fxAnimator=(AnimatorController)EditorGUILayout.ObjectField(fxAnimator, typeof(AnimatorController), true);
   EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

   if(null==AudioClip)
    EditorGUILayout.HelpBox("Audio Clip is required", MessageType.Error, true);
   EditorGUILayout.LabelField("Any song in your Song Library");
   AudioClip=(AudioClip)EditorGUILayout.ObjectField(AudioClip, typeof(AudioClip), true);
   EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

   EditorGUILayout.LabelField("Player loops after clip ends");
   PlayerLoops=EditorGUILayout.Toggle(PlayerLoops);
   EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

   // images
   EditorGUILayout.HelpBox("Icons used for the ExpressionsMenu controls"
   +"\n"
   +"feel free to skip this. we brought our own :)"
   , MessageType.None, true);
   EditorGUILayout.LabelField("Folder Icon: "+AssetDatabase.GetAssetPath(FolderIcon));
   FolderIcon=(Texture2D)EditorGUILayout.ObjectField(FolderIcon, typeof(Texture2D), true);
   EditorGUILayout.LabelField("Play/Stop Icon: "+AssetDatabase.GetAssetPath(StopIcon));
   StopIcon=(Texture2D)EditorGUILayout.ObjectField(StopIcon, typeof(Texture2D), true);
   EditorGUILayout.LabelField("Volume Icon");
   VolumeIcon=(Texture2D)EditorGUILayout.ObjectField(VolumeIcon, typeof(Texture2D), true);
   EditorGUILayout.LabelField("Next Disk/Sub Menu Icon");
   DiskIcon=(Texture2D)EditorGUILayout.ObjectField(DiskIcon, typeof(Texture2D), true);
   EditorGUILayout.LabelField("Track Icon");
   TrackIcon=(Texture2D)EditorGUILayout.ObjectField(TrackIcon, typeof(Texture2D), true);
  }

  public void SetFromAvatar(VRCAvatarDescriptor avatar) {
   if(null!=avatar) {
    Avatar=avatar;
    fxAnimator=AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(avatar.baseAnimationLayers[4].animatorController));
    Transform _8trackTransform = avatar.gameObject.transform.Find("8Track");
    if(null!=_8trackTransform)
     _8TrackObject=_8trackTransform.gameObject;
    else
     _8TrackObject=null;
   }
  }

  public bool BurnTracks(List<AudioClip> libAssets) {
   if(null==fxAnimator) {
    throw new System.Exception("We need your FX Animator Controller");
   }
   if(null==AudioClip) {
    throw new System.Exception("We need an AudioClip from your Song Library");
   }
   if(null==_8TrackObject) {
    throw new System.Exception("8Track object was not set");
   }
   _8TrackObject.SetActive(false);

   int childIndex = 0;
   while(true) {
    if(0==_8TrackObject.transform.childCount||childIndex>=_8TrackObject.transform.childCount) {
     break;
    } else if(_8TrackObject.transform.GetChild(childIndex).gameObject.name.StartsWith("Track")) {
     Debug.Log("deleting child "+_8TrackObject.transform.GetChild(childIndex).gameObject.name);
     DestroyImmediate(_8TrackObject.transform.GetChild(childIndex).gameObject);
    } else
     childIndex++;
   }

   RemoveExistingAnimatorLayer(fxAnimator, _8TrackObject.name);
   HandleAnimatorParameters(fxAnimator, _8TrackObject.name);

   //--- --- Volume Animation Controller --- ---
   AnimationClip volumeSet = new AnimationClip {
    name=VolumeEPName
   };

   //--- --- Track Animation Controller --- ---
   AnimatorControllerLayer trackLayer = new AnimatorControllerLayer {
    name=_8TrackObject.name,
    defaultWeight=1,
    stateMachine=new AnimatorStateMachine()
   };
   fxAnimator.AddLayer(trackLayer);
   AnimatorStateMachine stateMachine = trackLayer.stateMachine;

   //--- --- No Track --- ---
   int trackCount = 0;
   AnimatorState defaultState = stateMachine.AddState("Stop");
   defaultState.speed=100;
   AnimatorStateTransition anyToWait = stateMachine.AddAnyStateTransition(defaultState);
   anyToWait.AddCondition(AnimatorConditionMode.Equals, trackCount, TrackEPName);
   anyToWait.hasExitTime=true;
   anyToWait.exitTime=0;
   anyToWait.duration=0;

   AnimatorState loopState = stateMachine.AddState("LoopOff");
   loopState.speed=100;
   if(!PlayerLoops) {
    PersistAnimator(fxAnimator);
    VRCAvatarParameterDriver driver = loopState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
    driver.parameters.Add(new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter() {
     name=TrackEPName,
     value=0,
     type=VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set
    });
   }
   AnimatorStateTransition loopExit = loopState.AddExitTransition();
   loopExit.hasExitTime=true;
   loopExit.exitTime=0;
   loopExit.duration=0;

   //--- --- All Tracks --- ---
   foreach(Object asset in libAssets) {
    if(asset is AudioClip) {
     //Animation setup
     string trackName = "Track "+ ++trackCount;

     AnimationClip tracktoggle = new AnimationClip {
      name=trackName
     };

     AudioClip clip = asset as AudioClip;

     tracktoggle.SetCurve(_8TrackObject.name, typeof(GameObject), "m_IsActive", AnimationCurve.Linear(0.0f, 1f, clip.length, 1f));
     tracktoggle.SetCurve(_8TrackObject.name+"/"+trackName, typeof(GameObject), "m_IsActive", AnimationCurve.Linear(0.0f, 1f, clip.length, 1f));
     AnimatorState state = stateMachine.AddState(trackName);
     state.motion=tracktoggle;
     AnimatorStateTransition toTrack = defaultState.AddTransition(state);
     toTrack.AddCondition(AnimatorConditionMode.Equals, trackCount, TrackEPName);
     toTrack.hasExitTime=true;
     toTrack.exitTime=0;
     toTrack.duration=0;
     AnimatorStateTransition trackOut = state.AddTransition(loopState);
     trackOut.hasExitTime=true;
     trackOut.exitTime=1;
     trackOut.duration=0;
     AnimatorStateTransition trackSwitch = state.AddTransition(defaultState);
     trackSwitch.AddCondition(AnimatorConditionMode.NotEqual, trackCount, TrackEPName);
     trackSwitch.duration=0;
     Avatar8Track.CreateAsset(AudioClip, tracktoggle, trackName+".anim");

     //volume
     volumeSet.SetCurve(_8TrackObject.name+"/"+trackName, typeof(AudioSource), "m_Volume", AnimationCurve.Linear(0.0f, 0f, 0.033333335f, 1f));

     //Object setup
     GameObject trackObject = new GameObject(trackName, new System.Type[] { typeof(AudioSource) });
     trackObject.transform.SetParent(_8TrackObject.transform);
     trackObject.SetActive(false);
     //trackObject.AddComponent<AudioSource>();
     Avatar8Track.CopyComponent(_8TrackObject.GetComponent<AudioSource>(), trackObject.GetComponent<AudioSource>());
     AudioSource source = trackObject.GetComponent<AudioSource>();
     source.enabled=true;
     source.clip=clip;
     trackObject.name=trackName;
    }
   }

   fxAnimator.AddLayer(CreateVolumeLayer(_8TrackObject.name, VolumeEPName, volumeSet));
   PersistAnimator(fxAnimator);

   return true;
  }

  private void PersistAnimator(AnimatorController controller) {
   foreach(AnimatorControllerLayer layer in controller.layers) {
    if(""==AssetDatabase.GetAssetPath(layer.stateMachine)) {
     AssetDatabase.AddObjectToAsset(layer.stateMachine, controller);
     foreach(ChildAnimatorStateMachine cSM in layer.stateMachine.stateMachines) {
      AssetDatabase.AddObjectToAsset(cSM.stateMachine, controller);
      PersistStateMachine(controller, cSM.stateMachine);
     }
     PersistStateMachine(controller, layer.stateMachine);
    }
   }
   UnityEditor.EditorUtility.SetDirty(fxAnimator);
   AssetDatabase.SaveAssets();
  }

  private void PersistStateMachine(AnimatorController controller, AnimatorStateMachine stateMachine) {
   foreach(ChildAnimatorState cState in stateMachine.states) {
    AssetDatabase.AddObjectToAsset(cState.state, controller);
    foreach(AnimatorStateTransition transition in cState.state.transitions) {
     AssetDatabase.AddObjectToAsset(transition, controller);
    }
   }
   foreach(AnimatorStateTransition transition in stateMachine.anyStateTransitions) {
    AssetDatabase.AddObjectToAsset(transition, controller);
   }
   foreach(AnimatorTransition transition in stateMachine.entryTransitions) {
    AssetDatabase.AddObjectToAsset(transition, controller);
   }
  }

  private AnimatorControllerLayer CreatePlayLayer(string rootName, string paramName) {
   AnimatorControllerLayer playLayer = new AnimatorControllerLayer {
    name=paramName,
    defaultWeight=1,
    stateMachine=new AnimatorStateMachine()
   };
   AnimatorStateMachine stateMachine = playLayer.stateMachine;

   AnimatorState stopState = stateMachine.AddState("Stop");
   stateMachine.AddAnyStateTransition(stopState).AddCondition(AnimatorConditionMode.Equals, 0, paramName);

   AnimatorState playState = stateMachine.AddState(paramName);
   AnimationClip playtoggle = new AnimationClip {
    name=paramName
   };
   playtoggle.SetCurve(rootName, typeof(GameObject), "m_IsActive", AnimationCurve.Linear(0.0f, 1f, 0.0f, 1f));
   playState.motion=playtoggle;
   stateMachine.AddAnyStateTransition(playState).AddCondition(AnimatorConditionMode.Equals, 1, paramName);
   Avatar8Track.CreateAsset(AudioClip, playtoggle, paramName+".anim");

   return playLayer;
  }

  private AnimatorControllerLayer CreateVolumeLayer(string rootName, string paramName, AnimationClip volumeSet) {
   AnimatorControllerLayer layer = new AnimatorControllerLayer {
    name=paramName,
    defaultWeight=1,
    stateMachine=new AnimatorStateMachine()
   };
   AnimatorStateMachine stateMachine = layer.stateMachine;

   AnimatorState waitState = stateMachine.AddState("Wait");

   AnimatorState toggleState = stateMachine.AddState(paramName);
   toggleState.timeParameter=paramName;
   toggleState.timeParameterActive=true;
   toggleState.motion=volumeSet;
   waitState.AddTransition(toggleState).AddCondition(AnimatorConditionMode.Greater, 0, paramName);
   Avatar8Track.CreateAsset(AudioClip, volumeSet, paramName+".anim");

   return layer;
  }

  private void HandleAnimatorParameters(AnimatorController fxAnimator, string name) {
   foreach(AnimatorControllerParameter param in fxAnimator.parameters) {
    if(param.name==VolumeEPName||param.name==TrackEPName)
     fxAnimator.RemoveParameter(param);
   }
   fxAnimator.AddParameter(VolumeEPName, AnimatorControllerParameterType.Float);
   fxAnimator.AddParameter(TrackEPName, AnimatorControllerParameterType.Int);
  }

  private void RemoveExistingAnimatorLayer(AnimatorController fxAnimator, string name) {
   for(var i = 0; i<fxAnimator.layers.Length; i++) {
    Debug.Log("Layer name: "+fxAnimator.layers[i].name);
    if(fxAnimator.layers[i].name==_8TrackObject.name||fxAnimator.layers[i].name==VolumeEPName||fxAnimator.layers[i].name==TrackEPName) {
     Debug.Log("- Deleting layer: "+fxAnimator.layers[i].name);
     fxAnimator.RemoveLayer(i);
     i=0;
    }
   }
  }
 }
}