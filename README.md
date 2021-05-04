<h1><img src="https://github.com/EducatedTanuki/TEA-Manager/raw/main/Resources/UI/Icons/TEA.png" alt="TEA-icon.png" width="64" height="64" style="max-width:100%;"> Tanuki's Educated Avatar Manager</h1>

Build and Test VRChat 3.0 Avatars in play mode  

If you think I deserve a :cookie:  
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/donate?business=YYCSYXEYPMQK2&currency_code=USD)

# Setup
- Import [VRC Avatar SDK 3.0](https://vrchat.com/home/download)
- Import [Lean Tween](https://assetstore.unity.com/packages/tools/animation/leantween-3595)
- Import the `TEA-Manager-<version>.unitypackage` from [releases](https://github.com/EducatedTanuki/TEA-Manager/releases)
- Remove all cameras from your Scene. Additional cameras can interfere with `TEA Manager`.  

# Using Tea Manager

To use `Tea Manager` you are ***required*** to enter play mode using the play/validation buttons in the `Control Tab`.  
`TEA Manager` has to compile your avatars playable layers before entering play mode.  

![play-tab](https://github.com/EducatedTanuki/TEA-Manager/blob/main/tutorial/assets/play-tab.PNG)

Here are options for getting `Control Tab` into your workspace.
1. Load Supplied Layout
    - Go to `Window/Layouts/More/Load from disk...`  
    - Select `Assets/TEA Manager/TEA_Layout.wlt`  
2. Put `Control Tab` in your layout
    - Go to the `Tea Manager` menu and select `Control Tab` 
    - Dock `Control Tab` wherever makes sense for you
      - I recommend docking `Control Tab` below the [Unity Toolbar](https://docs.unity3d.com/Manual/Toolbar.html#:~:text=You%20can%20find%20the%20Toolbar%20at%20the%20top,interactive%20view%20into%20the%20world%20you%20are%20creating.)  

![add-play-tab](https://github.com/EducatedTanuki/TEA-Manager/blob/main/tutorial/assets/add-play-tab.gif)

- Use the layout checkboxes at the bottom to control the spacing of the main controls.  
- There are tooltips for most controls.  

# TEA UI

In play mode `Tea Manager` emulates VRChat's avatar system.  
Use the `TEA UI` in the Game tab to test your avatar.  

> Select your avatar in the heirarchy to see their active animator in the Animator tab.

![play-example](https://github.com/EducatedTanuki/TEA-Manager/blob/main/tutorial/assets/play-example.png)

### Inputs

|        Input       |          Action          |
|:------------------:|:------------------------:|
|     Left Mouse     |      Camera Rotation     |
|     Right Mouse    |  Reset Position or State |
|    Middle Mouse    |   Camera Vertical Pan    |
|  Alt+Middle Mouse  |  Camera Vertical Scroll  |
|     Mouse Wheel    |        Camera Zoom       |
| Shift+Mouse Wheel  |       Faster Action      |
|     Ctrl+Mouse     |       Slower Action      |
| W, A, S, D, Arrows |  Camera/Avatar Movement  |
|       Shift        |    Faster Camera Move Speeds    |
|       Ctrl        |    Slower Camera Move Speeds    |
|     Tap Shift      |    Avatar Move Speeds (Walk, Run, Sprint)    |
|        Q, E        |  Camera/Avatar Rotation  |
|          H         |   Hide/Show Controls   |

# TEA Settings

`TEA Settings` is a file that saves the state of:
- `Control Tab` toggles
- Avatar locomotion settings  
- [TEA Functions](#tea-functions) settings  

`Control Tab` will use the first `TEA Settings` found in Assets.  
If a `TEA Settings` file does not exist one is created at `Assets/TEA Settings.asset`.  

# Validation

Turn validation on or off using the <img src="https://github.com/EducatedTanuki/TEA-Manager/raw/main/Resources/UI/Icons/validation.png" alt="validation.png" width="16" height="16" style="max-width:100%;"> checkbox.  

`TEA Manager` can run validation on your avatar to check:  
- Avatar is compliant with SDK 3.0.  
  - Base, Additive, Gesture, and Action contains only Transform animations.
  - FX contains no Transform animations.
  - Toggle in [TEA Settings](#tea-settings) (Layer Restrictions)
- All Expression Parameters are being used
  - Toggle in [TEA Settings](#tea-settings)
- Expression Menu Controls are valid
  - Button parameter is set
  - Toggle parameter is set
  - SubMenus are set
  - Radial Puppet has at least 1 sub parameter
  - Two Axis Puppet has at least 1 sub parameter
  - Four Axis Puppet has all 4 sub parameters
- Parameter Drivers are valid
  - Parameters are in ExpressionParameters

# TEA Manager Prefab

- The `TEA Manager Prefab` contains everything needed for `TEA Manager` to operate in play mode.
- "*Keep Prefab While Working*" toggle
  - When OFF: `Control Tab` will only load the `TEA Manager Prefab` into the [Active Scene](https://docs.unity3d.com/Manual/MultiSceneEditing.html#:~:text=The%20Scene%20divider%20menu%20for%20loaded%20Scenes) when it is needed for play or validation.
  - When ON: `Control Tab` will automatically load the `TEA Manager Prefab` into the [Active Scene](https://docs.unity3d.com/Manual/MultiSceneEditing.html#:~:text=The%20Scene%20divider%20menu%20for%20loaded%20Scenes) when there is at least one avatar.  
  - OFF by default

# Things to Note

#### Multiple Avatars  

- You can switch between avatars using the avatar dropdown in **play mode**  
- `TEA UI` controls do not currently sync when switching avatars. Re-toggling them will sync them with the avatar's animator state.  
- ***Avatars added during play will not work***  

#### Multi-Scene setups  

- `Control Tab` will recognize avatars in every loaded Scene  
- There must be avatars in the [Active Scene](https://docs.unity3d.com/Manual/MultiSceneEditing.html#:~:text=The%20Scene%20divider%20menu%20for%20loaded%20Scenes) for `TEA Manager` to be added  

- There can only be one `TEA Manager Prefab` active at a time, accross all **loaded** Scenes.  
- `Control Tab` will add `TEA Manager Prefab` to the [Active Scene](https://docs.unity3d.com/Manual/MultiSceneEditing.html#:~:text=The%20Scene%20divider%20menu%20for%20loaded%20Scenes)
and will remove `TEA Manager Prefab` from all other loaded scenes.  

#### Default Expressions

`TEA Manager` does not add default Expressions like VRC does.
If your avatar does not have Expressions definded your radial menu will be blank.

#### Proxy Animations

Proxy animations in the SDK are all single frames. They are replaced by full animations when your avatar is uploaded.  
Do not expect your avatar to have full walking, running, or action animations in play mode unless you replace the appropriate layers and proxy animations.  
If you use [Make Avatar 3.0](#make-avatar-30) your **Action** layer will have full animations.  

<h1><img src="https://github.com/EducatedTanuki/TEA-Manager/raw/main/Resources/UI/Icons/TEA.png" alt="TEA-icon.png" width="48" height="48" style="max-width:100%;"> TEA Animations</h1>  

- `TEA Animations` are utility animations added to the runtime animation controller.  
- ***This is why your avatars hands are held up.***  
- You can toggle `TEA Animations` by clicking the TEA Icon <img src="https://github.com/EducatedTanuki/TEA-Manager/raw/main/Resources/UI/Icons/TEA.png" alt="TEA-icon.png" width="16" height="16" style="max-width:100%;"> in the [TEA UI](#tea-ui).  
- There are two hand poses and an assortment of full body animations.  
- TEA Animaiton layers are placed after **Base** and before **Additive** in the compiled animator.  

# TEA Functions

`Tea Manager` provides some functions to make your development easier.  
`TEA Functions` can be reached by right clicking a GameObject in the heirarchy.  

### Make Avatar 3.0
This sets up a GameObject as an avatar with defaults I defined.  
You can change these defaults through the `TEA Settings`.  

##### VRCAvatarDescriptor Setup
- Eye look positions
- Viewport set between eye bones
- Playable Layers
  - Copies default playable layers to `Assets/<Scene Folder>/<Avatar Name>/Playable Layers`
  - Base
    - Default VRC locomotion controller
  - Additive
    - Blank Controller
  - Gesture
    - Default VRC gesture controller
  - Action
    - Default vrc action layer with replaced emote animations
  - FX
    - Variation of default VRC gesture controller with empty animation clips
      - Layer `Right Hand`
        - weight 1 by default
      - Layer `Left Hand Unique` has separate animations from "Right Hand"
        - weight 1 by default
      - Layer `Left Hand` has the same animations as "Right Hand"
        - weight 0 by default
- Expressions
  - Copies default Expression Menus and Parameters to `Assets/<Scene Folder>/<Avatar Name>/Expressions`
  - Expression Menu
    - Contains a SubMenu with toggles for VRCEmote 1 \- 7
  - Expression Parameters
    - Contains VRCEmote
- Force 6 point locamotion \- off
  - Think of the Full Body Thotties

### Set Eye Look as Default

Sets an avatar's eye look as the default for [Make Avatar 3.0](#make-avatar-30).  

### Create Toggle

Create ON-OFF animations for a GameObject.  
The toggle(s) are added to `Assets/<Scene Folder>/<Avatar Name>/Toggles`.  
You can change the final folder name through the `TEA Settings`.  

# TEA Tools

### Avatar 8 Tracks

`Avatar 8Track` allows you to take a folder of AudioClips and add them all to your avatar.  
Go to the menu `TEA Manager/Tools/Avatar 8Track`.  
Instruction are part of the tool.  
Set default control icons using `Assets/TEA Manager/TEA Tools/Avatar 8Tracks/Avatar8TrackSettings.asset`.  

> You can bulk change the Audio Source settings by selecting all of the `Track` objects created.  

![play-example](https://github.com/EducatedTanuki/TEA-Manager/blob/main/tutorial/8Tracks/example.png)  

# Resources
[Game Dev Guide](https://www.youtube.com/channel/UCR35rzd4LLomtQout93gi0w)  
[Stage](https://assetstore.unity.com/packages/2d/textures-materials/sky/farland-skies-cloudy-crown-60004)  
[Raphtalia Model](https://www.vrcmods.com/item/7235)  
[Action Animations](https://www.vrcmods.com/user/09williamsad)  
[Sit Animation](https://www.vrcmods.com/item/3922)  