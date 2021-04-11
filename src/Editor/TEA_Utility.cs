using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;

namespace TEA {
 public class TEA_Utility : MonoBehaviour {
  // ----- ------ Avatar ----- -----
  public static Dictionary<string, VRCAvatarDescriptor> GetAvatars(out bool crossScene) {
   crossScene=false;
   Dictionary<string, VRCAvatarDescriptor> avatars = new Dictionary<string, VRCAvatarDescriptor>();
   for(int i = 0; i<SceneManager.sceneCount; i++) {
    Scene scene = SceneManager.GetSceneAt(i);
    if(!scene.isLoaded)
     continue;

    GameObject[] rootObjects = scene.GetRootGameObjects();
    foreach(GameObject root in rootObjects) {
     VRCAvatarDescriptor avatar = root.GetComponent<VRCAvatarDescriptor>();
     if(null!=avatar) {
      avatars.Add(TEA_Manager.GetSceneAvatarKey(scene, avatar), avatar);
      if(scene != SceneManager.GetActiveScene())
       crossScene=true;
     }
    }
   }
   return avatars;
  }

  public static Dictionary<string, VRCAvatarDescriptor> GetAvatars(Scene scene) {
   Dictionary<string, VRCAvatarDescriptor> avatars = new Dictionary<string, VRCAvatarDescriptor>();
   if(!scene.isLoaded) {
    Debug.Log("Scene is not loaded");
    return avatars;
   }
   GameObject[] rootObjects = scene.GetRootGameObjects();
   foreach(GameObject root in rootObjects) {
    VRCAvatarDescriptor avatar = root.GetComponent<VRCAvatarDescriptor>();
    if(null!=avatar) {
     avatars.Add(avatar.gameObject.name, avatar);
    }
   }

   return avatars;
  }

  public static VRCAvatarDescriptor HasAvatars(Scene scene) {
   if(!scene.isLoaded) {
    Debug.Log("Scene is not loaded");
    return null;
   }
   GameObject[] rootObjects = scene.GetRootGameObjects();
   foreach(GameObject root in rootObjects) {
    VRCAvatarDescriptor avatar = root.GetComponent<VRCAvatarDescriptor>();
    if(null!=avatar) {
     return avatar;
    }
   }

   return null;
  }

  // ----- ------ Folders ----- -----
  public static string CreatePath(bool keepslash, params string[] pathParts) {
   string path = CreatePath(pathParts);
   if(!keepslash)
    return Regex.Replace(path, @"/$", "");
   return path;
  }
  public static string CreatePath(params string[] pathParts) {
   string path = "";
   foreach(string part in pathParts) {
    if(path.Length>0&&!path.EndsWith("/"))
     path+="/";
    path+=part;
   }
   return path;
  }

  public static void CreateAsset(UnityEngine.Object sibling, UnityEngine.Object newAsset, string name) {
   AssetDatabase.CreateAsset(newAsset, GetAssetDirectory(sibling, true)+name);
  }

  public static string GetAssetDirectory(UnityEngine.Object obj, bool keepSlash) {
   if(keepSlash)
    return Regex.Replace(AssetDatabase.GetAssetPath(obj), @"[^/]+\..*$", "");
   else
    return Regex.Replace(AssetDatabase.GetAssetPath(obj), @"/[^/]+\..*$", "");
  }

  public static string GetParent(string asset, bool keepSlash) {
   if(keepSlash)
    return Regex.Replace(asset, @"[^/]+\..*$", "");
   else
    return Regex.Replace(asset, @"/[^/]+\..*$", "");
  }
 }
}