using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TEA {
 public class SpeedIndicator : MonoBehaviour {
	public List<GameObject> Exclusions = new List<GameObject>();

	void Update() {
	 if(null == AvatarController.current || null == AvatarController.current.Locomotion)
		return;

	 TEA_Settings.MoveTypes type = AvatarController.current.Locomotion.MoveType;
	 for(int i = 0; i < transform.childCount; i++) {
		if(!Exclusions.Contains(transform.GetChild(i).gameObject))
		 transform.GetChild(i).gameObject.SetActive(false);
	 }
	 transform.Find(type.ToString()).gameObject.SetActive(true);
	}
 }
}