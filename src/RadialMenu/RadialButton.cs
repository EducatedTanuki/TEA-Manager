using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System;

namespace TEA.UI
{
 public class RadialButton : Button
 {
  public RadialMenu RadialMenu;
  public VRCExpressionsMenu.Control Control;
  public RadialMenu SubMenu;
  //TODO set this in the prefab
  public Button Selectable;
  private bool Error;

  public bool isOn = false;
  public float xpos;
  public float ypos;

  private List<Vector3> childPos = new List<Vector3>();
  private List<float> childX = new List<float>();
  private List<float> childY = new List<float>();

  public RadialButton() : base()
  {
  }

  public RadialButton(RadialMenu radialMenu, VRCExpressionsMenu.Control control)
  {
   this.RadialMenu = radialMenu;
   this.Control = control;
   this.Selectable=GetComponent<Button>();
  }

  private void SetOnClick()
  {
   if (Error)
    return;
   if (VRCExpressionsMenu.Control.ControlType.SubMenu == Control.type)
   {
    Selectable.onClick.AddListener(delegate { RadialMenuController.current.SwapMenu(RadialMenu, SubMenu); });
   }
   //ignore back and home buttons
   else if(null != Control.parameter)
   {
    Selectable.onClick.AddListener(delegate { RadialMenuController.current.OnRadialButtonEvent(this); });
   }
   if(VRCExpressionsMenu.Control.ControlType.Toggle == Control.type)
    AvatarController.TEA_ParameterSet+=OnParameterSet;
  }

  public void OnParameterSet(AvatarController.Parameter parameter)
  {
   if (IsDestroyed())
   {
    AvatarController.TEA_ParameterSet-= OnParameterSet;
    return;
   }

   if (this.Control.parameter.name == parameter.name)
   {
    isOn = (this.Control.value==parameter.fVal);
    transform.Find("Toggle").gameObject.SetActive(isOn);
    //Debug.Log($"OnParameter called in [{gameObject.name}], toggled to [{isOn}]");
   }
  }

  public void MenuActivationEvent(RadialMenuController.ControlType type, bool status)
  {
   if (IsDestroyed())
   {
    RadialMenuController.current.MenuActivationEvent -= MenuActivationEvent;
    return;
   }
   if (RadialMenuController.ControlType.All == type || Control.type.ToString() == type.ToString())
   {
    GetComponent<Button>().interactable = status;
   }
  }

  private Sprite sprite;
  public void Initialize(RadialMenu parent, VRCExpressionsMenu.Control control, float xpos, float ypos)
  {
   this.RadialMenu = parent;
   this.Control = control;
   this.xpos = xpos;
   this.ypos = ypos;
   Texture2D tex = Control.icon ?? null;
   this.Selectable=GetComponent<Button>();

   RadialMenuController.current.MenuActivationEvent += MenuActivationEvent;

   string error = "";
   if (VRCExpressionsMenu.Control.ControlType.SubMenu == Control.type)
   {
    if (null == tex)
     tex = RadialMenuController.current.SubMenuIcon;

    if (null == Control.subMenu)
    {
     Error = true;
     error = $"Sub Menu [{control.name}] is not set";
     TEA_Manager.SDKError(error);
    }
    else
    {
     SubMenu = RadialMenuController.current.CreateMenu(parent, Control);
    }
   }
   if (VRCExpressionsMenu.Control.ControlType.RadialPuppet == Control.type)
   {
    if (null == tex)
     tex = RadialMenuController.current.RadialPuppetIcon;

    if (null == Control.subParameters[0] || string.IsNullOrEmpty(Control.subParameters[0].name))
    {
     error = $"Radial Puppet [{control.name}] does not have a Rotation Parameter";
     TEA_Manager.SDKError(error);
     Error = true;
    }
   }
   if (VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet == Control.type)
   {
    if (null == tex)
     tex = RadialMenuController.current.TwoAxisIcon;
    if ((null == Control.subParameters[0] || string.IsNullOrEmpty(Control.subParameters[0].name)) && (null == Control.subParameters[1] || string.IsNullOrEmpty(Control.subParameters[1].name)))
    {
     Error = true;
     error = $"Two Axis Radial Puppet [{control.name}] does not have a either Horizontal or Vertical Parameters";
     TEA_Manager.SDKError(error);
    }
   }
   if (VRCExpressionsMenu.Control.ControlType.Toggle == Control.type)
   {
    if (null == tex)
     tex = RadialMenuController.current.ToggleIcon;
   }
   if (VRCExpressionsMenu.Control.ControlType.FourAxisPuppet == Control.type)
   {
    if (null == tex)
     tex = RadialMenuController.current.FourAxisIcon;
    if((null==Control.subParameters[0]||string.IsNullOrEmpty(Control.subParameters[0].name))
     &&(null==Control.subParameters[1]||string.IsNullOrEmpty(Control.subParameters[1].name))
     &&(null==Control.subParameters[2]||string.IsNullOrEmpty(Control.subParameters[2].name))
     &&(null==Control.subParameters[3]||string.IsNullOrEmpty(Control.subParameters[3].name))) {
     Error=true;
     error=$"Four Axis Radial Puppet [{control.name}] is missing a parameter";
     TEA_Manager.SDKError(error);
    }
   }
   if (VRCExpressionsMenu.Control.ControlType.Button == Control.type)
   {
    if (null == tex)
     tex = RadialMenuController.current.ButtonIcon;
   }

    if (!Error)
   {
    transform.Find("Text").GetComponent<Text>().text = Control.name;
    if (null != tex)
    {
     sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
     SetImageSprite(image, sprite);
    }
   }
   else
   {
    sprite = RadialMenuController.current.ErrorSprite;
    SetImageSprite(image, sprite);
    transform.Find("Text").GetComponent<Text>().text = error;
   }

   transform.SetParent(RadialMenu.transform);
   SetScale();
   SetOnClick();
  }

  internal void SetActive(bool isActive) {
   isOn=isActive;
   if(isActive)
    AvatarController.current.ExpressionParameterSet(Control);
   else 
    AvatarController.current.ExpressionParameterReset(Control);
  }

  protected override void OnEnable()
  {
   base.OnEnable();
   SetImageSprite(image, sprite);
  }

  protected override void OnDisable()
  {
   base.OnDisable();
   SetImageSprite(image, sprite);
  }

  private void Update()
  {
   SetScale();
  }

  private void SetScale()
  {
   Vector2 size = GetComponent<RectTransform>().rect.size;
   float nSize = RadialMenuController.current.ButtonSize();
   Vector2 nScale = GetComponent<RectTransform>().localScale;

   float radius = RadialMenuController.current.ButtonRadius(gameObject);

   transform.localPosition = new Vector3(xpos, ypos) * radius;
   GetComponent<RectTransform>().localScale = new Vector3(nSize / size.x, nSize / size.y, 1);
   //Debug.Log($"Set [{name}] with pos [{transform.localPosition}] and Sc[{nScale}] Sn [{nSize}] S[{size}] radius [{radius}]");
  }
  internal static void SetImageSprite(Image image, Sprite sprite)
  {
   if (null != sprite)
   {
    image.overrideSprite = sprite;
    image.sprite = sprite;
   }
  }
 }
}