<h1><img src="https://github.com/EducatedTanuki/TEA-Manager/raw/1.0.0/Resources/UI/Icons/TEA.png" alt="TEA-icon.png" width="64" height="64" style="max-width:100%;"> Tanuki's Educated Avatar Manager</h1>

Build and Test VRChat 3.0 Avatars in play mode  

If you think I deserve a :cookie:  
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/donate?business=YYCSYXEYPMQK2&currency_code=USD)

# Setup
- import [VRC Avatar SDK 3.0](https://vrchat.com/home/download)
- import [Lean Tween](https://assetstore.unity.com/packages/tools/animation/leantween-3595)
- import the `TEA-Manager-<version>.unitypackage` from [releases](https://github.com/EducatedTanuki/TEA-Manager/releases)

# Using `Tea Manager`
To use `Tea Manager` you are ***required*** to enter play mode using the `TEA Play` buttons in the `Play Tab`  
`TEA Manager` has to compile your avatars playable layers for everything to work  
- Go to the `Tea Manager` menu and select `Play Tab`  to open the `Play Tab` window  
- Dock `Play Tab` wherever makes sense for you
  - I recommend docking `Play Tab` below the Unity Toolbar (see below)
- Use the checkboxes at the bottom to control the spacing of the main controls
- There are tooltips for most controls

![add-play-tab](https://github.com/EducatedTanuki/TEA-Manager/blob/1.0.0/tutorial/assets/add-play-tab.gif)

In play mode `Tea Manager` emulates VRChat's Avatar system  
Use the `TEA UI` in the Game tab to test your avatar  

##### TEA UI

![play-example](https://github.com/EducatedTanuki/TEA-Manager/blob/1.0.0/tutorial/assets/play-example.png)

### TEA Manager Prefab
The `TEA Manager Prefab` contains everything needed for `TEA Manager` to operate in play mode.
`Play Tab` will automatically load the `TEA Manager Prefab` into the active scene when there is at least one avatar.  
If you toggle the "*Keep Prefab While Working*" option OFF, `Play Tab` will only load `TEA Manager Prefab` when it is needed for play or validation.

### Considerations
> `TEA Manager Prefab` does not work across **loaded** Scenes. It will **not** detect Avatars in adjacent loaded Scenes.  
> There can only be one `TEA Manager Prefab` active at a time, accross all **loaded** Scenes.  
> `Play Tab` tracks the active Scene, so multiple **loaded** Scenes can cause conflicts.  
> I recommend you work with one Scene loaded at a time.  

> For Scenes with multiple avatars  
> You can switch between avatars using the avatar dropdown but only do so in **play mode**  

# Inputs
Controls are context sensative, but in general this is the Input mapping  

|        Input       |          Action          |
|:------------------:|:------------------------:|
|     Left Mouse     |      Camera Rotation     |
|     Right Mouse    |  Reset Position or State |
|    Middle Mouse    | Camera Vertical Position |
| W, A, S, D, Arrows |  Camera/Avatar Movement  |
|        Q, E        |      Camera Rotation     |

<h1><img src="https://github.com/EducatedTanuki/TEA-Manager/raw/1.0.0/Resources/UI/Icons/TEA.png" alt="TEA-icon.png" width="48" height="48" style="max-width:100%;"> TEA Animations</h1>  

TEA Animations are utility animations added to the runtime animation controller.  
There are two hand poses and an assortment of full body animations.  
You can toggle TEA Animations by clicking the TEA Icon <img src="https://github.com/EducatedTanuki/TEA-Manager/raw/1.0.0/Resources/UI/Icons/TEA.png" alt="TEA-icon.png" width="16" height="16" style="max-width:100%;"> in the [TEA UI](#tea-ui)  
TEA Animaiton layers are placed after **Base** and before **Additive** in the compiled animator.  

# Utilities
`Tea Manager` provides some functions to make your development easier.

### GameObject Context Menu `TEA Functions`

> This menu can be reached by right clicking a GameObject in the heirarchy

##### Make Avatar 3.0
This sets up a GameObject as an Avatar with some defaults I defined.  
What it does:  
- Add VRCAvatarDescriptor with default values
  - Eye look positions
  - Viewport set to between eye bones
  - Sets playable layers to copies of defaults
  - Adds default Expression menu and parameters
  - Force 6 point locamotion \- off
    - Think of the Full Body Thotties
- Playable Layers
  - Copies default layers to `Playable Layers` folder in same folder as the Scene
  - Base
    - Default VRC locomotion controller
  - Additive
    - Blank
  - Gesture
    - Default VRC gesture controller
  - Action
    - Default vrc action layer with replaced emote animations
  - FX
    - Variation of default VRC gesture controller
- Expressions
  - Copies Expression Menus and Parameters to `Expressions` folder in same folder as the Scene
  - Expression Menu
    - Contains a SubMenu with toggles for VRCEmote 1 \- 7
  - Expression Parameters
    - Contains VRCEmote

##### Create Toggle
Create ON-OFF animations for a GameObject  
The toggle(s) are added to a `Toggles` folder in same folder as the Scene

# Notes

#### Proxy Animations

> Proxy animations in the SDK are all single frames. They are replaced by full animations when your avatar is uploaded.  
> Do not expect your avatar to have full walking, running, or action animations in play mode unless you replace the appropriate layers and proxy animations.  
> If you use [Make Avatar 3.0](#make-avatar-30) your **Action** layer will have full animations