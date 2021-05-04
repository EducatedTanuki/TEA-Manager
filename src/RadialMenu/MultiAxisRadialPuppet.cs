using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace TEA.UI {
 public class MultiAxisRadialPuppet : Control, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler {
  private bool mouseIn = false;
  private bool dragging = false;
  private RectTransform rectTransform;

  public Image Joystick;
  public Sprite UpSprite;
  public Image Up;
  public Sprite DownSprite;
  public Image Down;
  public Sprite LeftSprite;
  public Image Left;
  public Sprite RightSprite;
  public Image Right;

  internal override void SetRadialButton(RadialButton button) {
   this.button=button;
   if(!(VRCExpressionsMenu.Control.ControlType.FourAxisPuppet==button.Control.type||VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet==button.Control.type)) {
    Debug.LogError($"Button [{button.name}] is not a MultiAxisPuppet");
    return;
   }

   if(null!=rt) {
    if(VRCExpressionsMenu.Control.ControlType.FourAxisPuppet==button.Control.type) {
     float pUp = AvatarController.current.GetParameterValue(button.Control.subParameters[0].name);
     float pRight = AvatarController.current.GetParameterValue(button.Control.subParameters[1].name);
     float pDown = AvatarController.current.GetParameterValue(button.Control.subParameters[2].name);
     float pLeft = AvatarController.current.GetParameterValue(button.Control.subParameters[3].name);
     Joystick.rectTransform.localPosition=
      new Vector3(
       maxRadius*(pRight>0 ? pRight : -1*pLeft),
       maxRadius*(pUp>0 ? pUp : -1*pDown));
    } else if(VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet==button.Control.type) {
     Joystick.rectTransform.localPosition=
      new Vector3(
       maxRadius*AvatarController.current.GetParameterValue(button.Control.subParameters[0].name),
       maxRadius*AvatarController.current.GetParameterValue(button.Control.subParameters[1].name));
    }
   }

   VRCExpressionsMenu.Control.Label up = button.Control.GetLabel(0);
   if(null!=up.icon) {
    UpSprite=Sprite.Create(up.icon, new Rect(0.0f, 0.0f, up.icon.width, up.icon.height), new Vector2(0.5f, 0.5f));
    RadialButton.SetImageSprite(Up, UpSprite);
   }
   if(!string.IsNullOrEmpty(up.name))
    Up.transform.Find("Text").GetComponent<Text>().text=up.name;

   VRCExpressionsMenu.Control.Label down = button.Control.GetLabel(2);
   if(null!=down.icon) {
    DownSprite=Sprite.Create(down.icon, new Rect(0.0f, 0.0f, down.icon.width, down.icon.height), new Vector2(0.5f, 0.5f));
    RadialButton.SetImageSprite(Down, DownSprite);
   }
   if(!string.IsNullOrEmpty(down.name))
    Down.transform.Find("Text").GetComponent<Text>().text=down.name;

   VRCExpressionsMenu.Control.Label left = button.Control.GetLabel(3);
   if(null!=left.icon) {
    LeftSprite=Sprite.Create(left.icon, new Rect(0.0f, 0.0f, left.icon.width, left.icon.height), new Vector2(0.5f, 0.5f));
    RadialButton.SetImageSprite(Left, LeftSprite);
   }
   if(!string.IsNullOrEmpty(left.name))
    Left.transform.Find("Text").GetComponent<Text>().text=left.name;

   VRCExpressionsMenu.Control.Label right = button.Control.GetLabel(1);
   if(null!=right.icon) {
    RightSprite=Sprite.Create(right.icon, new Rect(0.0f, 0.0f, right.icon.width, right.icon.height), new Vector2(0.5f, 0.5f));
    RadialButton.SetImageSprite(Right, RightSprite);
   }
   if(!string.IsNullOrEmpty(right.name))
    Right.transform.Find("Text").GetComponent<Text>().text=right.name;
  }

  // Start is called before the first frame update
  private RectTransform rt;
  private float radius;
  private float pRadius;
  private float maxRadius;
  void Start() {
   rectTransform=GetComponent<RectTransform>();
   rt = Joystick.rectTransform;
   GetComponent<RectTransform>();
   radius = (rt.sizeDelta.x*rt.localScale.x)/2;
   pRadius = (rectTransform.sizeDelta.x*rectTransform.localScale.x)/2;
   maxRadius = pRadius-radius;
  }

  // Update is called once per frame
  void Update() {
   if(dragging&&Input.GetMouseButton(0)) {
    Vector3 newPos = rectTransform.InverseTransformPoint(Input.mousePosition);
    float mRadius = Mathf.Sqrt(Mathf.Pow(newPos.x, 2)+Mathf.Pow(newPos.y, 2));
    //Debug.Log($"newPos[{newPos}], mRadius[{mRadius}], radius[{radius}], pRadius[{pRadius}], Theta[{Mathf.Atan(rt.localPosition.y / rt.localPosition.x)}]");

    if(mRadius>maxRadius) {
     float theta = Mathf.Atan2(newPos.y, newPos.x);
     newPos.x=Mathf.Cos(theta)*(maxRadius);
     newPos.y=Mathf.Sin(theta)*(maxRadius);
     //Debug.Log($"[{theta}], [{newPos.x}]");
    }

    rt.localPosition=newPos;
    float xPos = newPos.x/maxRadius;
    float yPos = newPos.y/maxRadius;
    if(button.Control.type==VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet) {
     float[] values = { xPos, yPos };
     AvatarController.current.ExpressionParameterSet(button.Control, values);
    } else if(button.Control.type==VRCExpressionsMenu.Control.ControlType.FourAxisPuppet) {
     float[] values = { yPos>0 ? yPos : 0, xPos>0 ? xPos : 0, yPos<0 ? Mathf.Abs(yPos) : 0, xPos<0 ? Mathf.Abs(xPos) : 0 };
     AvatarController.current.ExpressionParameterSet(button.Control, values);
    }
   }

   if(mouseIn) {
    if(Input.GetMouseButton(1)) {
     Joystick.rectTransform.localPosition=Vector3.zero;
     if(button.Control.type==VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet) {
      float[] values = { 0, 0 };
      AvatarController.current.ExpressionParameterSet(button.Control, values);
     } else if(button.Control.type==VRCExpressionsMenu.Control.ControlType.FourAxisPuppet) {
      float[] values = { 0, 0, 0, 0 };
      AvatarController.current.ExpressionParameterSet(button.Control, values);
     }
    }
   } else {
    if((Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1)||Input.GetMouseButtonDown(2))||Input.anyKeyDown) {
     //Debug.Log($"mouse at {Input.mousePosition} rect is pos {GetComponent<RectTransform>().po} size {GetComponent<RectTransform>().rect.size}");
     RadialMenuController.current.CloseControl(button, this);
    }
   }
  }

  public void OnBeginDrag(PointerEventData eventData) {
    Vector3 mouseLoc = Joystick.rectTransform.InverseTransformPoint(Input.mousePosition);
    if(Input.GetMouseButton(0)&&Joystick.rectTransform.rect.Contains(mouseLoc)) {
     dragging=true;
    }
  }

  public void OnDrag(PointerEventData eventData) {

  }

  public void OnEndDrag(PointerEventData eventData) {
   dragging=false;
  }

  public void OnPointerEnter(PointerEventData eventData) {
   mouseIn=true;
  }

  public void OnPointerExit(PointerEventData eventData) {
   mouseIn=false;
  }

  public void OnPointerDown(PointerEventData eventData) {
   if(mouseIn && PointerEventData.InputButton.Left==eventData.button)
    dragging=true;
  }
 }
}