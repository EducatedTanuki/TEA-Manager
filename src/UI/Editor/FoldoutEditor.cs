using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEditor;

namespace TEA.UI
{
		[CustomEditor(typeof(FoldoutToolbar))]
		public class FoldoutEditor : Editor
		{
				public override void OnInspectorGUI()
				{
						FoldoutToolbar script = (FoldoutToolbar)target;
						base.OnInspectorGUI();
						if(script.Direction == FoldoutToolbar.FoldoutType.Right)
						{
								script.transform.Find("unchecked").GetComponent<Image>().sprite = script.RightArrow;
								script.transform.Find("checked").GetComponent<Image>().sprite = script.LeftArrow;
						}
				}
		}
}