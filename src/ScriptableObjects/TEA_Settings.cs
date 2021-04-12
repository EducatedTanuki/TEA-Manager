using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TEA {
 [System.Serializable]
 public class TEA_Settings : UnityEngine.ScriptableObject {
  [Header("Toggle Settings")]
  [SerializeField] public bool keepInScene = true;
  [SerializeField] public string keepInSceneTooltip = "Keep the TEA Manager prefab in your Scene while not in play mode";
  [SerializeField] public bool CanvasActive = true;
  [SerializeField] public string CanvasTooltip = "TEA Canvas ON-OFF, will activate when you play";
  [SerializeField] public bool WorldCenterActive = true;
  [SerializeField] public string worldCenterTooltip = "World Center ON-OFF";
  [SerializeField] public bool AudioListenerActive = true;
  [SerializeField] public string AudioListenerTooltip = "Audio Listener ON-OFF";
  [SerializeField] public bool LightActive = true;
  [SerializeField] public string LighTooltipt = "Directional Light ON-OFF";
  [SerializeField] public bool StageActive = true;
  [SerializeField] public string StageTooltip = "Stage ON-OFF";
  [SerializeField] public bool ValidateActive = true;
  [SerializeField] public string ValidateTooltip = "Validate SDK Compliance and All Parameters used";

  [Header("Layout Settings")]
  [SerializeField] public bool AllLayout = true;
  [SerializeField] public bool BeforeToggles = true;
  [SerializeField] public bool AfterToggles = true;
  [SerializeField] public bool BeforeButtons = true;
  [SerializeField] public bool AfterButtons = true;
  [SerializeField] public bool BeforeInfo = true;
  [SerializeField] public bool AfterInfo = true;
 }
}