using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraFOV_Panel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
 public Camera Camera;

 public float movementTime = 5f;
 public float DragMultiplier = 1f;
 private Vector3 mouseStart;
 private Vector3 mouseStop;
 private float fovNew;

 public void OnBeginDrag(PointerEventData eventData)
 {
  if (Input.GetMouseButton(0))
  {
   mouseStart = Input.mousePosition;
  }
 }

 public void OnDrag(PointerEventData eventData)
 {
  if (Input.GetMouseButton(0))
  {
   mouseStop = Input.mousePosition;
   Vector3 diff = mouseStart - mouseStop;
   mouseStart = Input.mousePosition;

   fovNew = Camera.fieldOfView + diff.y * DragMultiplier;
   if (0 >= fovNew)
    fovNew = 0;
   else if (179 <= fovNew)
    fovNew = 179;
  }
 }

 public void OnEndDrag(PointerEventData eventData)
 {
  
 }

 private void Start()
 {
  if (null == Camera)
  {
   Debug.LogError($"No camera attached to [{gameObject.name}]");
   return;
  }

  fovNew = Camera.fieldOfView;
 }

 private void Update()
 {
  if (null == Camera)
   return;

  Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView, fovNew, Time.deltaTime * movementTime);
 }
}
