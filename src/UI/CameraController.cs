using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;

namespace TEA {
 [SerializeField]
 public class CameraController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler {
	[Header("Camera Movement")]
	[Tooltip("Movement speed of the camera rig when in Free Camera mode")]
	public float MoveSpeed = 0.15f;
	private Vector3 newPosition;
	private bool _freeCamera = false;
	public bool FreeCamera {
	 get => _freeCamera; set {
		_freeCamera = value;
		if(value) {
		 CameraRig.enabled = false;
		 CameraRig.transform.position = new Vector3(TEA_Manager.current.Avatar.transform.position.x, CameraRig.transform.position.y, TEA_Manager.current.Avatar.transform.position.z);
		 newPosition = CameraRig.transform.position;
		} else {
		 newPosition = TEA_Manager.current.Avatar.transform.position;
		 newPosition.y = TEA_Manager.current.GetAvatarViewPortWorld().y;
		 offsetId = offsets.Length - 1;
		 CameraRig.enabled = true;
		}
	 }
	}

	[Header("Camera Zoom")]
	//--- --- Zoom --- ---
	public Camera RigCamera;
	public float ClosestCameraZoom = .2f;
	public Vector3 newZoom;
	public Vector3 desiredZoom;
	[Tooltip("Base distance the camera will move when zooming")]
	public Vector3 zoomAmount = Vector3.forward;
	[Tooltip("Multiplies base distance the camera will move (see slow and fast zoom)")]
	public float zoomMultiplier = 1;
	[Tooltip("Multiplier for fast zooming")]
	public float zoomQuickly = 3.5f;
	[Tooltip("Multiplier for slow zooming")]
	public float zoomSlowly = 0.35f;
	public float newZoomOrtho;
	public float zoomAmountOrtho = .1f;
	private bool backingOff = false;

	[Header("Camera Rig")]
	//--- --- --- Rig --- --- ---
	public ParentConstraint CameraRig;
	public GameObject VerticalRotationRig;
	public Quaternion VerticalRotation;
	public float movementTime = 5f;
	private Transform armature;

	//--- Rig Pos ---
	public Vector3 MaxOffset;
	public Vector3 MinOffset;
	private int offsetId = 0;
	private Vector3[] offsets;

	//--- Rotation ---
	public float rotationAmount = 2f;
	public Quaternion horizontalRotation;

	//--- --- --- Mouse --- --- ---

	[Header("Camera Rotation")]
	//--- --- Left --- ---
	public bool mouseIn = false;
	public Vector3 mouseStartRotation;
	public Vector3 mouseStopRotation;

	[Header("Camera Vertical Position")]
	//--- --- Middle --- ---
	[Tooltip("Base speed for vertical camera movement in Free Camera mode")]
	public float VerticalMoveSpeed = 1f;
	[Tooltip("Theshold for moving between feet, hips, and viewport when locked to avatar")]
	public float VerticalMoveThreshold = 5f;
	private Transform rootBone;
	private Vector3 middleMouseStart;
	private Vector3 middleMouseStop;
	private float middleMousediff = 0;
	private int middleMousedirection = 0;

	public float LeftMouseVerticalMultiplier = .1f;
	private Vector3 leftMousediff;
	private Vector3 leftMouseDragStart;

	bool _initialized = false;
	private void Start() {
	 _initialized = Initialized();

	 if(!TEAManagerUpdateRegistered) {
		TEA_Manager.current.TEAManagerEvent += OnTEAManagerUpdate;
		TEAManagerUpdateRegistered = true;
	 }
	}

	private bool TEAManagerUpdateRegistered = false;
	public void OnTEAManagerUpdate(TEA_Manager tea_manager) {
	 _initialized = false;
	 Start();
	}

	private bool Initialized() {
	 if(_initialized)
		return true;

	 if(null == TEA_Manager.current.Avatar)
		return false;

	 newZoom = RigCamera.transform.localPosition;
	 desiredZoom = RigCamera.transform.localPosition;
	 newZoomOrtho = RigCamera.orthographicSize;

	 horizontalRotation = CameraRig.transform.rotation;
	 VerticalRotation = VerticalRotationRig.transform.localRotation;

	 middleMouseStart = Input.mousePosition;

	 rootBone = AvatarController.GetRootBone();

	 MaxOffset = TEA_Manager.current.GetAvatarViewPortWorld() - rootBone.position;
	 MaxOffset.x = 0;
	 MaxOffset.z = 0;
	 MinOffset = TEA_Manager.current.Avatar.transform.position - rootBone.position;
	 MinOffset.x = 0;
	 MinOffset.z = 0;

	 leftMousediff = Vector3.zero;
	 leftMouseDragStart = Vector3.zero;
	 offsets = new Vector3[] { MinOffset, Vector3.zero, MaxOffset };
	 offsetId = offsets.Length - 1;

	 newPosition = CameraRig.transform.position;

	 // --- avoid parent constrait lock on switch
	 FreeCamera = FreeCamera;

	 _initialized = true;
	 return true;
	}

	// Update is called once per frame
	void Update() {
	 if(!Initialized())
		return;

	 HandleAcceleration();

	 HandleCameraMovement();

	 if(!IsLeftVerticalDrag())
		HandleVerticalPosition();

	 if(FreeCamera)
		CameraRig.transform.position = Vector3.Lerp(CameraRig.transform.position, newPosition, Time.deltaTime * movementTime);

	 DetectCollision();

	 if(IsLeftVerticalDrag()) {
		Vector3 pos = CameraRig.transform.position;
		pos.y += (leftMousediff.y * LeftMouseVerticalMultiplier);
		if(!FreeCamera) {
		 pos = pos - rootBone.position;
		 pos.x = 0;
		 pos.z = 0;
		 if(pos.y >= offsets[1].y && Vector3.Distance(offsets[1], pos) >= .5 * Vector3.Distance(offsets[1], offsets[2]))
			offsetId = 2;
		 else if(pos.y >= offsets[1].y)
			offsetId = 1;
		 else if(Vector3.Distance(offsets[1], pos) >= .5 * Vector3.Distance(offsets[1], offsets[0]))
			offsetId = 0;
		 else
			offsetId = 1;

		 if(pos.y >= offsets[offsets.Length - 1].y) {
			pos = offsets[offsets.Length - 1];
			offsetId = offsets.Length - 1;
		 } else if(pos.y <= offsets[0].y) {
			pos = offsets[0];
			offsetId = 0;
		 }
		 if(leftMousediff.y != 0)
			CameraRig.SetTranslationOffset(0, Vector3.Lerp(CameraRig.GetTranslationOffset(0), pos, Time.deltaTime * movementTime));
		} else
		 CameraRig.transform.position = Vector3.Lerp(CameraRig.transform.position, pos, Time.deltaTime * movementTime);
		leftMousediff = Vector3.zero;
	 } else {
		HandleHorizontalRotationInput();
		HandleVerticalRotationInput();
	 }
	 HandleZoom();
	}

	private void HandleCameraMovement() {
	 if(Input.GetMouseButton(1) && FreeCamera) {
		newPosition = TEA_Manager.current.Avatar.transform.position;
		newPosition.y = TEA_Manager.current.GetAvatarViewPortWorld().y;
	 } else if(FreeCamera) {
		if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) {
		 newPosition += (RigCamera.transform.forward * zoomMultiplier * MoveSpeed);
		}
		if(Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) {
		 newPosition += (RigCamera.transform.forward * zoomMultiplier * -MoveSpeed);
		}
		if(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
		 newPosition += (RigCamera.transform.right * zoomMultiplier * -MoveSpeed);
		}
		if(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
		 newPosition += (RigCamera.transform.right * zoomMultiplier * MoveSpeed);
		}
		newPosition.y = CameraRig.transform.position.y;
	 }
	}

	private struct CameraObject {
	 public float cameraDistance;
	 public float rootDistance;
	 public string name;
	 public Transform transform;
	}

	private void DetectCollision() {
	 CameraObject closest = getClosest(rootBone);
	 //Debug.Log($"Closest: name[{closest.name}] distance[{closest.cameraDistance}] root distance [{closest.rootDistance}] tranloc[{closest.transform.position}] girloc[{CameraRig.gameObject.transform.position}");
	 if(!FreeCamera && (closest.rootDistance > desiredZoom.z)) {
		backingOff = true;
		newZoom = Vector3.forward * (closest.rootDistance + ClosestCameraZoom);
		//Debug.Log($"desired zoom ({desiredZoom}) | new zoom ({newZoom}) \n Closest: name[{closest.name}] distance[{closest.cameraDistance}] root distance [{closest.rootDistance}]\n closest[{ClosestCameraZoom}] ");
	 } else {
		backingOff = false;
		newZoom = desiredZoom;
	 }
	 RigCamera.transform.localPosition = Vector3.Lerp(RigCamera.transform.localPosition, newZoom, Time.deltaTime * movementTime);
	}

	private CameraObject getClosest(Transform tran) {
	 CameraObject closest = new CameraObject();
	 closest.cameraDistance = Mathf.Abs(Vector3.Distance(
		tran.position,
		RigCamera.transform.position));
	 closest.rootDistance = Mathf.Abs(Vector3.Distance(CameraRig.transform.position, tran.position));
	 closest.transform = tran;
	 closest.name = tran.name;

	 for(int i = 0; i < tran.childCount; i++) {
		CameraObject child = getClosest(tran.GetChild(i));
		if(child.cameraDistance < closest.cameraDistance)
		 closest = child;
	 }
	 return closest;
	}

	bool verticalSpring = false;
	int verticalSpringOffsetId = 0;
	public float VerticalSpringTrigger = .5f;
	void HandleVerticalPosition() {
	 if(FreeCamera) {
		newPosition.y += middleMousedirection * VerticalMoveSpeed * zoomMultiplier;
	 } else if(verticalSpring) {
		Vector3 pos = Vector3.Lerp(CameraRig.GetTranslationOffset(0), offsets[verticalSpringOffsetId], (middleMousediff * VerticalSpringTrigger) / VerticalMoveThreshold);
		CameraRig.SetTranslationOffset(0, Vector3.Lerp(CameraRig.GetTranslationOffset(0), pos, Time.deltaTime * movementTime));
	 } else {
		CameraRig.SetTranslationOffset(0, Vector3.Lerp(CameraRig.GetTranslationOffset(0), offsets[offsetId], Time.deltaTime * movementTime));
	 }
	}

	void HandleHorizontalRotationInput() {
	 if(FreeCamera) {
		if(Input.GetKey(KeyCode.E)) {
		 horizontalRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
		} else if(Input.GetKey(KeyCode.Q)) {
		 horizontalRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);
		}
	 }
	 CameraRig.transform.rotation = Quaternion.Lerp(CameraRig.transform.rotation, horizontalRotation, Time.deltaTime * movementTime);
	}

	void HandleVerticalRotationInput() {
	 if(Input.GetKey(KeyCode.UpArrow)) {
		//VerticalRotation *= Quaternion.Euler(Vector3.right * rotationAmount);
	 } else if(Input.GetKey(KeyCode.DownArrow)) {
		//VerticalRotation *= Quaternion.Euler(Vector3.right * -rotationAmount);
	 }
	 VerticalRotationRig.transform.localRotation = Quaternion.Lerp(VerticalRotationRig.transform.localRotation, VerticalRotation, Time.deltaTime * movementTime);
	 //Debug.Log($"vertical rotation ({verticalRotation.x},{verticalRotation.y},{verticalRotation.z})");
	}

	void HandleAcceleration() {
	 if(Input.GetKey(KeyCode.LeftShift)) {
		zoomMultiplier = zoomQuickly;
	 } else if(Input.GetKey(KeyCode.LeftControl)) {
		zoomMultiplier = zoomSlowly;
	 } else {
		zoomMultiplier = 1;
	 }
	}

	void HandleZoom() {
	 if(!mouseIn)
		return;

	 if(RigCamera.GetComponent<Camera>().orthographic) {
		if(Input.mouseScrollDelta.y != 0) {
		 newZoomOrtho -= Input.mouseScrollDelta.y * zoomAmountOrtho;
		} else if(Input.GetKey(KeyCode.R)) {
		 newZoomOrtho += zoomAmountOrtho;
		} else if(Input.GetKey(KeyCode.F)) {
		 newZoomOrtho -= zoomAmountOrtho;
		}
		newZoomOrtho = newZoomOrtho < ClosestCameraZoom ? ClosestCameraZoom : newZoomOrtho;
		RigCamera.orthographicSize = Mathf.Lerp(RigCamera.orthographicSize, newZoomOrtho, Time.deltaTime * movementTime);
	 } else {
		if(Input.mouseScrollDelta.y != 0 && !(Input.mouseScrollDelta.y > 0 && backingOff == true)) {
		 desiredZoom -= Input.mouseScrollDelta.y * zoomAmount * zoomMultiplier;
		} else if(Input.GetKey(KeyCode.R)) {
		 desiredZoom += zoomAmount * zoomMultiplier;
		} else if(Input.GetKey(KeyCode.F)) {
		 desiredZoom -= zoomAmount * zoomMultiplier;
		}
		if(desiredZoom.z < ClosestCameraZoom)
		 desiredZoom.z = ClosestCameraZoom;
	 }
	}

	public void OnBeginDrag(PointerEventData eventData) {
	 // --- middle mouse ---
	 if(Input.GetMouseButton(2)) {
		middleMousediff = 0;
		middleMousedirection = 0;
		middleMouseStart = Input.mousePosition;
	 }
	 // --- left mouse ---
	 if(Input.GetMouseButton(0)) {
		mouseStartRotation = Input.mousePosition;
		leftMousediff = Vector3.zero;
	 }
	}

	public void OnDrag(PointerEventData eventData) {
	 //Debug.Log($"mouse dragged middle[{Input.GetMouseButton(2)}");
	 // --- middle mouse ---
	 if(Input.GetMouseButton(2)) {
		middleMouseStop = Input.mousePosition;
		middleMousediff = Vector3.Distance(middleMouseStop, middleMouseStart);

		Vector3 diff = middleMouseStop - middleMouseStart;
		middleMousedirection = (int)Mathf.Sign(diff.y);
		int newOffset = offsetId + middleMousedirection;
		if(VerticalMoveThreshold <= middleMousediff) {
		 //Debug.Log($"offset {offsetId} new offset {newOffset}");
		 verticalSpring = false;
		 offsetId = newOffset >= 0 && newOffset < offsets.Length ? newOffset : offsetId;
		 middleMouseStart = Input.mousePosition;
		} else if(newOffset >= 0 && newOffset < offsets.Length) {
		 verticalSpring = true;
		 verticalSpringOffsetId = newOffset;
		} else {
		 verticalSpring = false;
		}
	 }
	 // --- left mouse ---
	 if(Input.GetMouseButton(0)) {
		mouseStopRotation = Input.mousePosition;
		Vector3 diff = mouseStartRotation - mouseStopRotation;
		mouseStartRotation = Input.mousePosition;

		//Debug.Log($"Mouse diff ({diff})");
		leftMousediff = diff;

		if(!(IsLeftVerticalDrag())) {
		 horizontalRotation *= Quaternion.Euler(Vector3.up * (-diff.x / 5f));
		 VerticalRotation *= Quaternion.Euler(Vector3.right * (-diff.y / 5f));
		}
	 }
	}

	private bool IsLeftVerticalDrag() {
	 return (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
	}

	public void OnEndDrag(PointerEventData eventData) {
	 // --- middle mouse ---
	 if(Input.GetMouseButtonUp(2)) {
		middleMousediff = 0;
		middleMousedirection = 0;
		verticalSpring = false;
	 }
	}

	public void OnPointerEnter(PointerEventData eventData) {
	 mouseIn = true;
	}

	public void OnPointerExit(PointerEventData eventData) {
	 mouseIn = false;
	}
 }
}