# Member B Guide: Player and Movement

Project: Station Orion: VR Escape Room  
Unity version: 6000.5.11f1  
Role: Build the XR player, teleport movement, snap turn, comfort setup, simulator testing, keyboard fallback, and Windows build testing.

## 1. What You Are Responsible For

Your job is to make sure the player can move comfortably in VR.

You are responsible for:

- XR Origin
- Camera position
- Hand controllers
- Teleport movement
- Snap turning
- XR Device Simulator
- Comfort settings
- Basic keyboard or simulator fallback
- Windows build test

You should work in your own test scene first. Do not edit the main scene unless Member A asks you to help.

## 2. Install the Required Tools

Install:

1. Git
2. GitHub Desktop
3. Unity Hub
4. Unity Editor `6000.5.11f1`
5. Windows Build Support module

In Unity Hub:

1. Open `Installs`.
2. Install Unity `6000.5.11f1`.
3. Add `Windows Build Support`.
4. Open the project only with this version.

## 3. Get the Project from GitHub

Using GitHub Desktop:

1. Open GitHub Desktop.
2. Sign in.
3. Click `File > Clone Repository`.
4. Clone `panchaliSam/station-orion-vr-escape-room`.

Using Terminal:

```bash
git clone https://github.com/panchaliSam/station-orion-vr-escape-room.git
cd station-orion-vr-escape-room
```

## 4. Open the Project in Unity

1. Open Unity Hub.
2. Click `Add`.
3. Select the project folder.
4. Open with Unity `6000.5.11f1`.

If Unity asks to change version, do not continue until the group agrees.

## 5. Create Your Git Branch

In GitHub Desktop:

1. Click `Current Branch`.
2. Click `New Branch`.
3. Use the name `feature/player-movement`.

In Terminal:

```bash
git checkout -b feature/player-movement
```

## 6. Unity Packages You Need

Use the Unity VR template if possible. It should include XR tools already.

Check:

1. Open `Window > Package Manager`.
2. Look for `XR Interaction Toolkit`.
3. Open its `Samples` tab.
4. Import:
   - Starter Assets
   - XR Device Simulator

If Unity asks to enable the new Input System, accept it and restart Unity.

## 7. Create Your Test Scene

Create:

```text
Assets/Scenes/Test_PlayerMovement.unity
```

In the scene:

1. Add a floor.
2. Add a few cubes as obstacles.
3. Add teleport areas.
4. Add the XR Origin.
5. Add the XR Device Simulator.
6. Press Play and test movement.

## 8. Movement Rules for This Project

Use comfort-first movement:

- Teleport movement
- Snap turn
- No smooth walking
- No forced camera movement
- Floor-referenced standing origin

This supports the design rationale: reduce cybersickness and keep the player comfortable.

## 9. Create the Player Prefab

When movement works, create a prefab:

```text
Assets/Prefabs/Player/XR_Player.prefab
```

The prefab should include:

- XR Origin
- Camera
- Left hand controller
- Right hand controller
- Teleport interactor
- Ray interactor if needed
- Snap turn provider
- Input action manager

Do not include the entire test scene as your final work. Member A needs the prefab.

## 10. Daily Git Workflow

Before working:

```bash
git pull
```

After working:

```bash
git status
git add .
git commit -m "Add XR teleport player prefab"
git push
```

In GitHub Desktop:

1. Fetch and pull before starting.
2. Make your Unity changes.
3. Review the changed files.
4. Commit with a clear message.
5. Push your branch.

## 11. Beginner Git Rules

Remember these:

- `pull` means download the latest team changes.
- `commit` means save a checkpoint on your computer.
- `push` means upload your commits to GitHub.
- `branch` means your own workspace.
- `pull request` means asking the team to merge your work.

Do not commit `Library`, `Temp`, or `Build` folders.

Always commit Unity `.meta` files.

## 12. Windows Build Test

Near the end:

1. Go to `File > Build Profiles`.
2. Choose Windows.
3. Add the main scene.
4. Build the project.
5. Test that the `.exe` opens without Unity.

If the build fails, write down the error and share it with the group.

## 13. Your Completion Checklist

Your part is done when:

- The player can teleport.
- Snap turn works.
- The XR Device Simulator works in Play mode.
- The player starts at correct floor height.
- The player can reach and grab interactable objects.
- The player prefab is saved in `Assets/Prefabs/Player/`.
- Member A can place your prefab into the main scene.
- A Windows build can run.
