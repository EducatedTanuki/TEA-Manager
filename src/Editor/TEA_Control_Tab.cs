using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using static TEA.TEA_Utility;
using TEA.ScriptableObject;
using System;

namespace TEA {
 public class TEA_Control_Tab : EditorWindow {
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
   EditorWindow window = EditorWindow.GetWindow(typeof(TEA_Control_Tab), false, "TEA Manager", true);
   window.minSize=new Vector2(500, MIN_HEIGHT+5);
  }

  [MenuItem("Window/TEA Manager/Play Tab")]
  static void AddTab() {
   EditorWindow window = EditorWindow.GetWindow(typeof(TEA_Control_Tab), false, "TEA Manager", true);
   window.minSize=new Vector2(500, MIN_HEIGHT);
  }

  // ----- ----- Instance ----- -----
  TEA_Manager manager;
  GameObject prefabObject;

  //--- Avatar ---
  public int avatarIndex = 0;

  // --- state bools
  bool _avatars = false;
  bool _play = false;
  bool _compile = false;
  bool _managerOverload = false;
  bool _startedPlaying = false;
  bool _stoppedPlaying = false;

  // --- Compiler
  private TEA_Compiler compiler = new TEA_Compiler();

  // ----- ----- Toggles ----- -----
  TEA_Settings settings;

  // --- gui ---
  Texture2D play;
  Texture2D stop;
  Texture2D canvasTex;
  Texture2D stage;
  Texture2D center;
  Texture2D validation;

  // --- toggle objects
  GameObject canvasObj;
  GameObject stageObj;
  GameObject worldCenterObj;
  GameObject audioListenerObj;
  GameObject lightObj;

  // --- layout 
  bool _allLayout = true;
  bool _beforeToggles = true;
  bool _afterToggles = true;
  bool _beforeButtons = true;
  bool _afterButtons = true;
  bool _beforeInfo = true;
  bool _afterInfo = true;

  // --- styles
  GUIStyle layoutStyle;
  GUIStyle sectionStyle;
  RectOffset padding;
  GUIStyle imageStyle;

  // ----- ----- Methods ----- -----
  private void init() {
   prefabObject=EditorGUIUtility.Load(PREFAB) as GameObject;

   padding=new RectOffset(0, 0, 0, 0);

   play=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/play.png") as Texture2D;
   stop=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/stop.png") as Texture2D;
   canvasTex=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/TEA.png") as Texture2D;
   stage=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/floor.png") as Texture2D;
   center=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/center.png") as Texture2D;
   validation=EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/validation.png") as Texture2D;

   if(null==settings) {
    var assets = AssetDatabase.FindAssets("t:TEA_Settings");
    if(null==assets||assets.Length==0) {
     Debug.Log("Creating setting file at Assets/");
     string settingsPath = CreatePath(false, "Assets", "TEA_Settings.asset");
     AssetDatabase.CreateAsset(TEA_Settings.CreateInstance<TEA_Settings>(), settingsPath);
     settings=AssetDatabase.LoadAssetAtPath<TEA_Settings>(settingsPath);
    } else if(assets.Length>1) {
     bool delete = EditorUtility.DisplayDialog("TEA Settings", "there are more than one setting file present.\nUsing the first found", "Delete Extra", "Continue");
     if(delete) {
      for(int i = 1; i<assets.Length; i++) {
       AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(assets[i]));
      }
     }
    } else {
     settings=AssetDatabase.LoadAssetAtPath<TEA_Settings>(AssetDatabase.GUIDToAssetPath(assets[0]));
    }
   }
  }

  private void OnGUI() {
   if(null==settings)
    return;

   try {
    imageStyle=new GUIStyle() {
     alignment=TextAnchor.UpperLeft
    };
    if(null==layoutStyle) {
     layoutStyle=new GUIStyle(EditorStyles.boldLabel) {
      alignment=TextAnchor.MiddleCenter,
      fixedHeight=MIN_HEIGHT
     };
    }
    if(null==sectionStyle) {
     sectionStyle=new GUIStyle(EditorStyles.helpBox) {
      padding=this.padding
     };
    }

    EditorGUILayout.BeginHorizontal();
    //------
    if(_managerOverload) {
     EditorGUILayout.HelpBox($"Only one TEA Manager can be loaded at a time, please delete one form the Active Scene", MessageType.Error);
     EndLayout();
     return;
    }

    DrawToggleParent();
    EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH), GUILayout.Height(MIN_HEIGHT));

    if(!_avatars) {
     DrawButtonParent(false);
     DrawInfoParent(DrawNoAvatars, SECTION_WIDTH*2);
     //TODO add list of potential avatar?
     EndLayout();
     return;
    } else if(null==manager) {
     DrawButtonParent(true);
     DrawInfoParent(DrawNoManagers, SECTION_WIDTH*2);
     EndLayout();
     return;
    }

    if(null!=manager&&!EditorApplication.isPlaying&&PrefabUtility.IsAnyPrefabInstanceRoot(manager.gameObject)) {
     PrefabUtility.UnpackPrefabInstance(manager.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
     Debug.Log("Automatically unpacked TEA Manager");
    }

    // --- buttons
    DrawButtonParent(true);

    if(EditorApplication.isPlaying)
     DrawInfoParent(DrawSelector, SECTION_WIDTH);
    else
     DrawInfoParent(DrawWaiting, SECTION_WIDTH);


    //----------------
    EndLayout();

    CleanLeanTween();
   } catch(System.Exception ex) {
    Debug.LogException(new System.Exception("Play Tab ran into an unexpected issue", ex));
   }
  }

  // --- --- --- Update --- --- ---
  bool _patched = false;
  private void Update() {
   if(null==settings)
    init();

   AvatarUtilities();
   ManagerControls();
  }

  private void ManagerControls() {
   _avatars=null!=HasAvatars(SceneManager.GetActiveScene());

   // --- managers
   _managerOverload=ManagerSetup(EditorApplication.isPlaying, _play, _compile);

   // --- Avatar
   Dictionary<string, VRCAvatarDescriptor> newAvatars;
   if(null!=manager&&!EditorApplication.isPlaying&&_avatars&&(_play||_compile)) {
    avatarIndex=0;

    manager.Controllers=new List<RuntimeAnimatorController>();
    manager.LayerInfo=new List<TEA_PlayableLayerData>();
    manager.ViewPorts=new List<Vector3>();

    newAvatars=GetAvatars(out bool crossScene);
    manager.Avatars=newAvatars.Keys.ToList<string>();
    TEA_Manager.AvatarDescriptor=newAvatars.Values.ToList<VRCAvatarDescriptor>();

    manager.SetupComponents(0);
    _patched=false;
   } else if(null!=manager&&EditorApplication.isPlaying&&!_patched) {
    newAvatars=GetAvatars(out bool crossScene);
    TEA_Manager.AvatarDescriptor=newAvatars.Values.ToList<VRCAvatarDescriptor>();
    if(manager.Avatar==null)
     manager.Initialize(0);
    _patched=true;
   }

   // --- button presses
   if(!EditorApplication.isPlaying) {
    bool valid = true;
    if(_play||_compile) {
     manager.gameObject.SetActive(false);
     bool issues = compiler.CompileAnimators(manager);
     if(!_play&&!issues)
      _play=EditorUtility.DisplayDialog($"Compilation", "Avatars Compiled", "Play", "Continue");
     manager.gameObject.SetActive(!(!settings.keep_in_scene&&!_play));
     _compile=false;
    }
    if(_play) {
     if(!compiler.validate||valid) {
      manager.Canvas.SetActive(true);
      EditorApplication.isPlaying=true;
     }
     _play=false;
    }
   }
  }

  private void AvatarUtilities() {
   if(EditorApplication.isPlaying||_play||_compile)
    return;

   //TODO set viewport on avatar move
  }

  // --- --- --- GUI Util Methods --- --- ---
  // --- --- Controls
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

  // --- --- End Layout
  private void EndLayout() {
   EditorGUILayout.EndHorizontal();
   LayoutControls();
  }

  // --- --- Button Section
  private void DrawButtonParent(bool render) {
   if(_beforeButtons)
    GUILayout.FlexibleSpace();

   EditorGUILayout.BeginHorizontal(sectionStyle, GUILayout.Width(BUTTON_WIDTH*3), GUILayout.Height(MIN_HEIGHT));
   GUILayout.FlexibleSpace();

   if(render)
    DrawButtons();

   GUILayout.FlexibleSpace();
   EditorGUILayout.EndHorizontal();

   if(_afterButtons)
    GUILayout.FlexibleSpace();
   EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH), GUILayout.Height(MIN_HEIGHT));
  }

  private void DrawButtons() {
   if(!EditorApplication.isPlaying) {
    _play=GUILayout.Button(play, compiler.validate ? EditorStyles.miniButtonLeft : EditorStyles.miniButton, GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(BUTTON_WIDTH), GUILayout.ExpandWidth(false));
    if(compiler.validate)
     _compile=GUILayout.Button(new GUIContent(validation, "Validate playable layers adhere to SDK standards; Validate all Expression Parameters are used"), EditorStyles.miniButtonRight, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(BUTTON_WIDTH), GUILayout.ExpandWidth(false));
   } else if(GUILayout.Button(stop, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(BUTTON_WIDTH), GUILayout.ExpandWidth(false))) {
    EditorApplication.isPlaying=false;
   }
  }

  // --- --- Toggle Section
  private void SetToggleObjects() {
   if(null==manager) {
    canvasObj=null;
    stageObj=null;
    worldCenterObj=null;
    audioListenerObj=null;
    lightObj=null;
   } else {
    canvasObj=manager.Canvas;
    stageObj=manager.Stage;
    worldCenterObj=manager.WorldCenter;
    audioListenerObj=manager.AudioListener;
    lightObj=manager.Light;
   }
  }

  private void DrawToggleParent() {
   if(_beforeToggles)
    GUILayout.FlexibleSpace();

   EditorGUILayout.BeginHorizontal(sectionStyle, GUILayout.MaxWidth(7*TOGGLE_WIDTH));

   DrawAllToggles();

   EditorGUILayout.EndHorizontal();

   if(_afterToggles)
    GUILayout.FlexibleSpace();
  }

  private void DrawAllToggles() {
   SetToggleObjects();
   settings.keep_in_scene=DrawToggle(settings.keep_in_scene, EditorGUIUtility.IconContent("d_Prefab Icon").image, "Keep the TEA Manager prefab in your Scene while not in play mode");

   string toolTip = "TEA Canvas ON-OFF, will activate when you play";
   if(null!=canvasObj&&EditorApplication.isPlaying) {
    canvasObj.SetActive(true);
    GUILayout.Box(new GUIContent(canvasTex, toolTip), new GUIStyle() { alignment=TextAnchor.MiddleCenter }, GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(TOGGLE_WIDTH), GUILayout.ExpandWidth(false));
   } else
    settings._canvas=DrawObjectToggle(settings._canvas, canvasObj, canvasTex, toolTip);

   settings._stage=DrawObjectToggle(settings._stage, stageObj, stage, "Stage ON-OFF");
   settings._worldCenter=DrawObjectToggle(settings._worldCenter, worldCenterObj, center, "World Center ON-OFF");
   settings._audioListener=DrawObjectToggle(settings._audioListener, audioListenerObj, EditorGUIUtility.IconContent("AudioListener Icon").image, "Audio Listener ON-OFF");
   settings._light=DrawObjectToggle(settings._light, lightObj, EditorGUIUtility.IconContent("DirectionalLight Gizmo").image, "Directional Light ON-OFF");
   compiler.validate=DrawToggle(compiler.validate, validation, "turn off validation");

   EditorUtility.SetDirty(settings);
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

  // --- --- Info Section
  private delegate void DrawInfoContent();

  private void DrawInfoParent(DrawInfoContent drawContent, int width) {
   if(_beforeInfo)
    GUILayout.FlexibleSpace();

   EditorGUILayout.BeginHorizontal(GUILayout.Width(SECTION_WIDTH*2.5f));

   EditorGUILayout.BeginHorizontal(sectionStyle, GUILayout.Width(width));

   drawContent();

   EditorGUILayout.EndHorizontal();

   GUILayout.FlexibleSpace();
   EditorGUILayout.EndHorizontal();
   if(_afterInfo)
    GUILayout.FlexibleSpace();
  }

  private void DrawNoManagers() {
   GUILayout.Box(EditorGUIUtility.IconContent("d_Prefab Icon"), imageStyle, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
   EditorGUILayout.LabelField("TEA Manager will be added on Play", GUILayout.Height(MIN_HEIGHT), GUILayout.MinWidth(SECTION_WIDTH), GUILayout.ExpandWidth(true));
  }

  private void DrawNoAvatars() {
   GUILayout.Box(EditorGUIUtility.IconContent("sv_icon_dot12_pix16_gizmo"), imageStyle, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
   EditorGUILayout.LabelField("There are no Avatars in the active Scene", GUILayout.Height(MIN_HEIGHT), GUILayout.MinWidth(SECTION_WIDTH), GUILayout.ExpandWidth(true));
  }

  private void DrawWaiting() {
   GUILayout.Box(EditorGUIUtility.IconContent("sv_icon_dot11_pix16_gizmo"), imageStyle, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
   EditorGUILayout.LabelField("Ready for play or validation", GUILayout.Height(MIN_HEIGHT), GUILayout.MinWidth(SECTION_WIDTH), GUILayout.ExpandWidth(true));
  }

  private void DrawSelector() {
   EditorGUI.BeginChangeCheck();
   avatarIndex=EditorGUILayout.Popup("", avatarIndex, manager.Avatars.ToArray(), EditorStyles.popup, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(SECTION_WIDTH+50), GUILayout.ExpandWidth(false));
   if(EditorGUI.EndChangeCheck()) {
    if(EditorApplication.isPlaying) {
     Debug.Log($"index selected {avatarIndex}");
     manager.Initialize(avatarIndex);
    }
   }
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

  public bool ManagerSetup(bool play, bool _play, bool _compile) {
   List<TEA_Manager> managers = new List<TEA_Manager>();
   manager=null;
   int activeCount = 0;
   bool destroy = (!play||!_play||!_compile)&&!settings.keep_in_scene;
   for(int i = 0; i<SceneManager.sceneCount; i++) {
    Scene scene = SceneManager.GetSceneAt(i);
    if(!scene.isLoaded)
     continue;
    int count = 0;

    foreach(GameObject obj in scene.GetRootGameObjects()) {
     Component comp = obj.GetComponentInChildren(typeof(TEA_Manager), true);
     if(null!=comp) {
      count++;
      TEA_Manager manager = (TEA_Manager)comp;

      if(!play&&(count>1||destroy||scene!=SceneManager.GetActiveScene()))
       DestroyImmediate(manager.gameObject);
      else if(scene==SceneManager.GetActiveScene()) {
       this.manager=manager;
       activeCount++;
      }
     }//exists
    }//for obj

   }// for scene

   // add managers
   if(null==manager&&_avatars&&(_compile||_play||play||settings.keep_in_scene)) {
    TEA_Manager newManager = Instantiate(prefabObject).GetComponent<TEA_Manager>();
    this.manager=newManager;
   }
   return activeCount>1;
  }
 }//class
}//namespace
