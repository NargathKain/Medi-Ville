# Medieval Village - Script Documentation

This document provides a comprehensive overview of all custom C# scripts used in the Medieval Village Unity project.

---

## Table of Contents

1. [Interaction System](#1-interaction-system)
2. [Dialogue System](#2-dialogue-system)
3. [Animal AI System](#3-animal-ai-system)
4. [Ambient Effects](#4-ambient-effects)
5. [Audio System](#5-audio-system)
6. [UI System](#6-ui-system)

---

## 1. Interaction System

Located in: `Assets/MyProject/myScripts/Interact/`

The interaction system allows the player to interact with objects in the world by looking at them and pressing the E key.

### IInteractable.cs
**Type:** Interface
**Purpose:** Defines the contract that all interactable objects must implement.

| Method/Property | Description |
|-----------------|-------------|
| `InteractionPrompt` | Text shown in "Press E to..." prompt |
| `OnInteract(Interactor)` | Called when player presses interact |
| `OnEndInteract()` | Called when interaction ends |
| `OnReadyInteract()` | Called when player looks at object |
| `OnAbortInteract()` | Called when player looks away |

---

### Interactor.cs
**Type:** MonoBehaviour
**Attach to:** Player's camera
**Purpose:** Detects interactable objects via raycast and manages interaction state.

| Field | Description |
|-------|-------------|
| `targetTag` | Tag for interactable objects (default: "Interactable") |
| `rayMaxDistance` | Maximum interaction distance (default: 5) |
| `layerMask` | Layers to raycast against |
| `playerCamera` | Camera to raycast from |
| `interactorUI` | Reference to InteractorUI for messages |
| `hint` | GameObject showing interaction prompt |
| `promptText` | TextMeshPro for "Press E to..." text |

**Key Methods:**
- `ReceiveInteract(string)` - Display a message from an interactable
- `EndInteract(IInteractable)` - End interaction (called by interactables)

---

### InteractorUI.cs
**Type:** MonoBehaviour
**Attach to:** Canvas or UI manager
**Purpose:** Displays text messages from interactable objects on screen.

| Field | Description |
|-------|-------------|
| `messageText` | TextMeshPro element for displaying messages |

**Key Methods:**
- `ShowTextMessage(string)` - Display a message
- `HideTextMessage()` - Hide the message

---

### Door.cs
**Type:** MonoBehaviour, implements IInteractable
**Attach to:** Door objects
**Purpose:** Animated door that rotates open/closed when interacted with.

| Field | Description |
|-------|-------------|
| `indicator` | Visual feedback when player can interact |
| `openAngle` | Rotation angle when opened (default: 90°) |
| `rotationSpeed` | Degrees per second (default: 180) |
| `rotationAxis` | Axis of rotation (default: Y-up) |
| `openSound` | Sound when opening |
| `closeSound` | Sound when closing |

**Properties:**
- `IsOpen` - Whether door is currently open
- `IsMoving` - Whether door is animating

---

### Chest.cs
**Type:** MonoBehaviour, implements IInteractable
**Attach to:** Chest objects
**Purpose:** Animated chest with opening lid and optional content message.

| Field | Description |
|-------|-------------|
| `lid` | Transform of the chest lid |
| `openAngle` | Lid rotation angle (default: -110°) |
| `rotationSpeed` | Degrees per second |
| `contentMessage` | Optional message shown when opened |
| `oneTimeOnly` | If true, chest stays open permanently |

**Properties:**
- `IsOpen` - Whether chest is open
- `HasBeenOpened` - Whether chest has ever been opened

---

### Lever.cs
**Type:** MonoBehaviour, implements IInteractable
**Attach to:** Lever objects
**Purpose:** Interactive lever that controls a CastleGate.

| Field | Description |
|-------|-------------|
| `leverHandle` | Transform that rotates |
| `pullAngle` | Rotation when pulled (default: 45°) |
| `connectedGate` | CastleGate this lever controls |
| `toggleMode` | If true, lever stays pulled |
| `gateActivationDelay` | Delay before gate moves |

---

### CastleGate.cs
**Type:** MonoBehaviour
**Attach to:** Portcullis/gate objects
**Purpose:** Vertical gate controlled by a Lever.

| Field | Description |
|-------|-------------|
| `raiseHeight` | How high the gate raises |
| `moveSpeed` | Movement speed |
| `moveSound` | Sound when starting to move |
| `stopSound` | Sound when reaching destination |
| `movingLoopSound` | Looping sound during movement |

**Public Methods:**
- `Raise()` - Open the gate
- `Lower()` - Close the gate
- `Toggle()` - Switch state
- `SetStateImmediate(bool)` - Instant position change

---

### TextSign.cs
**Type:** MonoBehaviour, implements IInteractable
**Attach to:** Signs, notices, readable objects
**Purpose:** Displays text message when player interacts.

| Field | Description |
|-------|-------------|
| `indicator` | Visual feedback object |
| `text` | Message to display |
| `promptText` | Custom prompt (default: "Read") |

---

## 2. Dialogue System

Located in: `Assets/MyProject/myScripts/Dialogue/`

Handles NPC conversations with two types: interactive (press E) and proximity-based (automatic).

### DialogueData.cs
**Type:** ScriptableObject
**Create via:** Right-click → Create → MyProject → Dialogue Data
**Purpose:** Stores NPC name and dialogue lines.

| Field | Description |
|-------|-------------|
| `npcName` | Name displayed above dialogue |
| `dialogueLines` | Array of dialogue strings |
| `autoAdvanceTime` | Seconds before auto-advancing (0 = manual) |

---

### DialogueManager.cs
**Type:** MonoBehaviour (Singleton)
**Attach to:** DialogueManager GameObject
**Purpose:** Controls the dialogue UI panel and manages active dialogues.

| Field | Description |
|-------|-------------|
| `dialoguePanel` | Panel to show/hide |
| `npcNameText` | TextMeshPro for NPC name |
| `dialogueText` | TextMeshPro for dialogue lines |
| `promptText` | Optional "Press E to continue" text |
| `interactivePrompt` | Prompt text for manual advance |
| `autoPrompt` | Prompt text for auto-advance |

**Public Methods:**
- `StartDialogue(DialogueData, bool, Action)` - Start a dialogue
- `AdvanceLine()` - Go to next line
- `EndDialogue()` - End dialogue immediately

**Events:**
- `OnDialogueStarted` - Fired when dialogue begins
- `OnDialogueEnded` - Fired when dialogue ends

**Access via:** `DialogueManager.Instance`

---

### InteractableNPC.cs
**Type:** MonoBehaviour, implements IInteractable
**Attach to:** NPCs the player talks to by pressing E
**Purpose:** NPC that starts dialogue when player interacts.

| Field | Description |
|-------|-------------|
| `dialogueData` | DialogueData asset for this NPC |
| `indicator` | Visual feedback object |
| `customPrompt` | Custom prompt text (optional) |

**Setup:**
1. Add to NPC GameObject
2. Add Collider
3. Tag as "Interactable"
4. Assign DialogueData

---

### ProximityNPC.cs
**Type:** MonoBehaviour
**Attach to:** NPCs that talk when player approaches
**Purpose:** Automatically triggers dialogue when player enters trigger zone, camera looks at NPC.

| Field | Description |
|-------|-------------|
| `dialogueData` | DialogueData asset |
| `lookAtTarget` | Transform camera looks at (NPC's head) |
| `cameraRotationSpeed` | How fast camera rotates to NPC |
| `lockCameraDuringDialogue` | Keep camera locked on NPC |
| `playerTag` | Tag to identify player |
| `triggerOnce` | Only trigger once ever |
| `startDelay` | Delay before dialogue starts |
| `useAutoAdvance` | Auto-advance dialogue lines |

**Setup:**
1. Add to NPC GameObject
2. Add Collider with Is Trigger = true
3. Set collider radius for detection area
4. Assign lookAtTarget (NPC's head)

---

## 3. Animal AI System

Located in: `Assets/MyProject/myScripts/Animals/`

Simple wandering AI for farm animals using NavMesh.

### AnimalData.cs
**Type:** ScriptableObject
**Create via:** Right-click → Create → MyProject → Animals → Animal Data
**Purpose:** Defines behavioral parameters for animals.

| Field | Description |
|-------|-------------|
| `moveSpeed` | Walking speed |
| `wanderRadius` | Max distance for new destinations |
| `turnSpeed` | Angular rotation speed |
| `acceleration` | NavMeshAgent acceleration |
| `stoppingDistance` | Distance to stop at destination |
| `minIdleTime` | Minimum idle duration |
| `maxIdleTime` | Maximum idle duration |
| `walkTrigger` | Animation name for walking |
| `idleTrigger` | Animation name for idle |
| `navMeshSampleDistance` | NavMesh sampling distance |
| `maxDestinationAttempts` | Max tries to find valid destination |

---

### AnimalController.cs
**Type:** MonoBehaviour
**Requires:** NavMeshAgent
**Attach to:** Animal prefabs
**Purpose:** State machine controlling animal wandering behavior.

| Field | Description |
|-------|-------------|
| `animalData` | AnimalData ScriptableObject |
| `animator` | Animator component (auto-found) |
| `showDebugGizmos` | Show debug visualization |

**States:**
- `Idle` - Animal stands still for random duration
- `Walking` - Animal moves to random destination

**Properties:**
- `IsWalking` - True if currently walking
- `IsIdle` - True if currently idle
- `Data` - Returns assigned AnimalData

**Setup:**
1. Add NavMeshAgent to animal
2. Add AnimalController
3. Create and assign AnimalData asset
4. Bake NavMesh in scene

---

## 4. Ambient Effects

Located in: `Assets/MyProject/myScripts/Ambient/`

Scripts for environmental animations and effects.

### ScrollingWater.cs
**Type:** MonoBehaviour
**Attach to:** Stream/river meshes
**Purpose:** Scrolls texture UV to simulate flowing water.

| Field | Description |
|-------|-------------|
| `scrollSpeed` | Scroll direction and speed (Vector2) |
| `texturePropertyName` | Shader property ("_BaseMap" for URP) |
| `materialIndex` | Which material to scroll |

**Usage:**
- X speed = horizontal scroll
- Y speed = vertical scroll
- Typical value: (0, 0.5) for vertical flow

---

### CloudDrift.cs
**Type:** MonoBehaviour
**Attach to:** Cloud objects or parent
**Purpose:** Moves clouds across the sky.

| Field | Description |
|-------|-------------|
| `mode` | Linear or Rotate movement |
| `speed` | Movement/rotation speed |
| `moveDirection` | Direction for Linear mode |
| `wrapAround` | Teleport when reaching distance |
| `wrapDistance` | Distance before wrapping |
| `pivotPoint` | Center for Rotate mode |
| `rotationAxis` | Axis for rotation (default: Y) |

**Modes:**
- `Linear` - Move in straight line, wrap around
- `Rotate` - Orbit around pivot point

---

## 5. Audio System

Located in: `Assets/MyProject/myScripts/Audio/`

Proximity-based ambient audio with ducking.

### AmbientSoundManager.cs
**Type:** MonoBehaviour (Singleton)
**Requires:** AudioSource
**Attach to:** AmbientSoundManager GameObject
**Purpose:** Plays looping ambient audio, supports volume ducking.

| Field | Description |
|-------|-------------|
| `ambientAudioSource` | AudioSource for ambient track |
| `baseVolume` | Normal volume (0-1) |
| `minDuckedVolume` | Minimum volume when fully ducked |
| `duckLerpSpeed` | Speed of volume transitions |

**Public Methods:**
- `SetDuckAmount(float)` - Set duck amount (0-1)
- `SetVolumeImmediate(float)` - Set volume instantly

**Access via:** `AmbientSoundManager.Instance`

---

### ProximityAudioZone.cs
**Type:** MonoBehaviour
**Requires:** AudioSource
**Attach to:** Sound-emitting objects (fires, streams, etc.)
**Purpose:** Creates proximity-based audio that fades with distance.

| Field | Description |
|-------|-------------|
| `audioSource` | AudioSource for this zone |
| `radius` | Fade distance |
| `maxVolume` | Volume at center (0-1) |
| `playerTag` | Tag to find player |

**Properties:**
- `CurrentVolumeRatio` - Current volume ratio (0-1)
- `Radius` - Zone radius
- `MaxVolume` - Maximum volume

**Example Settings:**
| Location | Radius | Max Volume |
|----------|--------|------------|
| Campfire | 10 | 0.35 |
| Stream | 20 | 0.6 |
| Market | 25 | 0.9 |

---

### AudioZoneDucker.cs
**Type:** MonoBehaviour
**Attach to:** Same object as AmbientSoundManager
**Purpose:** Monitors zones and ducks ambient audio based on loudest nearby zone.

| Field | Description |
|-------|-------------|
| `audioZones` | List of zones (auto-populated) |
| `duckingIntensity` | Multiplier for duck amount |
| `autoFindZones` | Auto-find zones in Start() |

**Public Methods:**
- `FindAllAudioZones()` - Refresh zone list
- `RegisterZone(ProximityAudioZone)` - Add zone manually
- `UnregisterZone(ProximityAudioZone)` - Remove zone
- `CleanupNullZones()` - Remove destroyed zones

---

## 6. UI System

Located in: `Assets/MyProject/myScripts/UI/`

Menu and pause functionality.

### MainMenu.cs
**Type:** MonoBehaviour
**Attach to:** MainMenuManager GameObject in menu scene
**Purpose:** Handles main menu button actions.

**Public Methods:**
- `StartGame()` - Loads "GameScene1"
- `ExitGame()` - Quits application

**Button Setup:**
1. Create button
2. In OnClick(), assign MainMenu object
3. Select appropriate method

---

### PauseMenu.cs
**Type:** MonoBehaviour
**Attach to:** PauseMenuManager GameObject in game scene
**Purpose:** Handles pause functionality with Escape key.

| Field | Description |
|-------|-------------|
| `pauseMenuPanel` | Panel to show/hide |

**Public Methods:**
- `PauseGame()` - Pause and show menu
- `ResumeGame()` - Resume and hide menu
- `GoToMainMenu()` - Load main menu scene
- `ExitGame()` - Quit application

**Features:**
- Escape key toggles pause
- Freezes time (Time.timeScale = 0)
- Disables PlayerInput (stops camera)
- Unlocks cursor for menu navigation

**Properties:**
- `IsPaused` - Current pause state

---

## Project Structure

```
Assets/MyProject/myScripts/
├── Interact/
│   ├── IInteractable.cs
│   ├── Interactor.cs
│   ├── InteractorUI.cs
│   ├── Door.cs
│   ├── Chest.cs
│   ├── Lever.cs
│   ├── CastleGate.cs
│   └── TextSign.cs
├── Dialogue/
│   ├── DialogueData.cs
│   ├── DialogueManager.cs
│   ├── InteractableNPC.cs
│   └── ProximityNPC.cs
├── Animals/
│   ├── AnimalData.cs
│   └── AnimalController.cs
├── Ambient/
│   ├── ScrollingWater.cs
│   └── CloudDrift.cs
├── Audio/
│   ├── AmbientSoundManager.cs
│   ├── ProximityAudioZone.cs
│   └── AudioZoneDucker.cs
└── UI/
    ├── MainMenu.cs
    └── PauseMenu.cs
```

---

## Dependencies

- **Unity 6** (6000.2.8f1)
- **Universal Render Pipeline** (URP 17.2.0)
- **Input System** (1.14.2)
- **AI Navigation** (2.0.9)
- **TextMeshPro**
- **Cinemachine** (2.10.4)

---

## Controls

| Key | Action |
|-----|--------|
| WASD | Move |
| Mouse | Look |
| E | Interact / Advance dialogue |
| Escape | Pause menu |
| Shift | Sprint |
| Space | Jump |

---

*Documentation generated for Medieval Village VR Assignment*
