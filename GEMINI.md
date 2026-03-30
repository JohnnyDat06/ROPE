# ROPE - Project Overview & Context

This document provides a comprehensive overview of the **ROPE** game project, its architecture, development standards, and instructions for working within this codebase.

## 🛠 Project Overview
- **Project Type:** 3D Third-Person Shooter/Action Game.
- **Engine:** Unity 6 (`6000.0.69f1`).
- **Render Pipeline:** Universal Render Pipeline (URP) with profiles for PC and Mobile.
- **Main Gameplay Systems:**
    - **Third-Person Movement:** A robust controller supporting normal locomotion and "Strafe Mode" (always face camera direction) when aiming.
    - **Core Management:** Centralized systems for checkpoints, respawning, and UI flow (GameManager).
    - **Input:** Utilizes the Unity New Input System (`InputSystem_Actions.inputactions`).
    - **Performance:** Implements `ObjectPoolManager` for efficient resource handling.

## 📂 Directory Structure (Core)
- `Assets/_Game/`: Main game assets and logic.
    - `Animations/`: Animation clips and controllers.
    - `Art/`: Mesh, textures, and VFX assets.
    - `Data/`: ScriptableObjects for item stats and database.
    - `Prefabs/`: Pre-configured GameObjects (logic + art).
    - `Scenes/`: Game levels (Main scenes and Sandbox for testing).
    - `Scripts/`: C# Source code.
        - `_Core/`: High-level systems (GameManager, Inventory).
        - `_Characters/`: Player and Enemy logic.
        - `_UI/`: HUD, Menus, and tutorials.
        - `_Utils/`: Helper functions and shared utilities.
- `Assets/_ThirdParty/`: Externally sourced assets (ignored in Git LFS for large binaries).
- `Assets/_Tool/`: Editor tools and extensions.
- `Assets/Settings/`: Configuration for Input, URP, and Post-processing.

## 💻 Technical Implementation
### Core Systems
- **GameManager:** Singleton pattern managing spawn points, checkpoints, and global UI panels (GameOver/Tutorial).
- **ThirdPersonController:** Handles locomotion, strafing, jump/gravity, and Cinemachine camera integration.
- **Input Management:** Uses `StarterAssetsInputs` to bridge the New Input System and gameplay logic.

### Development Conventions (Mandatory)
- **Code Style:**
    - Always use **Namespaces** (e.g., `DatScript`, `StarterAssets`).
    - Use **PascalCase** for Script names and Public methods/properties.
- **Naming Conventions (Assets):**
    - **Textures:** Use suffixes (`_D`: Albedo, `_N`: Normal, `_E`: Emission, `_M`: Metallic, `_Icon`: UI).
    - **Materials:** Prefix with `M_`.
    - **Animations:** Named as `Object_Action` (e.g., `Hero_Attack_01`).
- **Scene Handling:**
    - Minimize changes to `.unity` files to avoid binary merge conflicts.
    - **Prefer Prefabs:** Work on Prefabs whenever possible and instantiate them in scenes.

## 🚀 Building and Running
- **Primary Environment:** Unity Editor version `6000.0.69f1`.
- **Build Targets:** Configured for PC and Mobile.
- **Key Commands:**
    - `G`: Use/Interact (inferred).
    - `V`: Scan (inferred from Scan folder).
    - `H`: Toggle Tutorial Panel.
    - `Shift`: Sprint.
    - `Space`: Jump.

## 🤝 Contribution Workflow
- **Git Branching:** Use `git pull --rebase` to avoid merge commits.
- **Commits:** Prefix messages with `Feat`, `Fix`, `Refactor`, or `Chore`.
- **Formatting:** Titles < 72 chars, descriptions wrapped at 80 chars.

---
*For more detailed contribution rules, see `CONTRIBUTING.md`.*
