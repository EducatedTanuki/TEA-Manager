using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace TEA.UI {
 public class RadialMenu : MonoBehaviour {
  public VRCExpressionsMenu VRCMenu;
  public VRCExpressionParameters Parameters;

  public RadialMenu Parent;

  public RadialButton backButton;

  public RadialMenu() {
  }

  public void Initialize(RadialMenu parent, VRCExpressionsMenu menu, VRCExpressionParameters parameters) {
   this.Parent=parent;
   this.VRCMenu=menu;
   this.Parameters=parameters;

   backButton=CreateButton(null, 0, 1);

   //Debug.Log("VRCMenu: null=" + (null == VRCMenu) + " : control="+ (null==VRCMenu.controls));
   int buttonCount = VRCMenu.controls.Count+1;
   int buttonIndex = 1;
   foreach(VRCExpressionsMenu.Control control in VRCMenu.controls) {
    float theta = (2*Mathf.PI/buttonCount)*buttonIndex++;
    float xpos = Mathf.Sin(theta);
    float ypos = Mathf.Cos(theta);
    CreateButton(control, xpos, ypos);
   }
  }

  public RadialButton CreateButton(VRCExpressionsMenu.Control control, float xpos, float ypos) {
   RadialButton newButton = Instantiate(RadialMenuController.current.ButtonPrefab) as RadialButton;
   Button selectable = newButton.GetComponent<Button>();

   if(null==control) {
    if(null==Parent) {
     selectable.interactable=false;
     control=new VRCExpressionsMenu.Control() {
      icon=RadialMenuController.current.HomeIcon,
      name="home"
     };
    } else {
     selectable.onClick.AddListener(delegate { RadialMenuController.current.SwapMenu(this, Parent); });
     control=new VRCExpressionsMenu.Control() {
      icon=RadialMenuController.current.BackIcon,
      name="back"
     };
    }
   }

   newButton.Initialize(this, control, xpos, ypos);
   newButton.gameObject.name=control.name;

   return newButton;
  }

  // Start is called before the first frame update
  void Start() {

  }

  // Update is called once per frame
  void Update() {

  }
 }
}