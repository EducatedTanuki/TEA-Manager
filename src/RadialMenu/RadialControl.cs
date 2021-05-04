using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace TEA.UI
{
 public class RadialControl : Control
 {
  internal override void SetRadialButton(RadialButton button)
  {
   this.button = button;
   if (VRCExpressionsMenu.Control.ControlType.RadialPuppet == button.Control.type)
   {
    Slider slider = GetComponent<Slider>();
    slider.value = AvatarController.current.GetParameterValue(button.Control.subParameters[0].name);
    slider.onValueChanged.RemoveAllListeners();
    slider.onValueChanged.AddListener(delegate (float value) { AvatarController.current.ExpressionParameterSet(button.Control, value); });
   }
  }

  private void Update()
  {
   Vector3 mouseLoc = GetComponent<RectTransform>().InverseTransformPoint(Input.mousePosition);
   if (((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) && !GetComponent<RectTransform>().rect.Contains(mouseLoc))
     || (!(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) && Input.anyKeyDown))
   {
    //Debug.Log($"mouse at {Input.mousePosition} rect is pos {GetComponent<RectTransform>().po} size {GetComponent<RectTransform>().rect.size}");
    RadialMenuController.current.CloseControl(button, this);
   }
  }
 }
}