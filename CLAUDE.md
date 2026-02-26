# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 6 (6000.2.8f1) 3D game project using Universal Render Pipeline (URP). Third-person character controller with medieval village/environment theme.

## Build & Run

This is a Unity project - open with Unity Editor 6000.2.8f1:
- **Play in Editor**: Ctrl+P (or Play button)
- **Build**: File > Build Settings > Build (target: Windows x64)
- **Sync C# Projects**: Automatic on script changes, or Edit > Preferences > External Tools > Regenerate project files

## Testing

Uses Unity Test Framework (`com.unity.test-framework` v1.6.0):
- **Run Tests**: Window > General > Test Runner
- **Edit Mode Tests**: Tests that run without Play mode
- **Play Mode Tests**: Tests that run during Play mode

## Architecture

### Input System
- Uses Unity's new Input System (`com.unity.inputsystem`)
- Input actions defined in `Assets/InputSystem_Actions.inputactions`
- Two action maps: "Player" (gameplay) and "UI" (menus)
- Control schemes: Keyboard&Mouse, Gamepad, Touch, Joystick, XR

**Player Actions**: Move, Look, Attack, Interact (hold), Crouch, Jump, Sprint, Previous, Next

### Player Controller
- `Assets/StarterAssets/ThirdPersonController/Scripts/ThirdPersonController.cs` - Main controller
- Uses CharacterController (not Rigidbody) for movement
- Cinemachine for camera follow (`com.unity.cinemachine`)
- Input handled via `StarterAssetsInputs` component

### Mobile Support
- Virtual joystick/button system in `Assets/StarterAssets/Mobile/Scripts/VirtualInputs/`
- `UICanvasControllerInput` bridges UI events to `StarterAssetsInputs`

### Project Structure
```
Assets/
├── MyProject/           # Custom game content
│   ├── myScripts/       # Custom scripts (add new scripts here)
│   ├── myScene/         # Game scenes (GameScene1, MainMenu)
│   └── myPrefabs/       # Environment prefabs (Carnival, Castle, Church, village, walls)
├── StarterAssets/       # Unity's third-person controller package
├── SyntyStudios/        # Polygon art assets (Adventure, Knights, Prototype, Starter)
└── Settings/            # URP render pipeline assets (PC_RPAsset, Mobile_RPAsset)
```

### Key Dependencies
- Universal Render Pipeline 17.2.0
- Input System 1.14.2
- Cinemachine 2.10.4
- AI Navigation 2.0.9 (NavMesh)
- Timeline 1.8.9

## Code Conventions

- C# scripts use Unity's component pattern with MonoBehaviour
- Animation parameters accessed via `Animator.StringToHash()` for performance
- Input System conditional compilation: `#if ENABLE_INPUT_SYSTEM`
- StarterAssets namespace for controller scripts
