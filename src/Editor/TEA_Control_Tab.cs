using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using static TEA.TEA_EditorUtility;
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
	private static readonly int LABEL_WIDTH = 70;

	[MenuItem("TEA Manager/Play Tab", false, 0)]
	static void OpenWindow() {
	 EditorWindow window = EditorWindow.GetWindow(typeof(TEA_Control_Tab), false, "TEA Manager", true);
	 window.minSize = new Vector2(500, MIN_HEIGHT + 5);
	}

	[MenuItem("Window/TEA Manager/Play Tab")]
	static void AddTab() {
	 EditorWindow window = EditorWindow.GetWindow(typeof(TEA_Control_Tab), false, "TEA Manager", true);
	 window.minSize = new Vector2(500, MIN_HEIGHT);
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
	bool _compiled = false;
	bool _managerOverload = false;

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

	// --- styles
	GUIStyle layoutStyle;
	GUIStyle sectionStyle;
	RectOffset padding;
	GUIStyle imageStyle;

	// ----- ----- Methods ----- -----
	private void Init() {
	 if(null != settings)
		return;

	 prefabObject = EditorGUIUtility.Load(PREFAB) as GameObject;

	 padding = new RectOffset(0, 0, 0, 0);

	 play = EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/play.png") as Texture2D;
	 stop = EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/stop.png") as Texture2D;
	 canvasTex = EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/TEA.png") as Texture2D;
	 stage = EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/floor.png") as Texture2D;
	 center = EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/center.png") as Texture2D;
	 validation = EditorGUIUtility.Load("Assets/TEA Manager/Resources/UI/Icons/validation.png") as Texture2D;

	 try {
		settings = GetTEA_Settings();
	 } catch(TEA_Exception) {
		this.Close();
	 }
	}

	private void OnInspectorUpdate() {
	 try {
		settings = GetTEA_Settings();
	 } catch(TEA_Exception) {
		this.Close();
	 }
	}

	private void OnGUI() {
	 if(null == settings)
		return;

	 try {
		if(null == imageStyle) {
		 imageStyle = new GUIStyle() {
			alignment = TextAnchor.UpperLeft
		 };
		}
		if(null == layoutStyle) {
		 layoutStyle = new GUIStyle(EditorStyles.boldLabel) {
			alignment = TextAnchor.MiddleCenter,
			fixedHeight = MIN_HEIGHT
		 };
		}
		if(null == sectionStyle) {
		 sectionStyle = new GUIStyle(EditorStyles.helpBox) {
			padding = this.padding
		 };
		}

		EditorGUILayout.BeginHorizontal();
		//------
		if(EditorApplication.isPlaying && !_compiled) {
		 if(null != manager)
			manager.gameObject.SetActive(false);
		 EditorGUILayout.HelpBox("TEA Manager will not activate unless you use the custom play/validate buttons", MessageType.Warning);
		 return;
		}

		if(_managerOverload) {
		 EditorGUILayout.HelpBox($"Only one TEA Manager can be loaded at a time, please delete one form the Active Scene", MessageType.Error);
		 EndLayout();
		 return;
		}

		if(GUILayout.Button("Settings", GUILayout.Width(SECTION_WIDTH / 3)))
		 TEA_Settings_EditorWindow.OpenWindow();

		DrawToggleParent();
		EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH), GUILayout.Height(MIN_HEIGHT));

		if(!_avatars) {
		 DrawButtonParent(false);
		 DrawInfoParent(DrawNoAvatars, SECTION_WIDTH * 2);
		 //TODO add list of potential avatar?
		 EndLayout();
		 return;
		} else if(null == manager) {
		 DrawButtonParent(true);
		 DrawInfoParent(DrawNoManagers, SECTION_WIDTH * 2);
		 EndLayout();
		 return;
		}

		if(null != manager && !EditorApplication.isPlaying && PrefabUtility.IsAnyPrefabInstanceRoot(manager.gameObject)) {
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

		EditorGUILayout.BeginVertical();

		EditorGUILayout.EndVertical();

		CleanLeanTween();
	 } catch(System.Exception ex) {
		Debug.LogException(new System.Exception("Play Tab ran into an unexpected issue", ex));
	 }
	}

	// --- --- --- Update --- --- ---
	bool _patched = false;
	private void Update() {
	 Init();
	 if(null == settings)
		return;

	 try {
		ManagerControls();
		//AvatarUtilities();
	 } catch(TEA_Exception e) {
		_play = false;
		_compile = false;
		_compiled = false;
		if(null != manager)
		 manager.gameObject.SetActive(true);
		Debug.LogError(e);
	 }
	}

	Dictionary<string, VRCAvatarDescriptor> newAvatars;
	private void ManagerControls() {
	 _avatars = null != HasAvatars(SceneManager.GetActiveScene());

	 // --- managers
	 _managerOverload = ManagerSetup(EditorApplication.isPlaying, _play, _compile);

	 // --- Avatar
	 if(!EditorApplication.isPlaying)
		newAvatars = GetAvatars(out bool crossScene);
	 if(null != manager && !EditorApplication.isPlaying && _avatars && (_play || _compile)) {
		avatarIndex = 0;

		manager.Controllers = new List<RuntimeAnimatorController>();
		manager.LayerInfo = new List<TEA_PlayableLayerData>();
		manager.ViewPorts = new List<Vector3>();

		manager.Avatars = newAvatars.Keys.ToList<string>();
		TEA_Manager.AvatarDescriptor = newAvatars.Values.ToList<VRCAvatarDescriptor>();

		manager.SetupComponents(0);
		_patched = false;
	 } else if(null != manager && EditorApplication.isPlaying && !_patched) {
		newAvatars = GetAvatars(out bool crossScene);
		TEA_Manager.AvatarDescriptor = newAvatars.Values.ToList<VRCAvatarDescriptor>();
		if(manager.Avatar == null)
		 manager.Initialize(0);
		manager.Settings = settings;
		manager.GetComponent<AvatarController>().Locomotion = settings;
		_patched = true;
	 }

	 // --- button presses
	 if(!EditorApplication.isPlaying) {
		bool valid = true;
		if(_play || _compile) {
		 manager.gameObject.SetActive(false);
		 valid = compiler.CompileAnimators(manager, settings);
		 if(!_play && valid)
			_play = EditorUtility.DisplayDialog($"Compilation", "Avatars Compiled", "Play", "Continue");
		 manager.gameObject.SetActive(!(!settings.keepInScene && !_play));
		 _compile = false;
		}
		if(_play) {
		 if(!compiler.validate || valid) {
			manager.Canvas.SetActive(true);
			_compiled = true;
			CheckAdditionalCamera();
			EditorApplication.isPlaying = true;
		 }
		 _play = false;
		} else
		 _compiled = false;
	 }
	}

	private void CheckAdditionalCamera() {
	 Dictionary<string, Camera> cameras = GetCameras();
	 string cameraText = "The following Cameras may interfere with TEA Manager:\n";
	 if(cameras.Count == 0)
		return;

	 foreach(KeyValuePair<string, Camera> key in cameras) {
		cameraText += "  ";
		cameraText += key.Key;
		cameraText += "\n";
	 }
	 EditorUtility.DisplayDialog("Camera Conflicts", cameraText, "Continue");
	}

	Dictionary<VRCAvatarDescriptor, Vector3> prevPositions = new Dictionary<VRCAvatarDescriptor, Vector3>();
	private void AvatarUtilities() {
	 if(EditorApplication.isPlaying || _play || _compile)
		return;

	 foreach(KeyValuePair<string, VRCAvatarDescriptor> key in newAvatars) {
		if(key.Value.transform.hasChanged) {
		 if(prevPositions.TryGetValue(key.Value, out Vector3 prevPosition)) {
			key.Value.ViewPosition += (key.Value.transform.position - prevPosition);
		 }
		}
		prevPositions.Remove(key.Value);
		prevPositions.Add(key.Value, key.Value.transform.position);
	 }
	}

	// --- --- --- GUI Util Methods --- --- ---
	// --- --- Controls
	private void LayoutControls() {
	 EditorGUILayout.Space();
	 EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

	 EditorGUILayout.BeginHorizontal(new GUIStyle() {
		alignment = TextAnchor.MiddleCenter
	 });

	 settings.AllLayout = settings.BeforeToggles && settings.AfterToggles && settings.BeforeButtons && settings.AfterButtons && settings.BeforeInfo && settings.AfterInfo;

	 EditorGUILayout.LabelField("All", layoutStyle, GUILayout.Width(LABEL_WIDTH), GUILayout.ExpandWidth(false));
	 EditorGUI.BeginChangeCheck();
	 settings.AllLayout = EditorGUILayout.Toggle("", settings.AllLayout, GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
	 EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH), GUILayout.Height(MIN_HEIGHT));
	 if(EditorGUI.EndChangeCheck()) {
		if(settings.AllLayout) {
		 settings.BeforeToggles = true;
		 settings.BeforeButtons = true;
		 settings.AfterButtons = true;
		 settings.BeforeInfo = true;
		 settings.AfterInfo = true;
		 settings.AfterToggles = true;
		} else {
		 settings.AfterToggles = false;
		 settings.BeforeToggles = false;
		 settings.BeforeButtons = false;
		 settings.AfterButtons = false;
		 settings.BeforeInfo = false;
		 settings.AfterInfo = false;
		}
	 }

	 settings.BeforeToggles = EditorGUILayout.Toggle("", settings.BeforeToggles, GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
	 EditorGUILayout.LabelField("Toggles", layoutStyle, GUILayout.Width(LABEL_WIDTH), GUILayout.ExpandWidth(false));
	 settings.AfterToggles = EditorGUILayout.Toggle("", settings.AfterToggles, GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));

	 EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH), GUILayout.Height(MIN_HEIGHT));
	 settings.BeforeButtons = EditorGUILayout.Toggle("", settings.BeforeButtons, GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
	 EditorGUILayout.LabelField("Buttons", layoutStyle, GUILayout.Width(LABEL_WIDTH), GUILayout.ExpandWidth(false));
	 settings.AfterButtons = EditorGUILayout.Toggle("", settings.AfterButtons, GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
	 EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH), GUILayout.Height(MIN_HEIGHT));

	 settings.BeforeInfo = EditorGUILayout.Toggle("", settings.BeforeInfo, GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
	 EditorGUILayout.LabelField("Info", layoutStyle, GUILayout.Width(LABEL_WIDTH), GUILayout.ExpandWidth(false));
	 settings.AfterInfo = EditorGUILayout.Toggle("", settings.AfterInfo, GUILayout.ExpandWidth(false));

	 EditorGUILayout.EndHorizontal();
	}

	// --- --- End Layout
	private void EndLayout() {
	 EditorGUILayout.EndHorizontal();
	 LayoutControls();
	}

	// --- --- Button Section
	private void DrawButtonParent(bool render) {
	 if(settings.BeforeButtons)
		GUILayout.FlexibleSpace();

	 EditorGUILayout.BeginHorizontal(sectionStyle, GUILayout.Width(BUTTON_WIDTH * 3), GUILayout.Height(MIN_HEIGHT));
	 GUILayout.FlexibleSpace();

	 if(render)
		DrawButtons();

	 GUILayout.FlexibleSpace();
	 EditorGUILayout.EndHorizontal();

	 if(settings.AfterButtons)
		GUILayout.FlexibleSpace();
	 EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(SEPARATOR_WIDTH), GUILayout.Height(MIN_HEIGHT));
	}

	private void DrawButtons() {
	 if(!EditorApplication.isPlaying) {
		_play = GUILayout.Button(play, compiler.validate ? EditorStyles.miniButtonLeft : EditorStyles.miniButton, GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(BUTTON_WIDTH), GUILayout.ExpandWidth(false));
		if(compiler.validate)
		 _compile = GUILayout.Button(new GUIContent(validation, "Validate playable layers adhere to SDK standards; Validate all Expression Parameters are used"), EditorStyles.miniButtonRight, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(BUTTON_WIDTH), GUILayout.ExpandWidth(false));
	 } else if(GUILayout.Button(stop, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(BUTTON_WIDTH), GUILayout.ExpandWidth(false))) {
		EditorApplication.isPlaying = false;
	 }
	}

	// --- --- Toggle Section
	private void SetToggleObjects() {
	 if(null == manager) {
		canvasObj = null;
		stageObj = null;
		worldCenterObj = null;
		audioListenerObj = null;
		lightObj = null;
	 } else {
		canvasObj = manager.Canvas;
		stageObj = manager.Stage;
		worldCenterObj = manager.WorldCenter;
		audioListenerObj = manager.AudioListener;
		lightObj = manager.Light;
	 }
	}

	private void DrawToggleParent() {
	 if(settings.BeforeToggles)
		GUILayout.FlexibleSpace();

	 EditorGUILayout.BeginHorizontal(sectionStyle, GUILayout.MaxWidth(7 * TOGGLE_WIDTH));

	 DrawAllToggles();

	 EditorGUILayout.EndHorizontal();

	 if(settings.AfterToggles)
		GUILayout.FlexibleSpace();
	}

	private void DrawAllToggles() {
	 SetToggleObjects();
	 settings.keepInScene = DrawToggle(settings.keepInScene, EditorGUIUtility.IconContent("d_Prefab Icon").image, settings.keepInSceneTooltip);

	 if(null != canvasObj && EditorApplication.isPlaying) {
		canvasObj.SetActive(true);
		GUILayout.Box(new GUIContent(canvasTex, settings.CanvasTooltip), new GUIStyle() { alignment = TextAnchor.MiddleCenter }, GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(TOGGLE_WIDTH), GUILayout.ExpandWidth(false));
	 } else
		settings.CanvasActive = DrawObjectToggle(settings.CanvasActive, canvasObj, canvasTex, settings.CanvasTooltip);

	 settings.StageActive = DrawObjectToggle(settings.StageActive, stageObj, stage, settings.StageTooltip);
	 settings.WorldCenterActive = DrawObjectToggle(settings.WorldCenterActive, worldCenterObj, center, settings.worldCenterTooltip);
	 settings.AudioListenerActive = DrawObjectToggle(settings.AudioListenerActive, audioListenerObj, EditorGUIUtility.IconContent("AudioListener Icon").image, settings.AudioListenerTooltip);
	 settings.LightActive = DrawObjectToggle(settings.LightActive, lightObj, EditorGUIUtility.IconContent("DirectionalLight Gizmo").image, settings.LighTooltipt);
	 settings.ValidateActive = DrawToggle(settings.ValidateActive, validation, settings.ValidateTooltip);
	 compiler.validate = settings.ValidateActive;

	 EditorUtility.SetDirty(settings);
	}

	private bool DrawObjectToggle(bool val, GameObject obj, Texture tex, string toolTip) {
	 val = GUILayout.Toggle(val, new GUIContent(tex, toolTip), GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(TOGGLE_WIDTH), GUILayout.ExpandWidth(false));
	 if(null != obj)
		obj.SetActive(val);
	 return val;
	}

	private bool DrawToggle(bool val, Texture tex, string toolTip) {
	 return GUILayout.Toggle(val, new GUIContent(tex, toolTip), GUILayout.Height(MIN_HEIGHT), GUILayout.MaxWidth(TOGGLE_WIDTH), GUILayout.ExpandWidth(false));
	}

	// --- --- Info Section
	private delegate void DrawInfoContent();

	private void DrawInfoParent(DrawInfoContent drawContent, int width) {
	 if(settings.BeforeInfo)
		GUILayout.FlexibleSpace();

	 EditorGUILayout.BeginHorizontal(GUILayout.Width(SECTION_WIDTH * 2.5f));

	 EditorGUILayout.BeginHorizontal(sectionStyle, GUILayout.Width(width));

	 drawContent();

	 EditorGUILayout.EndHorizontal();

	 GUILayout.FlexibleSpace();
	 EditorGUILayout.EndHorizontal();
	 if(settings.AfterInfo)
		GUILayout.FlexibleSpace();
	}

	private void DrawNoManagers() {
	 GUILayout.Box(EditorGUIUtility.IconContent("d_Prefab Icon"), imageStyle, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
	 EditorGUILayout.LabelField("TEA Manager will be added on Play", GUILayout.Height(MIN_HEIGHT), GUILayout.MinWidth(SECTION_WIDTH), GUILayout.ExpandWidth(true));
	}

	private void DrawNoAvatars() {
	 GUILayout.Box(EditorGUIUtility.IconContent("sv_icon_dot12_pix16_gizmo"), imageStyle, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
	 EditorGUILayout.LabelField("There are no Avatars in the Active Scene", GUILayout.Height(MIN_HEIGHT), GUILayout.MinWidth(SECTION_WIDTH), GUILayout.ExpandWidth(true));
	}

	private void DrawWaiting() {
	 GUILayout.Box(EditorGUIUtility.IconContent("sv_icon_dot11_pix16_gizmo"), imageStyle, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(MIN_HEIGHT), GUILayout.ExpandWidth(false));
	 EditorGUILayout.LabelField("Ready for play or validation", GUILayout.Height(MIN_HEIGHT), GUILayout.MinWidth(SECTION_WIDTH), GUILayout.ExpandWidth(true));
	}

	private void DrawSelector() {
	 EditorGUI.BeginChangeCheck();
	 avatarIndex = EditorGUILayout.Popup("", avatarIndex, manager.Avatars.ToArray(), EditorStyles.popup, GUILayout.Height(MIN_HEIGHT), GUILayout.Width(SECTION_WIDTH + 50), GUILayout.ExpandWidth(false));
	 if(EditorGUI.EndChangeCheck()) {
		if(EditorApplication.isPlaying) {
		 //Debug.Log($"index selected {avatarIndex}");
		 manager.Initialize(avatarIndex);
		}
	 }
	}

	// --- --- --- Utility --- --- ---
	public static void CleanLeanTween() {
	 List<GameObject> remove = new List<GameObject>();
	 foreach(GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects()) {
		if("~LeanTween" == obj.name) {
		 remove.Add(obj);
		}
	 }

	 if(remove.Count > 0)
		Debug.Log($"Cleaning {remove.Count} ~LeanTween objects");

	 foreach(GameObject obj in remove) {
		DestroyImmediate(obj);
	 }
	}

	public static string GetManagerList(List<TEA_Manager> managers) {
	 string ret = "";
	 foreach(TEA_Manager c in managers) {
		ret += "\n[";
		ret += c.gameObject.scene.name + "/" + c.gameObject.name;
		ret += "]";
	 }
	 return ret;
	}

	public bool ManagerSetup(bool play, bool _play, bool _compile) {
	 List<TEA_Manager> managers = new List<TEA_Manager>();
	 manager = null;
	 int activeCount = 0;
	 bool destroy = (!play || !_play || !_compile) && !settings.keepInScene;
	 for(int i = 0; i < SceneManager.sceneCount; i++) {
		Scene scene = SceneManager.GetSceneAt(i);
		if(!scene.isLoaded)
		 continue;
		int count = 0;

		foreach(GameObject obj in scene.GetRootGameObjects()) {
		 Component comp = obj.GetComponentInChildren(typeof(TEA_Manager), true);
		 if(null != comp) {
			count++;
			TEA_Manager manager = (TEA_Manager)comp;

			if(!play && (count > 1 || destroy || scene != SceneManager.GetActiveScene()))
			 DestroyImmediate(manager.gameObject);
			else if(scene == SceneManager.GetActiveScene()) {
			 this.manager = manager;
			 activeCount++;
			}
		 }//exists
		}//for obj

	 }// for scene

	 // add managers
	 if(null == manager && _avatars && (_compile || _play || play || settings.keepInScene)) {
		TEA_Manager newManager = Instantiate(prefabObject).GetComponent<TEA_Manager>();
		this.manager = newManager;
	 }
	 return activeCount > 1;
	}
 }//class
}//namespace
