# Medieval Village – VR Assignment (Unity)
## Project Guide for Claude CLI

This file tracks every task needed to achieve a 10/10 on the Virtual Reality assignment.
Work through tasks **one at a time**. Mark each task `[x]` when done.

---

## Project Context

- **Engine:** Unity (Starter Assets + SYNTY polygon packs)
- **Language:** C#
- **Scene:** Medieval village with mountains, stream, flowers, castle, church, graveyard
- **No enemies, no health bars, no combat system**

---

## Task List

### 🎮 1. Character Controller
- [ ] Import Unity Starter Assets (Third Person or First Person — decide which)
- [ ] Configure movement speed, mouse sensitivity, camera settings
- [ ] Add a small crosshair UI element (Canvas > Image)
- [ ] Test that the player cannot walk through walls or terrain
- [ ] Make the player character invisible if using third-person (or set up properly)

---

### 💬 2. NPC Dialogue System
- [ ] Create `DialogueData.cs` — ScriptableObject holding NPC name + array of dialogue lines
- [ ] Create `NPCDialogue.cs` — MonoBehaviour on each NPC, holds reference to its DialogueData
- [ ] Create `DialogueManager.cs` — singleton that shows/hides the dialogue UI panel
- [ ] Create `DialogueUI` Canvas — panel with NPC name text, dialogue line text, "Press E to continue" prompt
- [ ] Wire up input: player presses E when in range → dialogue opens, E advances lines, closes at end
- [ ] Place and configure at least 4 NPCs with unique dialogue:
  - [ ] Blacksmith
  - [ ] Priest (near church)
  - [ ] Gravedigger (near graveyard)
  - [ ] Villager / Guard near castle
- [ ] Disable player movement while dialogue is open
- [ ] Add a proximity trigger (sphere collider, is trigger) to each NPC with a "Press E" hint UI

---

### 🚪 3. Interactable Objects
- [ ] Create abstract `Interactable.cs` base class with `virtual void Interact()` method
- [ ] Create `Door.cs` extending Interactable — animates open/close via coroutine (rotate on Y axis)
- [ ] Create `Chest.cs` extending Interactable — animates lid open/close
- [ ] Create `InteractionRaycaster.cs` on player camera — raycasts forward, detects Interactable layer, shows "Press E" prompt, calls Interact()
- [ ] Place interactables in scene:
  - [ ] Church front door
  - [ ] Castle gate or door
  - [ ] At least one chest (graveyard or castle interior)
- [ ] Make sure all interactables have correct colliders and are on `Interactable` layer

---

### 🌿 4. Ambient Animations (critical for "dynamic world" criterion)
- [ ] **Torches** — add particle system (fire + embers) to torch objects around the village
- [ ] **Stream** — apply animated water shader/material to the stream mesh (SYNTY water or Unity's built-in)
- [ ] **Birds** — create 2–3 birds on a looping `iTween` or `Vector3.Lerp` patrol path above the village
- [ ] **Clouds** — slow-moving cloud objects across the skybox (translate on X axis in `Update`)
- [ ] **Church Bell** — periodic swinging animation via coroutine (rotate bell bone or object back and forth)
- [ ] **Flowers/Grass sway** — apply a simple sine-wave position offset script `FloatSway.cs` to flower groups, OR use a wind zone with terrain detail

---

### ⚡ 5. Optimization
- [ ] Mark all static environment objects (buildings, walls, ground, trees) as **Static** in Inspector
- [ ] **Bake Occlusion Culling** (Window > Rendering > Occlusion Culling > Bake)
- [ ] Add **LOD Groups** to the castle and church (LOD0 full detail, LOD1 reduced, LOD2 very low or billboard)
- [ ] Enable **Static Batching** (Player Settings > Other Settings — should be on by default)
- [ ] Check Stats window in Game view — aim for reasonable draw calls
- [ ] Document the optimization steps taken (for the written report)

---

### 💡 6. Lighting
- [ ] Set Directional Light as the sun with warm color (slightly orange/yellow)
- [ ] Set Lighting Mode to **Mixed** for main light
- [ ] Bake lightmaps (Window > Rendering > Lighting > Generate Lighting)
- [ ] Add Point Lights near all torches (warm orange, low range)
- [ ] Add a cool-toned fill light inside the church interior
- [ ] Configure skybox (HDRI or gradient — something atmospheric, overcast or golden hour)

---

### 🌙 7. Day/Night Cycle *(optional but recommended for originality)*
- [ ] Create `DayNightCycle.cs` — rotates Directional Light on X axis over time
- [ ] Lerp sun color between warm day color and cool night color
- [ ] Lerp ambient light intensity
- [ ] Swap skybox material or tint based on time of day (optional extra)
- [ ] Make torch point lights activate/intensify at night

---

### 📋 8. In-World Asset Credits (required by assignment)
- [ ] Create a noticeboard or sign object in the village square
- [ ] Add a readable texture or UI WorldSpace Canvas listing:
  - SYNTY polygon pack name(s) and source URL
  - Unity Starter Assets
  - Any other imported assets
- [ ] Alternatively: accessible pause menu with a "Credits / Sources" tab

---

### 📄 9. Documentation & Submission
- [ ] **PowerPoint** (10–15 slides): overview, scene screenshots, feature highlights, optimization notes
- [ ] **Short video** (5–10 min): walkthrough of the village, demonstrate all features — NPC dialogue, doors, animations, day/night
- [ ] **Written manual** containing:
  - [ ] Introduction
  - [ ] Problem description
  - [ ] Analysis → Design → Implementation phases
  - [ ] Table of all C# scripts with 4–5 line descriptions each
  - [ ] Detailed breakdown of the 4–5 most important scripts
  - [ ] Asset sources list
  - [ ] User manual (controls, how to interact)
  - [ ] Screenshots
  - [ ] Section answering each grading criterion explicitly
- [ ] **Build** the project (File > Build Settings > PC build)
- [ ] Upload project + build + docs to Dropbox/Drive, submit link to https://thales.cs.unipi.gr

---

## Script Architecture Overview

```
Assets/
└── Scripts/
    ├── Player/
    │   ├── InteractionRaycaster.cs   ← detects and triggers Interactables
    │   └── PlayerMovementLock.cs     ← disables movement during dialogue
    ├── Dialogue/
    │   ├── DialogueData.cs           ← ScriptableObject (NPC name + lines)
    │   ├── NPCDialogue.cs            ← on each NPC, holds DialogueData ref
    │   └── DialogueManager.cs        ← singleton, controls UI panel
    ├── Interactables/
    │   ├── Interactable.cs           ← abstract base class
    │   ├── Door.cs                   ← open/close animation
    │   └── Chest.cs                  ← open/close animation
    ├── Ambient/
    │   ├── FloatSway.cs              ← sine wave sway for flowers/grass
    │   ├── BirdPatrol.cs             ← simple looping flight path
    │   └── CloudDrift.cs             ← slow X-axis cloud movement
    └── World/
        └── DayNightCycle.cs          ← sun rotation + color lerp
```

---

## Controls (for user manual)

| Key | Action |
|-----|--------|
| WASD | Move |
| Mouse | Look |
| E | Interact / Advance dialogue |
| Esc | Pause / Credits menu |

---

## Notes & Reminders

- **Comment every script** thoroughly — the graders explicitly check this
- Keep scripts **single-responsibility** — one job per script
- The assignment says complexity is NOT automatically rewarded — clean and complete beats ambitious and broken
- Test the build on a clean machine before submitting
