using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Avatar8TrackSettings", menuName = "ScriptableObjects/Avatar8TrackSettings", order = 1)]
public class Avatar8TrackSettings : ScriptableObject {
 public Texture2D StopIcon;
 public Texture2D FolderIcon;
 public Texture2D VolumeIcon;
 public Texture2D DiskIcon;
 public Texture2D TrackIcon;
}
