using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System;

namespace TEA.UI {
 public class RadialMenuController : UIBehaviour {

  public static RadialMenuController current;

  protected override void Awake() {
   current=this;
  }

  protected override void Start() {
   if(null!=root) {
    Destroy(root.gameObject);
    root=null;
    foreach(GameObject obj in subMenus) {
     Destroy(obj);
    }
    subMenus=new List<GameObject>();
   }

   if(null!=TEA_Manager.current.Avatar) {
    base.Start();
    mainMenu=TEA_Manager.current.Avatar.expressionsMenu;
    parameters=TEA_Manager.current.Avatar.expressionParameters;

    if(null==mainMenu||null==parameters) {
     //TODO warn of no menu or use default
     Debug.Log($"Expression Menu [{null!=mainMenu}] or Parameters [{null!=parameters}]");
     return;
    }
    root=CreateMainMenu(mainMenu, parameters);
    root.gameObject.SetActive(true);
    RadialPuppet.transform.SetAsLastSibling();
    MultiAxisPuppet.transform.SetAsLastSibling();
    RadialButtonEvent+=OnRadialButtonEvent;
   }

   if(!TEAManagerUpdateRegistered) {
    TEA_Manager.current.TEAManagerEvent+=OnTEAManagerUpdate;
    TEAManagerUpdateRegistered=true;
   }
  }

  private bool TEAManagerUpdateRegistered = false;
  public void OnTEAManagerUpdate(TEA_Manager tea_manager) {
   if(IsDestroyed()) {
    TEA_Manager.current.TEAManagerEvent-=OnTEAManagerUpdate;
    return;
   }
   Start();
  }

  public VRCExpressionsMenu mainMenu;
  public VRCExpressionParameters parameters;

  private RadialMenu root;
  private List<GameObject> subMenus = new List<GameObject>();

  public Texture2D HomeIcon;
  public Texture2D BackIcon;

  public Texture2D SubMenuIcon;
  public Texture2D ToggleIcon;

  public Texture2D ButtonIcon;
  public Sprite ErrorSprite;
  public Texture2D RadialPuppetIcon;
  public Texture2D TwoAxisIcon;
  public Texture2D FourAxisIcon;

  public RadialMenu RadialMenuPrefab;
  public RadialButton ButtonPrefab;
  public RadialControl RadialPuppet;
  public MultiAxisRadialPuppet MultiAxisPuppet;

  public float ButtonScale = 16f;
  public float ButtonRadiusScale = 92f;

  public RadialMenu CreateMainMenu(VRCExpressionsMenu menu, VRCExpressionParameters parameters) {
   RadialMenu rMenu = Instantiate(RadialMenuPrefab) as RadialMenu;
   rMenu.gameObject.name=menu.name;
   rMenu.Initialize(null, menu, parameters);
   rMenu.transform.SetParent(transform, false);
   rMenu.gameObject.SetActive(false);
   return rMenu;
  }

  public RadialMenu CreateMenu(RadialMenu parent, VRCExpressionsMenu.Control control) {
   RadialMenu rMenu = Instantiate(RadialMenuPrefab) as RadialMenu;
   rMenu.gameObject.name=control.subMenu.name;
   rMenu.Initialize(parent, control.subMenu, parameters);
   rMenu.transform.SetParent(transform, false);
   rMenu.gameObject.SetActive(false);

   subMenus.Add(rMenu.gameObject);
   return rMenu;
  }

  internal void SwapMenu(RadialMenu showing, RadialMenu parent) {
   showing.gameObject.SetActive(false);
   parent.gameObject.SetActive(true);
  }

  internal float ButtonSize() {
   return (GetComponent<RectTransform>().rect.size.x*ButtonScale)/100;
  }

  internal float ButtonRadius(GameObject button) {
   return (((GetComponent<RectTransform>().rect.size.x/2)*ButtonRadiusScale))/100-(button.GetComponent<RectTransform>().rect.size.x/2);
  }

  // --- --- Event --- ---
  private static readonly float MAX_BUTTON_PRESS_DURATION = 1f;
  private class ButtonWait {
   public RadialButton button;
   public float duration = 0;
  }
  private List<ButtonWait> pressedButtons = new List<ButtonWait>();
  private void Update() {
   List<ButtonWait> remove = new List<ButtonWait>();

   foreach(ButtonWait bw in pressedButtons) {
    if(bw.duration>=MAX_BUTTON_PRESS_DURATION) {
     remove.Add(bw);
     OnRadialButtonEvent(bw.button);
    }
    bw.duration+=Time.deltaTime;
   }

   foreach(ButtonWait bw in remove) {
    pressedButtons.Remove(bw);
   }
  }

  public event System.Action<RadialButton> RadialButtonEvent;

  internal void OnRadialButtonEvent(RadialButton radialButton) {
   //toggle the root parameter
   radialButton.SetActive(!radialButton.isOn);

   if(VRCExpressionsMenu.Control.ControlType.Button==radialButton.Control.type) {
    if(radialButton.isOn) { 
     radialButton.Selectable.interactable = false;
    pressedButtons.Add(new ButtonWait {
     button=radialButton,
     duration=0
    });
    } else {
     radialButton.Selectable.interactable=true;
    }
   }
   if(radialButton.isOn && VRCExpressionsMenu.Control.ControlType.RadialPuppet==radialButton.Control.type) {
    RadialPuppet.SetRadialButton(radialButton);
    RadialPuppet.gameObject.SetActive(true);
    if(null!=MenuActivationEvent)
     MenuActivationEvent(ControlType.All, false);
   }
   if(radialButton.isOn && VRCExpressionsMenu.Control.ControlType.FourAxisPuppet==radialButton.Control.type) {
    MultiAxisPuppet.SetRadialButton(radialButton);
    MultiAxisPuppet.gameObject.SetActive(true);
    if(null!=MenuActivationEvent)
     MenuActivationEvent(ControlType.All, false);
   }
   if(radialButton.isOn && VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet==radialButton.Control.type) {
    MultiAxisPuppet.SetRadialButton(radialButton);
    MultiAxisPuppet.gameObject.SetActive(true);
    if(null!=MenuActivationEvent)
     MenuActivationEvent(ControlType.All, false);
   }
  }

  public enum ControlType {
   Button = 101,
   Toggle = 102,
   SubMenu = 103,
   TwoAxisPuppet = 201,
   FourAxisPuppet = 202,
   RadialPuppet = 203,
   All = 111
  }

  //--- Events for Control Menus ---
  /**
   * All RadialButton need to subscribe to this
   */
  public event System.Action<ControlType, bool> MenuActivationEvent;

  public void SetMenuActivation(ControlType type, bool active) {
   MenuActivationEvent(type, active);
  }

  /**
   * Close the Control Menu and activate the menu
   */
  internal void CloseControl(RadialButton button, Control control) {
   control.gameObject.SetActive(false);
   if(null!=MenuActivationEvent)
    MenuActivationEvent(ControlType.All, true);
   OnRadialButtonEvent(button);
  }
 }
}