using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TEA.UI
{
 public abstract class Control : MonoBehaviour
 {
  protected RadialButton button;

  abstract internal void SetRadialButton(RadialButton button);
 }
}
