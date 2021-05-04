using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace TEA.UI
{
		[AddComponentMenu("UI/ObjectToggle", 32)]
		[DisallowMultipleComponent]
		public class ObjectToggle : Toggle
		{
				protected ObjectToggle()
				{}

				protected override void Start()
				{
						base.Start();
						onValueChanged.AddListener(delegate { this.SetState(); });
						SetState();
				}

				public void SetState()
				{
						for (int i = 0; i < this.gameObject.transform.childCount; i++)
						{
								GameObject obj = this.gameObject.transform.GetChild(i).gameObject;
								if (obj.name.StartsWith("checked") || obj.name.StartsWith("Checked"))
								{
										obj.SetActive(isOn);
								}
								if (obj.name.StartsWith("unchecked") || obj.name.StartsWith("Unchecked"))
								{
										obj.SetActive(!isOn);
								}
						}
				}
		}
}
