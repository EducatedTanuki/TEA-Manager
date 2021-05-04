using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TEA.UI {
 public class FoldoutController : MonoBehaviour {
	public static FoldoutController current;

	private void Awake() {
	 current = this;
	}

	public List<FoldoutToolbar> Toolbars = new List<FoldoutToolbar>();

	// Update is called once per frame
	void Update() {
	 if(Input.GetKeyUp(KeyCode.H)&& Toolbars.Count>0) {
		bool state = Toolbars[0].isOn;
		bool diff = false;
		foreach(FoldoutToolbar t in Toolbars) {
		 if(state != t.isOn)
			diff = true;
		}
		state = diff ? false : !state;
		foreach(FoldoutToolbar t in Toolbars) {
		 t.isOn = state;
		 t.onValueChanged.Invoke(state);
		}
	 }
	}
 }
}