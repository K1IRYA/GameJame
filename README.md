# 🛠️ 2RF (RTTD) - Real-Time Tower Defense

A feature-rich **Real-Time Strategy / Tower Defense (RTS-TD)** hybrid built with Unity. Command dwarf builders, manage local economic constraints, construct fortified boundary walls, deploy strategic defenses, and control your army using classic military micro-management tools.

---

## 🚀 Core Features & Implementation Details

### 🏗️ Advanced Economy & Construction Systems
* **Resource-Bound UI Management:** Built-in `BuildingCostUI` dynamically maps out structure requirements (Wood, Scrap Metal, Iron Ore) via flexible inspector lists. Material counters automatically recolor to red and toggle button click states (`interactable = false`) in real-time when global assets run dry.
* **Autonomous Builder Chaining:** Builder units feature proactive task-chaining. When completing a construction assignment, the builder automatically surveys a 35-meter radius for next-in-line unfinished projects (such as extensive wall boundaries) without relying on micro-intensive player commands.
* **Unique Landmark Restrictions:** Enforced a strict maximum restriction of **one Hub (Town Hall) per level**, blocking any duplicate blueprint placements immediately if a prior foundation exists on the battlefield.

### 🧱 Modular Fence & Snapping Mechanics
* **Procedural Snapping Arrays:** Implemented dual-sided `SnapPoint` detection matrices (`SnapPoint_L` and `SnapPoint_R`), enabling seamless linear edge-snapping and cross-directional connections over any standard 90-degree axis.
* **Visual Randomization:** Complete support for automated aesthetics. Building a fence line randomizes the underlying finalized mesh model array (`zabor1`, `zabor2`, `zabor3`) to ensure environments present natural variation.
* **Persistent Rotation Control:** Allows effortless wall realignment via standard rotation mapping before placing elements.

### ⚔️ RTS Micro-Management & Tactical Controls
* **Attack-Move Mechanics:** Deployed strategic military navigation mapping. Units ordered to move via the **Attack-Move** behavior state (Custom Hotkey + LMB) traverse the area while periodically querying a 10m scan perimeter, automatically switching tasks to pursue and eliminate encountered enemy groups.
* **Adaptive Input Sandbox:** Selection boxes and dragging rects are contextually throttled whenever the player enters building or wall placement modes, cleanly isolating mouse pointer tasks.
* **Scrollable Selection Viewport:** Massive unit management is handled via a vertical `ScrollRect` array paired with custom layout filters, safely capping UI scaling bounds.

### ⚙️ Systems Engineering & Global Persistence
* **Centralized Input Mapping:** Built an interactive **Key Rebinding Manager** using dynamic coroutine keyscan filters. Players can independently map camera pans, attack-move keys, and build adjustments with automatic persistent registry saving (`PlayerPrefs`).
* **Multi-Bus AudioManager:** Implemented a persistent `DontDestroyOnLoad` Singleton mixer. Features auto-binding hook-ins for newly loaded UI buttons, automatic background track loops, and individual master, music, and SFX slider attenuation matrices.
* **Animation-Synced Sound Design:** Uses precisely matched spatial 3D audio parameters tied straight to unit model timeline `AnimationEvent` frame definitions, backed by localized pitch variations to minimize repetitive noise exhaustion.

---

## 🛠️ Project Structure Overview

```text
Assets/
├── 📁 Audio/                 # Environmental soundtracks and pixel SFX elements
├── 📁 Models/                # Character FBX models, skeletal rigs, and mesh details
├── 📁 Prefabs/               # Ground structures, ghosts, sites, and dwarf units
├── 📁 Scenes/                # MainMenu (0) -> SettingsScene (1) -> MainScene (2)
└── 📁 Scripts/
     ├── 📄 InputManager.cs         # Key binding registries and dynamic data mapping
     ├── 📄 AudioManager.cs         # Master Singleton mixer and audio source pool
     ├── 📄 BuildingCostUI.cs       # Contextual economic indicators and button hooks
     ├── 📄 FencePlacer.cs          # Grid vector calculation and snapping engines
     ├── 📄 BuilderUnit.cs          # Smart construction chain tasks and state trees
     └── 📄 RTSController.cs       # Mouse drag tracking, UI locks, and combat states
```

---

## 💻 System Configuration & Development Requirements
* **Game Engine Unity:** Version `2022.3.52f1` (or newer LTS releases)
* **Graphics API Pipeline:** Universal Render Pipeline (URP)
* **Target Build Engine Platform:** Windows standalone execution (`x86_64`)
* **Core Controller Inputs:** Keyboard (WASD Panning / Custom Hotkeys) + Two-Button Optical Mouse

---

## 🚀 Quick-Start Development Guide

1. **Clone the Repository:**
   ```bash
   git clone https://github.com
   ```
2. **Launch via Unity Hub:**
   * Open Unity Hub, click **Add**, and select the cloned repository root folder.
   * Ensure you are launching with the correct editor version (`2022.3.52f1`).
3. **Build Profile Execution Check:**
   * Open `File -> Build Settings...`
   * Confirm the operational list structure matches:
     1. `Scenes/MainMenu` (Index 0)
     2. `Scenes/SettingsScene` (Index 1)
     3. `Scenes/MainScene` (Index 2)
   * Press **Play** or select **Build** to package a custom `.exe` standalone application.
