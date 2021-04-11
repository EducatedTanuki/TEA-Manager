using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TEA {
 [System.Serializable]
 public class TEA_Settings : UnityEngine.ScriptableObject {
  [SerializeField] public bool keep_in_scene = true;
  [SerializeField] public bool _canvas = true;
  [SerializeField] public bool _worldCenter = true;
  [SerializeField] public bool _audioListener = true;
  [SerializeField] public bool _light = true;
  [SerializeField] public bool _stage = true;
  [SerializeField] public bool _validate = true;
 }
}