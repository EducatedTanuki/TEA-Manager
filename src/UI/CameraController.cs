using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;

namespace TEA {
 [SerializeField]
 public class CameraController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler {
	public TEA_Settings Locomotion { get; set; }

	bool _freeCamera = false;
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
		 lockPosition = offsets[2];
		 region = Region.Upper;
		 CameraRig.enabled = true;
		}
	 }
	}

	private Vector3 newPosition;

	//--- --- Zoom --- ---
	Vector3 newZoom;
	Vector3 desiredZoom;
	float newZoomOrtho;
	private bool backingOff = false;

	[Header("Camera Rig")]
	//--- --- --- Rig --- --- ---
	public Camera RigCamera;
	public ParentConstraint CameraRig;
	public GameObject VerticalRotationRig;
	public Quaternion VerticalRotation;

	//--- Rig Pos ---
	[Header("Offsets")]
	public Vector3 MaxOffset;
	public Vector3 MinOffset;
	private int offsetId = 0;
	private Vector3[] offsets;

	//--- Rotation ---
	Quaternion horizontalRotation;

	//--- --- --- Mouse --- --- ---
	[Header("Mouse In")]
	public bool mouseIn = false;

	//--- --- Left --- ---
	Vector3 mouseStartRotation;
	Vector3 mouseStopRotation;

	//--- --- Middle --- ---
	Transform rootBone;
	Vector3 middleMouseStart;
	Vector3 middleMouseStop;
	int middleMousedirection = 0;
	Vector3 middleMousediff;
	Vector3 leftMouseDragStart;
	Vector3 middleMouseDirectionStart;

	enum Region {
	 Upper, Lower
	}
	Region region = Region.Upper;
	Vector3 lockPosition;

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

	 if(null == TEA_Manager.current.Settings)
		return false;

	 Locomotion = TEA_Manager.current.Settings;

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

	 middleMousediff = Vector3.zero;
	 leftMouseDragStart = Vector3.zero;
	 offsets = new Vector3[] { MinOffset, Vector3.zero, MaxOffset };
	 offsetId = offsets.Length - 1;
	 region = Region.Upper;
	 lockPosition = MaxOffset;

	 CameraRig.SetTranslationOffset(0, lockPosition);
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

	 HandleVerticalPosition();

	 if(FreeCamera)
		CameraRig.transform.position = Vector3.Lerp(CameraRig.transform.position, newPosition, Time.deltaTime * Locomotion.movementTime);

	 DetectCollision();

	 HandleHorizontalRotationInput();
	 HandleVerticalRotationInput();

	 HandleZoom();
	}

	private void HandleCameraMovement() {
	 if(Input.GetMouseButton(1) && FreeCamera) {
		newPosition = TEA_Manager.current.Avatar.transform.position;
		newPosition.y = TEA_Manager.current.GetAvatarViewPortWorld().y;
	 } else if(FreeCamera) {
		float y = newPosition.y;
		if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) {
		 newPosition += (RigCamera.transform.forward * Locomotion.zoomMultiplier * Locomotion.MoveSpeed);
		}
		if(Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) {
		 newPosition += (RigCamera.transform.forward * Locomotion.zoomMultiplier * -Locomotion.MoveSpeed);
		}
		if(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
		 newPosition += (RigCamera.transform.right * Locomotion.zoomMultiplier * -Locomotion.MoveSpeed);
		}
		if(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
		 newPosition += (RigCamera.transform.right * Locomotion.zoomMultiplier * Locomotion.MoveSpeed);
		}
		newPosition.y = y;
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
		newZoom = Vector3.forward * (closest.rootDistance + Locomotion.ClosestCameraZoom);
		//Debug.Log($"desired zoom ({desiredZoom}) | new zoom ({newZoom}) \n Closest: name[{closest.name}] distance[{closest.cameraDistance}] root distance [{closest.rootDistance}]\n closest[{ClosestCameraZoom}] ");
	 } else {
		backingOff = false;
		newZoom = desiredZoom;
	 }
	 RigCamera.transform.localPosition = Vector3.Lerp(RigCamera.transform.localPosition, newZoom, Time.deltaTime * Locomotion.movementTime);
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

	void HandleVerticalPosition() {
	 if(FreeCamera) {
		if(IsPanning())
		 newPosition.y += middleMousedirection * Locomotion.VerticalPanSpeed * Locomotion.zoomMultiplier;
		else {
		 Vector3 pos = Vector3.zero;
		 pos.y = -1 * middleMousediff.y * Locomotion.VerticalMoveMultiplier;
		 newPosition += pos;
		 CameraRig.transform.position += pos;
		}
	 } else if(Input.GetMouseButton(1)) {
		lockPosition = offsets[2];
		CameraRig.SetTranslationOffset(0, Vector3.Lerp(CameraRig.GetTranslationOffset(0), lockPosition, Time.deltaTime * Locomotion.movementTime));
		region = Region.Upper;
	 } else {
		float moveAmount = (-1 * middleMousediff.y * Locomotion.VerticalLockMoveMultiplier);

		if(IsPanning())
		 moveAmount = middleMousedirection * Locomotion.VerticalMoveSpeedLocked * Locomotion.zoomMultiplier;

		float dist = Vector3.Distance(Vector3.zero, CameraRig.GetTranslationOffset(0));
		if(Region.Upper == region) {
		 float totalDist = Vector3.Distance(Vector3.zero, offsets[2]);
		 float lerpTime = (moveAmount / totalDist) + (dist / totalDist);
		 lerpTime = lerpTime >= 1 ? 1 : lerpTime;
		 lockPosition = Vector3.Lerp(Vector3.zero, offsets[2], lerpTime);
		 //Debug.Log($"r[{region}] lerp[{lerpTime}] move[{moveAmount}] totalDist[{totalDist}] dist[{dist}]");
		 if(Mathf.Approximately(0, lerpTime) || 0 > lerpTime)
			region = Region.Lower;
		} else {
		 float totalDist = Vector3.Distance(Vector3.zero, offsets[0]);
		 float lerpTime = -1 * (moveAmount / totalDist) + (dist / totalDist);
		 lerpTime = lerpTime >= 1 ? 1 : lerpTime;
		 lockPosition = Vector3.Lerp(Vector3.zero, offsets[0], lerpTime);
		 //Debug.Log($"r[{region}] lerp[{lerpTime}] move[{moveAmount}] totalDist[{totalDist}] dist[{totalDist}]");
		 if(Mathf.Approximately(0, lerpTime) || 0 > lerpTime)
			region = Region.Upper;
		}
		CameraRig.SetTranslationOffset(0, Vector3.Lerp(CameraRig.GetTranslationOffset(0), lockPosition, Time.deltaTime * Locomotion.movementTime));
	 }//!FreeCamera
	 middleMousediff = Vector3.zero;
	}

	void HandleHorizontalRotationInput() {
	 if(FreeCamera) {
		if(Input.GetKey(KeyCode.E)) {
		 horizontalRotation *= Quaternion.Euler(Vector3.up * Locomotion.rotationAmount);
		} else if(Input.GetKey(KeyCode.Q)) {
		 horizontalRotation *= Quaternion.Euler(Vector3.up * -Locomotion.rotationAmount);
		}
	 }
	 CameraRig.transform.rotation = Quaternion.Lerp(CameraRig.transform.rotation, horizontalRotation, Time.deltaTime * Locomotion.movementTime);
	}

	void HandleVerticalRotationInput() {
	 VerticalRotationRig.transform.localRotation = Quaternion.Lerp(VerticalRotationRig.transform.localRotation, VerticalRotation, Time.deltaTime * Locomotion.movementTime);
	}

	void HandleAcceleration() {
	 if(Input.GetKey(KeyCode.LeftShift)) {
		Locomotion.zoomMultiplier = Locomotion.zoomQuickly;
	 } else if(Input.GetKey(KeyCode.LeftControl)) {
		Locomotion.zoomMultiplier = Locomotion.zoomSlowly;
	 } else {
		Locomotion.zoomMultiplier = 1;
	 }
	}

	void HandleZoom() {
	 if(!mouseIn)
		return;

	 if(RigCamera.GetComponent<Camera>().orthographic) {
		if(Input.mouseScrollDelta.y != 0) {
		 newZoomOrtho -= Input.mouseScrollDelta.y * Locomotion.zoomAmountOrtho;
		} else if(Input.GetKey(KeyCode.R)) {
		 newZoomOrtho += Locomotion.zoomAmountOrtho;
		} else if(Input.GetKey(KeyCode.F)) {
		 newZoomOrtho -= Locomotion.zoomAmountOrtho;
		}
		newZoomOrtho = newZoomOrtho < Locomotion.ClosestCameraZoom ? Locomotion.ClosestCameraZoom : newZoomOrtho;
		RigCamera.orthographicSize = Mathf.Lerp(RigCamera.orthographicSize, newZoomOrtho, Time.deltaTime * Locomotion.movementTime);
	 } else {
		if(Input.mouseScrollDelta.y != 0 && !(Input.mouseScrollDelta.y > 0 && backingOff == true)) {
		 desiredZoom -= Input.mouseScrollDelta.y * Locomotion.zoomAmount * Locomotion.zoomMultiplier;
		} else if(Input.GetKey(KeyCode.R)) {
		 desiredZoom += Locomotion.zoomAmount * Locomotion.zoomMultiplier;
		} else if(Input.GetKey(KeyCode.F)) {
		 desiredZoom -= Locomotion.zoomAmount * Locomotion.zoomMultiplier;
		}
		if(desiredZoom.z < Locomotion.ClosestCameraZoom)
		 desiredZoom.z = Locomotion.ClosestCameraZoom;
	 }
	}

	private bool IsPanning() {
	 return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
	}

	public void OnBeginDrag(PointerEventData eventData) {
	 // --- middle mouse ---
	 if(Input.GetMouseButton(2)) {
		middleMousedirection = 0;
		middleMouseStart = Input.mousePosition;
		middleMouseDirectionStart = Input.mousePosition;
		middleMousediff = Vector3.zero;
	 }
	 // --- left mouse ---
	 if(Input.GetMouseButton(0)) {
		mouseStartRotation = Input.mousePosition;
	 }
	}

	public void OnDrag(PointerEventData eventData) {
	 //Debug.Log($"mouse dragged middle[{Input.GetMouseButton(2)}");
	 // --- middle mouse ---
	 if(Input.GetMouseButton(2)) {
		middleMouseStop = Input.mousePosition;

		Vector3 diff = middleMouseStop - middleMouseStart;
		middleMousediff = diff;
		//Debug.Log($"mouse dragged middle[{leftMousediff}]");
		middleMousedirection = (int)Mathf.Sign((middleMouseStop - middleMouseDirectionStart).y);

		middleMouseStart = middleMouseStop;
	 }
	 // --- left mouse ---
	 if(Input.GetMouseButton(0)) {
		mouseStopRotation = Input.mousePosition;
		Vector3 diff = mouseStartRotation - mouseStopRotation;
		mouseStartRotation = Input.mousePosition;

		horizontalRotation *= Quaternion.Euler(Vector3.up * (-diff.x / 5f));
		VerticalRotation *= Quaternion.Euler(Vector3.right * (-diff.y / 5f));

	 }
	}
	public void OnEndDrag(PointerEventData eventData) {
	 // --- middle mouse ---
	 if(Input.GetMouseButtonUp(2)) {
		middleMousedirection = 0;
		middleMouseStop = Vector3.zero;
		middleMouseDirectionStart = Vector3.zero;
		middleMousediff = Vector3.zero;
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