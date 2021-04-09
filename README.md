# TEA Manager
Build and Test VRChat 3.0 Avatars in play mode

# Setup
- import [VRC Avatar SDK 3.0](https://vrchat.com/home/download)
- import [Lean Tween](https://assetstore.unity.com/packages/tools/animation/leantween-3595)
- import the `TEA-Manager-<version>.unitypackage` from [releases](https://github.com/EducatedTanuki/TEA-Manager/releases)
- Drag the `TEA Manager` prefab into your scene

> Only one `TEA Manager` can be active at a time, accross all loaded Scenes

- Unpack the `TEA Manager` prefab

TEA Manager will automaticaly detect all avatars in the Scene. **It does not see avatars in other Scenes**.

# Using `Tea Manager`
To use `Tea Manager` you must enter playmode using the custome play button.  
`Tea Manager` will compile your playable layers and put unity in play mode.  

There are play buttons in the `Tea Manager` inspector  

![inspector](/tutorial/assets/inspector.png)

and the Play Tab, accessable through the menu `Tea Manager/Play Tab`

![add-play-tab](/tutorial/assets/add-play-tab.gif)

In Play mode `Tea Manager` provides a UI under the "Game" tab that lets you interact with an emulated VRChat Avatar system. 

![play-example](/tutorial/assets/play-example.png)

### Game Controls
|        Input       |          Action          |
|:------------------:|:------------------------:|
|     Left Mouse     |      Camera Rotation     |
|     Right Mouse    |  Reset Position or State |
|    Middle Mouse    | Camera Vertical Position |
| W, A, S, D, Arrows |  Camera/Avatar Movement  |
|        Q, E        |      Camera Rotation     |

# Utilities
`Tea Manager` provides some functions to make your development easier.

### Right Click GameObjects in Hierarchy
###### Make Avatar 3.0
This setup a valid game object as an Avatar with some defaults I defined
- Add VRCAvatarDescriptor with default values
  - eye look positions
  - viewport set to between eye bones
  - Sets playable layers to copies of defaults
  - Adds default Expression menu and parameters
  - force 6 point locamotion off
- Playable Layers
  - Creates `Playable Layers` folder in same folder as the Scene
  - Copies layers I created to the folder
- Expressions
  - Creates `Expressions` folder in same folder as the Scene
  - Copies files I created to the folder
  - Contains a SubMenu with all default acitons

###### Create Toggle
Create ON-OFF animations for a GameObjec  
The toggle(s) are added to a `Toggles` folder next to the Scene

# Notes
### Proxy Animations

> Proxy animations in the SDK are all single frames. They are replaced by full animations when your avatar is uploaded.  
> Do not expect your avatar to have walking animations in play mode unless you override the Base layer and replace them.