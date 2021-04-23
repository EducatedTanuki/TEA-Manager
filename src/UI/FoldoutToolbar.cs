using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TEA.UI {
 public class FoldoutToolbar : ObjectToggle {
	public float ToggleTime = 1.5f;
	public FoldoutType Direction = FoldoutType.Left;
	public LeanTweenType EaseType = LeanTweenType.easeInBounce;
	public RectTransform Foldout;
	private Vector3 FoldoutPosition;
	private Vector3 localPos;

	public Sprite LeftArrow;
	public Sprite RightArrow;
	public Sprite UpArrow;
	public Sprite DownArrow;

	[HideInInspector]
	public enum FoldoutType {
	 Left = 0, Right = 1, Up = 2, Down = 3
	}

	protected override void Start() {
	 base.Start();

	 if(null != FoldoutController.current && !FoldoutController.current.Toolbars.Contains(this))
		FoldoutController.current.Toolbars.Add(this);

	 GameObject check = (transform.Find("Checked") ?? transform.Find("checked")).gameObject;
	 GameObject unCheck = (transform.Find("Unchecked") ?? transform.Find("unchecked")).gameObject;

	 if(Direction == FoldoutType.Left) {
		unCheck.GetComponent<Image>().sprite = LeftArrow;
		check.GetComponent<Image>().sprite = RightArrow;
	 } else if(Direction == FoldoutType.Right) {
		unCheck.GetComponent<Image>().sprite = RightArrow;
		check.GetComponent<Image>().sprite = LeftArrow;
	 } else if(Direction == FoldoutType.Up) {
		unCheck.GetComponent<Image>().sprite = UpArrow;
		check.GetComponent<Image>().sprite = DownArrow;
	 } else if(Direction == FoldoutType.Down) {
		unCheck.GetComponent<Image>().sprite = DownArrow;
		check.GetComponent<Image>().sprite = UpArrow;
	 }

	 FoldoutPosition = Foldout.anchoredPosition;
	 onValueChanged.AddListener(delegate { this.tween(); });
	 tween();
	}

	private Vector3 getDirection() {
	 Vector3 position = FoldoutPosition;
	 if(FoldoutType.Left == Direction) {
		position.x = 0 - Foldout.sizeDelta.x;
	 } else if(FoldoutType.Right == Direction) {
		position.x = FoldoutPosition.x + Foldout.sizeDelta.x;
	 } else if(FoldoutType.Up == Direction) {
		position.y = FoldoutPosition.y + Foldout.sizeDelta.y;
	 } else if(FoldoutType.Down == Direction) {
		position.y = FoldoutPosition.y - Foldout.sizeDelta.y;
	 }
	 return position;
	}

	private void tween() {
	 if(isOn) {
		LeanTween.move(Foldout, getDirection(), ToggleTime).setEase(EaseType);
	 } else {
		LeanTween.move(Foldout, FoldoutPosition, ToggleTime).setEase(EaseType);
	 }
	}

	// Update is called once per frame
	void Update() {
	}
 }
}