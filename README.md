<h1><img src="https://github.com/EducatedTanuki/TEA-Manager/raw/1.0.0/Resources/UI/Icons/TEA.png" alt="TEA-icon.png" width="64" height="64" style="max-width:100%;"> Tanuki's Educated Avatar Manager</h1>

Build and Test VRChat 3.0 Avatars in play mode  

If you think I deserve a :cookie:  
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/donate?business=YYCSYXEYPMQK2&currency_code=USD)

# Setup
- import [VRC Avatar SDK 3.0](https://vrchat.com/home/download)
- import [Lean Tween](https://assetstore.unity.com/packages/tools/animation/leantween-3595)
- import the `TEA-Manager-<version>.unitypackage` from [releases](https://github.com/EducatedTanuki/TEA-Manager/releases)

# Using `Tea Manager`
To use `Tea Manager` you are ***required*** to enter play mode using the custome play buttons in the `Play Tab`  
`TEA Manager` has to compile your avatars playable layers for everything to work  
- Go to the `Tea Manager` menu and select `Play Tab`  to open the `Play Tab` window  
- Dock `Play Tab` wherever makes sense for you
  - I recommend docking `Play Tab` at the below the Unity Toolbar
- Use the checkboxes at the bottom to control the spacing of the main controls
- There are tooltips for most controls

![add-play-tab](https://github.com/EducatedTanuki/TEA-Manager/blob/1.0.0/tutorial/assets/add-play-tab.gif)

In play mode `Tea Manager` emulates VRChat's Avatar system  
Use the `TEA UI` in the Game tab to test your avatar  

##### TEA UI

![play-example](https://github.com/EducatedTanuki/TEA-Manager/blob/1.0.0/tutorial/assets/play-example.png)

### Things to Note
> TEA Manager will automaticaly detect all avatars in the Scene. **It does not see avatars in other Scenes**. 

> Only switch avatars in play mode

> Only one `TEA Manager` can be active at a time, accross all **loaded** Scenes  
> `Play Tab` will enforce a valide setup  

### Play Mode Inputs
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

## GameObject Context Menu `TEA Functions`

> This menu can be reached by right clicking game objects in the heirarchy

##### Make Avatar 3.0
This setup a valid game object as an Avatar with some defaults I defined
- Add VRCAvatarDescriptor with default values
  - Eye look positions
  - Viewport set to between eye bones
  - Sets playable layers to copies of defaults
  - Adds default Expression menu and parameters
  - Force 6 point locamotion \- off
- Playable Layers
  - Creates `Playable Layers` folder in same folder as the Scene
  - Copies layers I created to the folder
- Expressions
  - Creates `Expressions` folder in same folder as the Scene
  - Copies files I created to the folder
  - Contains a SubMenu with all default acitons

##### Create Toggle
Create ON-OFF animations for a GameObject  
The toggle(s) are added to a `Toggles` folder next to the Scene

# Notes

#### Proxy Animations

> Proxy animations in the SDK are all single frames. They are replaced by full animations when your avatar is uploaded.  
> Do not expect your avatar to have full walking, running, or action animations in play mode unless you replace the appropriate layers and proxy animations.  
> If your user [Make Avatar 3.0](#make-avatar-30) your actions layer will have full animations