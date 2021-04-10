using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace TEA {
 public class TEA_Play_Tab : EditorWindow {
  // ----- ----- Static ----- -----
  public static readonly string PREFAB = "Assets/TEA Manager/TEA Manager.prefab";
  // --- height ---
  private static readonly int MIN_HEIGHT = 20;
  private static readonly int SEPARATOR_WIDTH = 10;
  private static readonly int SECTION_WIDTH = 180;
  private static readonly int TOGGLE_WIDTH = 35;
  private static readonly int BUTTON_WIDTH = 35;
  private static readonly int VALIDATE_BUTTON_WIDTH = 60;
  private static readonly int LABEL_WIDTH = 70;

  [MenuItem("TEA Manager/Play Tab")]
  static void OpenWindow() {
   EditorWindow window = EditorWindow.GetWindow(typeof(TEA_Play_Tab), false, "TEA Manager", true);
   window.minSize=new Vector2(500, MIN_HEIGHT);
  }

  [MenuItem("Window/TEA Manager/Play Tab")]
  static void AddTab() {
   EditorWindow window = EditorWindow.GetWindow(typeof(TEA_Play_Tab), false, "TEA Manager", true);
   window.minSize=new Vector2(500, MIN_HEIGHT);
  }

  // ----- ----- Instance ----- -----
  List<TEA_Manager> managers = new List<TEA_Manager>();
  TEA_Manager manager;

  //--- Avatar ---
  private Dictionary<string, VRCAvatarDescriptor> avatars = new Dictionary<string, VRCAvatarDescriptor>();
  public string[] avatarKeys;
  public int avatarIndex = 0;

  // --- state bools
  bool _avatars = false;
  bool _play = false;
  bool _compile = false;
  bool _managers = false;
  bool _startedPlaying = false;
  bool _stoppedPlaying = false;

  // ----- ----- Compiler ----- -----
  private TEA_Compiler compiler = new TEA_Compiler();

  // --- gui ---
  Texture2D play;
  Texture2D stop;
  Texture2D visible;
  Texture2D stage;
  Texture2D center;
  Texture2D validation;

  // ----- ----- Toggles ----- -----
  // --- state
  bool keep_in_scene = true;
  bool _visibility = true;
  bool _stage = true;
  bool _worldCenter = true;
  bool _audioListener = true;
  bool _light = true;

  // --- toggle objects
  GameObject mainObj;
  GameObject stageObj;
  GameObject worldCenterObj;
  GameObject audioListenerObj;
  GameObject lightObj;

  // --- layout 
  bool _allLayout = true;
  bool _beforeToggles = true;
  bool _afterToggles = true;
  bool _beforeButtons=true;
  bool _afterButtons=true;
  bool _beforeInfo=true;
  bool _afterInfo=true;

  // --- styles
  GUIStyle layoutStyle;
  GUIStyle sectionStyle;
  RectOffset padding;

  // ----- ----- Methods ----- -----
  private void OnEnable() {
   padding=new RectOffset(0, 0, 0, 0);

   play=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/play.png") as Texture2D;
   stop=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/stop.png") as Texture2D;
   visible=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/TEA.png") as Texture2D;
   stage=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/floor.png") as Texture2D;
   center=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/center.png") as Texture2D;
   validation=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/validation.png") as Texture2D;
  }

  private void OnGUI() {
   try {
    if(null==layoutStyle) {
     layoutStyle=new GUIStyle(EditorStyles.boldLabel) {
      alignment=TextAnchor.MiddleCenter
     };
    }
    if(null==sectionStyle) {
     sectionStyle=new GUIStyle(EditorStyles.helpBox) {
      padding=this.padding
     };
    }

    EditorGUILayout.BeginHorizontal();
    //------
    DrawAllToggles();
    EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH), GUILayout.Height(MIN_HEIGHT));

    if(_managers) {
     EditorGUILayout.HelpBox($"Only one TEA Manager can be loaded at a time {GetManagerList(managers)}", MessageType.Error);
     EndLayout();
     return;
    } else if(!_avatars) {
     GUILayout.Box(EditorGUIUtility.IconContent("Collab.Warning"), GUILayout.Height(MIN_HEIGHT), GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
     EditorGUILayout.LabelField("There are no Avatars in the active Scene", GUILayout.Height(MIN_HEIGHT), GUILayout.MinWidth(SECTION_WIDTH), GUILayout.ExpandWidth(true));
     //TODO add list of potential avatar
     EndLayout();
     return;
    } else if(0==managers.Count) {
     DrawButtons();
     EndLayout();
     return;
    }

    manager=managers[0];
    if(!EditorApplication.isPlaying&&PrefabUtility.IsAnyPrefabInstanceRoot(manager.gameObject)) {
     PrefabUtility.UnpackPrefabInstance(manager.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
     Debug.Log("Automatically unpacked TEA Manager");
    }

    if(avatars.Count>0&&(null==manager.Avatar||(avatarIndex>0&&avatars[avatarKeys[avatarIndex]]!=manager.Avatar))) {
     avatarIndex=0;
     manager.SetupComponents(avatars[avatarKeys[avatarIndex]]);
    }

    // --- buttons
    DrawButtons();
    EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH), GUILayout.Height(MIN_HEIGHT));

    if(_beforeInfo)
     GUILayout.FlexibleSpace();
    EditorGUI.BeginChangeCheck();
    avatarIndex=EditorGUILayout.Popup("", avatarIndex, avatarKeys, EditorStyles.popup, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(SECTION_WIDTH+50), GUILayout.ExpandWidth(false));
    if(EditorGUI.EndChangeCheck()) {
     manager.SetupComponents(avatars[avatarKeys[avatarIndex]]);
    }

    //----------------
    EndLayout();

    CleanLeanTween();
   } catch(MissingReferenceException ex) {
    // prevent issues when stopping play mode
    //Debug.Log($"Ignoring: {ex.Message} ");
   }
  }

  private void LayoutControls() {
   EditorGUILayout.Space();
   EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

   EditorGUILayout.BeginHorizontal(new GUIStyle() {
    alignment=TextAnchor.MiddleCenter
   });

   _allLayout=_beforeToggles&&_afterToggles&&_beforeButtons&&_afterButtons&&_beforeInfo&&_afterInfo;

   EditorGUILayout.LabelField("All", layoutStyle, GUILayout.Width(LABEL_WIDTH), GUILayout.ExpandWidth(false));
   EditorGUI.BeginChangeCheck();
   _allLayout=EditorGUILayout.Toggle("", _allLayout, GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
   EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH), GUILayout.Height(MIN_HEIGHT));
   if(EditorGUI.EndChangeCheck()) {
    if(_allLayout) {
     _beforeToggles=true;
     _beforeButtons=true;
     _afterButtons=true;
     _beforeInfo=true;
     _afterInfo=true;
     _afterToggles=true;
    } else {
     _afterToggles=false;
     _beforeToggles=false;
     _beforeButtons=false;
     _afterButtons=false;
     _beforeInfo=false;
     _afterInfo=false;
    }
   }

   _beforeToggles=EditorGUILayout.Toggle("", _beforeToggles, GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
   EditorGUILayout.LabelField("Toggles", layoutStyle, GUILayout.Width(LABEL_WIDTH), GUILayout.ExpandWidth(false));
   _afterToggles=EditorGUILayout.Toggle("", _afterToggles, GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));

   EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH), GUILayout.Height(MIN_HEIGHT));
   _beforeButtons=EditorGUILayout.Toggle("", _beforeButtons, GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
   EditorGUILayout.LabelField("Buttons", layoutStyle, GUILayout.Width(LABEL_WIDTH), GUILayout.ExpandWidth(false));
   _afterButtons=EditorGUILayout.Toggle("", _afterButtons, GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
   EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH), GUILayout.Height(MIN_HEIGHT));

   _beforeInfo=EditorGUILayout.Toggle("", _beforeInfo, GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
   EditorGUILayout.LabelField("Info", layoutStyle, GUILayout.Width(LABEL_WIDTH), GUILayout.ExpandWidth(false));
   _afterInfo=EditorGUILayout.Toggle("", _afterInfo, GUILayout.ExpandWidth(false));

   EditorGUILayout.EndHorizontal();
  }

  private void EndLayout() {
   if(_afterInfo)
    GUILayout.FlexibleSpace();
   EditorGUILayout.EndHorizontal();
   LayoutControls();
  }

  private void AddOrDestroy(bool play) {
   if(_avatars&&managers.Count==0&&null==manager&&(play||(!play&&keep_in_scene))) {
    GameObject newManager = EditorGUIUtility.Load(PREFAB) as GameObject;
    manager=Instantiate(newManager).GetComponent<TEA_Manager>();
    manager.SetupComponents(avatars[avatarKeys[0]]);
   }
   if(!play&&!keep_in_scene&&null!=manager) {
    DestroyImmediate(managers[0].gameObject);
    managers.Clear();
    Debug.LogWarning("Destroyed TEA Manager");
   }
  }

  // --- --- --- Update --- --- ---
  private void Update() {
   // --- managers
   managers=GetManagers();
   _managers=managers.Count>1;

   // --- avatars
   Dictionary<string, VRCAvatarDescriptor> newAvatars = AvatarController.GetAvatars(SceneManager.GetActiveScene());
   avatarKeys=newAvatars.Keys.ToArray();
   if(avatars.Count!=newAvatars.Count) {
    avatarIndex=0;
    if(null!=manager) {
     if(newAvatars.Count>0)
      manager.SetupComponents(newAvatars[avatarKeys[avatarIndex]]);
     else
      manager.Avatar=null;
    }
   } else {
    foreach(KeyValuePair<string, VRCAvatarDescriptor> key in avatars) {
     if(!(newAvatars.TryGetValue(key.Key, out VRCAvatarDescriptor value)&&value==key.Value)) {
      avatarIndex=0;
      if(null!=manager) {
       if(newAvatars.Count>0)
        manager.SetupComponents(newAvatars[avatarKeys[avatarIndex]]);
       else
        manager.Avatar=null;
      }
      break;
     }
    }
   }
   avatars=newAvatars;
   _avatars=avatars.Count!=0;

   // --- button presses
   AddOrDestroy(EditorApplication.isPlaying);
   if(!EditorApplication.isPlaying) {
    AddOrDestroy(_play||_compile);
    if(_play||_compile) {
     manager.gameObject.SetActive(false);
     compiler.CompileAnimators(avatars, manager);
     if(!_play&&!compiler.validationIssue)
      _play=EditorUtility.DisplayDialog($"{avatarKeys[avatarIndex]}", "Avatar Compiled", "Play", "Continue");
     manager.gameObject.SetActive(!(!keep_in_scene&&!_play));
     _compile=false;
    }
    if(_play) {
     if(!compiler.validate||!compiler.validationIssue) {
      EditorApplication.isPlaying=true;
     }
     _play=false;
    }
   }
  }

  // --- --- --- GUI Util Methods --- --- ---
  private void DrawButtons() {
   if(_beforeButtons)
    GUILayout.FlexibleSpace();

   EditorGUILayout.BeginHorizontal(sectionStyle,  GUILayout.Width(SECTION_WIDTH));
   GUILayout.FlexibleSpace();

   if(!EditorApplication.isPlaying) {
    _play=GUILayout.Button(play, compiler.validate ? EditorStyles.miniButtonLeft : EditorStyles.miniButton, GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(BUTTON_WIDTH), GUILayout.ExpandWidth(false));
    if(compiler.validate)
     _compile=GUILayout.Button(new GUIContent(validation, "Validate playable layers adhere to SDK standards; Validate all Expression Parameters are used"), EditorStyles.miniButtonRight, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(BUTTON_WIDTH), GUILayout.ExpandWidth(false));
   } else if(GUILayout.Button(stop, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(BUTTON_WIDTH), GUILayout.ExpandWidth(false))) {
    EditorApplication.isPlaying=false;
   }

   GUILayout.FlexibleSpace();
   EditorGUILayout.EndHorizontal();

   if(_afterButtons)
    GUILayout.FlexibleSpace();
  }

  private void SetToggleObjects() {
   if(null==manager) {
    mainObj=null;
    stageObj=null;
    worldCenterObj=null;
    audioListenerObj=null;
    lightObj=null;
   } else {
    mainObj=manager.gameObject;
    stageObj=manager.Stage;
    worldCenterObj=manager.WorldCenter;
    audioListenerObj=manager.AudioListener;
    lightObj=manager.Light;
   }
  }

  private void DrawAllToggles() {
   if(_beforeToggles)
    GUILayout.FlexibleSpace();

   SetToggleObjects();
   keep_in_scene=DrawToggle(keep_in_scene, EditorGUIUtility.IconContent("d_Prefab Icon").image, "Keep the TEA Manager prefab in your Scene while not in play mode");
   _visibility=DrawObjectToggle(_visibility, mainObj, visible, "TEA Manager ON-OFF, will activate when you play");
   _stage=DrawObjectToggle(_stage, stageObj, stage, "Stage ON-OFF");
   _worldCenter=DrawObjectToggle(_worldCenter, worldCenterObj, center, "World Center ON-OFF");
   _audioListener=DrawObjectToggle(_audioListener, audioListenerObj, EditorGUIUtility.IconContent("AudioListener Icon").image, "Audio Listener ON-OFF");
   _light=DrawObjectToggle(_light, lightObj, EditorGUIUtility.IconContent("DirectionalLight Gizmo").image, "Directional Light ON-OFF");
   compiler.validate=DrawToggle(compiler.validate, validation, "turn off validation");

   if(_afterToggles)
    GUILayout.FlexibleSpace();
  }

  private bool DrawObjectToggle(bool val, GameObject obj, Texture tex, string toolTip) {
   val=GUILayout.Toggle(val, new GUIContent(tex, toolTip), GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(TOGGLE_WIDTH), GUILayout.ExpandWidth(false));
   if(null!=obj)
    obj.SetActive(val);
   return val;
  }

  private bool DrawToggle(bool val, Texture tex, string toolTip) {
   return GUILayout.Toggle(val, new GUIContent(tex, toolTip), GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(TOGGLE_WIDTH), GUILayout.ExpandWidth(false));
  }

  // --- --- --- Utility --- --- ---
  public static void CleanLeanTween() {
   List<GameObject> remove = new List<GameObject>();
   foreach(GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects()) {
    if("~LeanTween"==obj.name) {
     remove.Add(obj);
    }
   }

   if(remove.Count>0)
    Debug.Log($"Cleaning {remove.Count} ~LeanTween objects");

   foreach(GameObject obj in remove) {
    DestroyImmediate(obj);
   }
  }

  public static string GetManagerList(List<TEA_Manager> managers) {
   string ret = "";
   foreach(TEA_Manager c in managers) {
    ret+="\n[";
    ret+=c.gameObject.scene.name+"/"+c.gameObject.name;
    ret+="]";
   }
   return ret;
  }

  public static List<TEA_Manager> GetManagers() {
   List<TEA_Manager> managers = new List<TEA_Manager>();
   for(int i = 0; i<SceneManager.sceneCount; i++) {
    Scene scene = SceneManager.GetSceneAt(i);
    if(!scene.isLoaded)
     continue;
    foreach(GameObject obj in scene.GetRootGameObjects()) {
     Component comp = obj.GetComponentInChildren(typeof(TEA_Manager), true);
     if(null!=comp) {
      managers.Add((TEA_Manager)comp);
     }
    }
   }
   return managers;
  }
 }//class
}//namespace
