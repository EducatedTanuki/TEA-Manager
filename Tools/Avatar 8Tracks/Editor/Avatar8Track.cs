using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Reflection;
using System.Linq;

/*
 * 
 */
namespace TEA.Tools {
 public enum Stage {
  Instruction,
  Avatar,
  _8Track,
  AudioSource,
  Burn
 }

 public class Avatar8Track : EditorWindow {
  //--- stage ---
  private Stage stage = Stage.Instruction;

  //--- GUI ---
  [SerializeField]
  private Vector2 scrollPos;
  [SerializeField]
  private Vector2 typeScroll;

  //--- audio source ---
  private AudioSource audioSource = new AudioSource();
  private SerializedObject sAudioSource;

  //--- Properties ---
  internal static string trackEPNameSuffix = "_track_selected";
  internal static string volumeEPNameSuffix = "_volume";

  Avatar8TrackEditor _8TrackEditor;
  bool expressionValid = true;
  VRCExpressionParameters expressionParameters;
  bool menuValid = true;
  VRCExpressionsMenu expressionsMenu;
  bool _8trackValid = false;

  //--- Avatar ---
  private int avatarIndex = 0;
  private string[] avatarKeys;
  private Dictionary<string, VRCAvatarDescriptor> avatars;

  [MenuItem("TEA Manager/Tools/Avatar 8Track",false,10)]
  static void MakeDisks() {
   EditorWindow.GetWindow(typeof(Avatar8Track), true, "Avatar 8Tracks", true);
  }

  private void OnEnable() {
   avatars=GetAvatars();
   avatarKeys=avatars.Keys.ToArray();
   if(0!=avatars.Count) {
    _8TrackEditor=new Avatar8TrackEditor(avatars[avatarKeys[0]]);
    expressionParameters=avatars[avatarKeys[0]].expressionParameters;
    expressionsMenu=avatars[avatarKeys[0]].expressionsMenu;
   } else
    _8TrackEditor=new Avatar8TrackEditor();
  }

  void OnGUI() {
   EditorGUILayout.BeginHorizontal();
   scrollPos=EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
   if(Stage.Instruction==stage) {
    EditorGUILayout.HelpBox("Welcome to Avatar 8Track. This tool let's you automagically turn a folder of songs into a music player in your avatars radial menu.\n"
    +"Song Library Setup\n"
    +"   - Create a folder somewhere under Assets\n"
    +"   - Put all of your songs into that folder\n"
    +"        The folder name does not matter.\n"
    +"        All songs from the folder will be added\n"
    , MessageType.None, true);
    EditorGUILayout.HelpBox("Do not store anything but your songs in the Song Library, it may be deleted", MessageType.Warning, true);
    EditorGUILayout.HelpBox("One song library per avatar!", MessageType.Warning, true);
    EditorGUILayout.HelpBox("If you update the library with new songs just run the tool again", MessageType.Info, true);
    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

    // --- Avatar stuff ---
    EditorGUI.BeginChangeCheck();
    avatarIndex=EditorGUILayout.Popup("Avatar", avatarIndex, avatarKeys, EditorStyles.popup);
    if(EditorGUI.EndChangeCheck()) {
     expressionParameters=avatars[avatarKeys[avatarIndex]].expressionParameters;
     expressionsMenu=avatars[avatarKeys[avatarIndex]].expressionsMenu;
     _8TrackEditor.SetFromAvatar(avatars[avatarKeys[avatarIndex]]);
    }
    //--- 8Track object
    EditorGUILayout.HelpBox("The 8Track object will hold all of your AudioClip Components."
     +"\nIt must be a first child of your avatar."
     +"\nThe name of the 8Track object will determine names for various assets."
     +"\nI highly recommend you use the default object.", MessageType.Info, true);
    EditorGUILayout.LabelField("The 8Track Object");
    _8TrackEditor._8TrackObject=(GameObject)EditorGUILayout.ObjectField(_8TrackEditor._8TrackObject, typeof(GameObject), true);
    if(null==_8TrackEditor._8TrackObject) {
     _8trackValid=false;
     if(GUILayout.Button("Create _8Track Object", GUILayout.Height(30))) {
      _8TrackEditor._8TrackObject=new GameObject("8Track");
      _8TrackEditor._8TrackObject.transform.parent=_8TrackEditor.Avatar.transform;
      Debug.Log($"Created new object for tracks [{_8TrackEditor._8TrackObject.name}]");
     } else
      _8TrackEditor.SetFromAvatar(avatars[avatarKeys[avatarIndex]]);
    } else if(_8TrackEditor._8TrackObject.transform.parent!=_8TrackEditor.Avatar.transform) {
     EditorGUILayout.HelpBox("The 8Track object must be a child of the avatar selected", MessageType.Error, true);
     _8trackValid=false;
    } else
     _8trackValid=true;

    //set var
    if(null!=_8TrackEditor._8TrackObject) {
     _8TrackEditor.VolumeEPName=Avatar8Track.GetVarName(_8TrackEditor._8TrackObject, Avatar8Track.volumeEPNameSuffix);
     _8TrackEditor.TrackEPName=Avatar8Track.GetVarName(_8TrackEditor._8TrackObject, Avatar8Track.trackEPNameSuffix);
    }

    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

    //--- Parameters
    if(expressionValid)
     EditorGUILayout.HelpBox("A Integer and Float parameter will be added", MessageType.Info, true);
    else if(null!=expressionParameters) {
     EditorGUILayout.HelpBox("The ExpressionParameters does not have space for a float and an int"+"\nYou can delete empty parameters to make room", MessageType.Error, true);
     if(GUILayout.Button("Delete Empty Parameters", GUILayout.Height(30))) {
      for(int i = 0; i<expressionParameters.parameters.Length; i++) {
       if(string.IsNullOrEmpty(expressionParameters.parameters[i].name)) {
        ArrayUtility.RemoveAt<VRCExpressionParameters.Parameter>(ref expressionParameters.parameters, i);
        i--;
       }
      }
     }
    } else
     EditorGUILayout.HelpBox("An ExpressionParameters object is required", MessageType.Error, true);

    EditorGUILayout.LabelField("VRC ExpressionParameters File");
    expressionParameters=(VRCExpressionParameters)EditorGUILayout.ObjectField(expressionParameters, typeof(VRCExpressionParameters), true);

    if(null!=expressionParameters && null != _8TrackEditor._8TrackObject) {
     int cost = VRCExpressionParameters.TypeCost(VRCExpressionParameters.ValueType.Int)+VRCExpressionParameters.TypeCost(VRCExpressionParameters.ValueType.Float);
     VRCExpressionParameters.Parameter[] parameters = expressionParameters.parameters;
     foreach(VRCExpressionParameters.Parameter parameter in parameters) {
      if(parameter.name==_8TrackEditor.VolumeEPName)
       cost-=VRCExpressionParameters.TypeCost(VRCExpressionParameters.ValueType.Float);
      else if(parameter.name==_8TrackEditor.TrackEPName)
       cost-=VRCExpressionParameters.TypeCost(VRCExpressionParameters.ValueType.Int);
     }
     expressionValid=expressionParameters.CalcTotalCost()+cost<=VRCExpressionParameters.MAX_PARAMETER_COST;
    } else
     expressionValid=null!=expressionParameters;
    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

    //--- Menu
    if(menuValid)
     EditorGUILayout.HelpBox("Which VRCExpressionsMenu do you want the SubMenu added to", MessageType.Info, true);
    else if(null!=expressionsMenu)
     EditorGUILayout.HelpBox("The VRCExpressionsMenu is full", MessageType.Error, true);

    if(null==expressionsMenu)
     EditorGUILayout.HelpBox("ExpressionsMenu is not set", MessageType.Warning, true);

    EditorGUILayout.LabelField("VRC Expressions Menu File");
    expressionsMenu=(VRCExpressionsMenu)EditorGUILayout.ObjectField(expressionsMenu, typeof(VRCExpressionsMenu), true);

    if(null!=expressionsMenu && null!=_8TrackEditor._8TrackObject) {
     menuValid=expressionsMenu.controls.Count<VRCExpressionsMenu.MAX_CONTROLS;
     foreach(VRCExpressionsMenu.Control control in expressionsMenu.controls) {
      if(control.name==_8TrackEditor._8TrackObject.name) {
       menuValid=true;
      }
     }
    } else
     menuValid=true;


    if(null!=expressionParameters&&menuValid&&expressionValid&&_8trackValid) {
     Button(Stage._8Track, "Setup");
    }
   } else if(Stage._8Track==stage) {
    _8TrackEditor.OnGUI();
    Button(Stage.Instruction, "Back <-");
    if(_8TrackEditor.IsValid()) {
     Button(Stage.Burn, "Continue ->");
    }
   }

   if(Stage.Burn==stage) {
    EditorGUILayout.HelpBox("Read Carefully!!!!!!", MessageType.Error);
    EditorGUILayout.HelpBox("This will break the 8Track functionality for any other avatar this song library has been added to previously."
     +"\nDuplicate the song library if you want to add it to a separate avatar.", MessageType.Warning);
    EditorGUILayout.HelpBox("Avatar 8Tracks lets you update your song library by re-running it on the same avatar."
     +"\nfor convenience it auto-detects and deletes assets it has created previously and replaces them."
     +"\nDouble check the following items below were not manually edited by you."
     , MessageType.Warning);
    EditorGUILayout.LabelField("FX Controller Layers, Expression Parameters, and Expression Menu Controls with the following names", EditorStyles.wordWrappedLabel);
    EditorGUILayout.LabelField($"     [{_8TrackEditor.TrackEPName}]", EditorStyles.boldLabel);
    EditorGUILayout.LabelField($"     [{_8TrackEditor.VolumeEPName}]", EditorStyles.boldLabel);
    EditorGUILayout.LabelField($"     [{_8TrackEditor._8TrackObject.name}]", EditorStyles.boldLabel);
    EditorGUILayout.HelpBox($"All AnimationClips, and ExpressionsMenus will be deleted in [{GetAssetDirectory(_8TrackEditor.AudioClip, true)}]", MessageType.Warning);
    Button(Stage._8Track, "Back <-");
    if(GUILayout.Button("Accept And Burn Tracks", GUILayout.Height(30))) {
     try {
      string songLibrary = GetAssetDirectory(_8TrackEditor.AudioClip, false);
      CleanSongLibrary(songLibrary);
      List<AudioClip> libAssets = GetSongList(songLibrary);
      _8TrackEditor.BurnTracks(libAssets);
      VRC_Actions(songLibrary, libAssets, _8TrackEditor);
      AssetDatabase.SaveAssets();
      EditorUtility.DisplayDialog("Avatar 8Tracks", "You're ready to rock", "great");
      this.Close();
     } catch(System.Exception e) {
      EditorUtility.DisplayDialog("so... this happened", e.Message, "sorry");
      Debug.LogException(e);
      this.Close();
     }
    }
    DrawEnd();
   } else {
    DrawEnd();
   }
  }

  private static void DrawEnd() {
   EditorGUILayout.EndScrollView();
   EditorGUILayout.EndHorizontal();
  }

  private void Button(Stage setStage, string text) {
   if(GUILayout.Button(text, GUILayout.Height(30))) {
    stage=setStage;
   }
  }

  internal static void CleanSongLibrary(string songLibrary) {
   var assets = AssetDatabase.FindAssets("t:AnimationClip", new[] { songLibrary });
   foreach(var guid in assets) {
    Debug.Log("Deleting: "+guid+": "+AssetDatabase.GUIDToAssetPath(guid));
    AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
   }
   assets=AssetDatabase.FindAssets("t:VRCExpressionsMenu", new[] { songLibrary });
   foreach(var guid in assets) {
    Debug.Log("Deleting: "+guid+": "+AssetDatabase.GUIDToAssetPath(guid));
    AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
   }
  }

  internal static List<AudioClip> GetSongList(string songLibrary) {
   List<AudioClip> audioClips = new List<AudioClip>();
   var assets = AssetDatabase.FindAssets("t:AudioClip", new[] { songLibrary });
   foreach(var guid in assets) {
    Debug.Log(guid);
    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
    audioClips.Add(clip);
   }
   return audioClips;
  }


  private bool VRC_Actions(string songLibrary, List<AudioClip> libAssets, Avatar8TrackEditor _8Track) {
   //--- --- VRC Expression Parameters --- ---
   if(null!=expressionParameters) {
    VRCExpressionParameters.Parameter volumeParam = new VRCExpressionParameters.Parameter() { name=_8Track.VolumeEPName, valueType=VRCExpressionParameters.ValueType.Float };
    VRCExpressionParameters.Parameter trackParam = new VRCExpressionParameters.Parameter() { name=_8Track.TrackEPName, valueType=VRCExpressionParameters.ValueType.Int };

    VRCExpressionParameters.Parameter[] parameters = expressionParameters.parameters;
    bool hasTrack = false;
    bool hasVolume = false;
    foreach(VRCExpressionParameters.Parameter parameter in parameters) {
     if(parameter.name==_8TrackEditor.VolumeEPName)
      hasVolume=true;
     else if(parameter.name==_8TrackEditor.TrackEPName)
      hasTrack=true;
    }

    if(!hasVolume)
     ArrayUtility.Add<VRCExpressionParameters.Parameter>(ref expressionParameters.parameters, volumeParam);
    if(!hasTrack)
     ArrayUtility.Add<VRCExpressionParameters.Parameter>(ref expressionParameters.parameters, trackParam);

    UnityEditor.EditorUtility.SetDirty(expressionParameters);
   }

   //--- --- VRC Expression Menu --- ---
   VRCExpressionsMenu mainMenu = new VRCExpressionsMenu() { name=_8Track._8TrackObject.name+" Menu" };
   mainMenu.controls.Add(new VRCExpressionsMenu.Control() { icon=_8Track.StopIcon, name="Stop", parameter=new VRCExpressionsMenu.Control.Parameter() { name=_8Track.TrackEPName }, type=VRCExpressionsMenu.Control.ControlType.Toggle, value=0 });
   mainMenu.controls.Add(new VRCExpressionsMenu.Control() { icon=_8Track.VolumeIcon, name="Volume", subParameters=new VRCExpressionsMenu.Control.Parameter[] { new VRCExpressionsMenu.Control.Parameter() { name=_8Track.VolumeEPName } }, type=VRCExpressionsMenu.Control.ControlType.RadialPuppet });

   VRCExpressionsMenu disk = new VRCExpressionsMenu() { name="Disk "+1 };
   mainMenu.controls.Add(new VRCExpressionsMenu.Control() { icon=_8Track.DiskIcon, name=disk.name, type=VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu=disk });

   int menuCount = 3;
   VRCExpressionsMenu menu = mainMenu;
   List<VRCExpressionsMenu> menus = new List<VRCExpressionsMenu>();

   int diskCount = 0;
   List<VRCExpressionsMenu> disks = new List<VRCExpressionsMenu>
   {
        disk
      };

   int trackNumber = 0;
   do {
    if(libAssets.Count<=trackNumber) {
     break;
    }

    string trackName = libAssets[trackNumber].name;
    trackNumber++;

    if(diskCount==8) {
     VRCExpressionsMenu newDisk = new VRCExpressionsMenu() { name="Disk "+(disks.Count+1) };

     if(7==menuCount&&0<libAssets.Count-(trackNumber)) {
      VRCExpressionsMenu newMenu = new VRCExpressionsMenu() { name="More..." };
      menu.controls.Add(new VRCExpressionsMenu.Control() { icon=_8Track.FolderIcon, name=newMenu.name, type=VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu=newMenu });
      menus.Add(newMenu);
      menu=newMenu;
      menuCount=0;
     }
     menu.controls.Add(new VRCExpressionsMenu.Control() { icon=_8Track.DiskIcon, name=newDisk.name, type=VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu=newDisk });
     menuCount++;
     disks.Add(newDisk);
     disk=newDisk;
     diskCount=0;
    }

    disk.controls.Add(new VRCExpressionsMenu.Control() { icon=_8Track.TrackIcon, name=trackName, parameter=new VRCExpressionsMenu.Control.Parameter() { name=_8Track.TrackEPName }, type=VRCExpressionsMenu.Control.ControlType.Toggle, value=trackNumber });
    diskCount++;
   } while(true);

   foreach(VRCExpressionsMenu d in disks) {
    CreateAsset(_8Track.AudioClip, d, d.name+".asset");
    EditorUtility.SetDirty(d);
   }

   CreateAsset(_8Track.AudioClip, mainMenu, mainMenu.name+".asset");
   EditorUtility.SetDirty(mainMenu);
   for(int i = 0; i<menus.Count; i++) {
    CreateAsset(_8TrackEditor.AudioClip, menus[i], _8Track._8TrackObject.name+" Menu "+i+".asset");
   }

   if(null!=expressionsMenu) {
    VRCExpressionsMenu.Control toggle = GetMenuControl();
    expressionsMenu.controls.Remove(toggle);
    expressionsMenu.controls.Add(new VRCExpressionsMenu.Control() { icon=_8Track.FolderIcon, name=_8TrackEditor._8TrackObject.name, type=VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu=mainMenu });
    EditorUtility.SetDirty(expressionsMenu);
   }
   EditorUtility.SetDirty(expressionParameters);
   AssetDatabase.SaveAssets();
   return true;
  }

  private VRCExpressionsMenu.Control GetMenuControl() {
   foreach(VRCExpressionsMenu.Control control in expressionsMenu.controls) {
    if(control.name==_8TrackEditor._8TrackObject.name) {
     return control;
    }
   }
   return null;
  }

  internal static VRCAvatarDescriptor GetFirstAvatar() {
   return GetFirstAvatar(SceneManager.GetActiveScene());
  }

  internal static VRCAvatarDescriptor GetFirstAvatar(Scene scene) {
   GameObject[] rootObjects = scene.GetRootGameObjects();
   foreach(GameObject root in rootObjects) {
    VRCAvatarDescriptor avatar = root.GetComponent<VRCAvatarDescriptor>();
    if(null!=avatar) {
     return avatar;
    }
   }
   return null;
  }

  internal static Dictionary<string, VRCAvatarDescriptor> GetAvatars() {
   Dictionary<string, VRCAvatarDescriptor> avatars = new Dictionary<string, VRCAvatarDescriptor>();
   int sceneCount = SceneManager.sceneCount;
   for(int sc = 0; sc<sceneCount; sc++) {
    Scene scene = SceneManager.GetSceneAt(sc);
    if(!scene.isLoaded)
     continue;
    GameObject[] rootObjects = scene.GetRootGameObjects();
    foreach(GameObject root in rootObjects) {
     VRCAvatarDescriptor avatar = root.GetComponent<VRCAvatarDescriptor>();
     if(null!=avatar) {
      avatars.Add(TEA_Manager.GetSceneAvatarKey(scene, avatar), avatar);
     }
    }
   }
   return avatars;
  }

  internal static string GetVarName(UnityEngine.Object obj, string varSuffix) {
   if(null!=obj) {
    return obj.name+varSuffix;
   }
   return null;
  }

  internal static void CreateAsset(UnityEngine.Object sibling, UnityEngine.Object newAsset, string name) {
   AssetDatabase.CreateAsset(newAsset, GetAssetDirectory(sibling, true)+name);
  }

  internal static string GetAssetDirectory(UnityEngine.Object obj, bool keepSlash) {
   if(keepSlash)
    return Regex.Replace(AssetDatabase.GetAssetPath(obj), @"[^/]+\..*$", "");
   else
    return Regex.Replace(AssetDatabase.GetAssetPath(obj), @"/[^/]+\..*$", "");
  }

  public static void CopyComponent(Component source, Component target) {
   if(null==source||null==target||source.GetType()!=target.GetType())
    return;

   foreach(FieldInfo field in source.GetType().GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Default)) {
    try {
     field.SetValue(target, field.GetValue(source));
    } catch(System.Exception e) {
     Debug.Log(e.Message);
    }
   }

   foreach(PropertyInfo property in source.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Default)) {
    try {

     if(null!=property.SetMethod&&null==property.GetCustomAttribute<System.ObsoleteAttribute>())
      property.SetValue(target, property.GetValue(source));
    } catch(System.Exception e) {
     Debug.Log(e.Message);
    }
   }
  }
 }
}