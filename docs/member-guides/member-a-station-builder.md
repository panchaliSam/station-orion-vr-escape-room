# Member A Guide: Station Builder

Project: Station Orion: VR Escape Room  
Unity version: 6000.5.11f1  
Role: Build the three rooms, doors, lighting, main scene, simple environment assets, and credits board.

## 1. What You Are Responsible For

You are the only person who edits the main playable scene.

Your main job is to create the space station layout:

- Room 1: Control Room
- Room 2: Engine Room
- Room 3: Reactor Room
- Doors between rooms
- Lighting and color mood
- Basic walls, floors, tables, shelves, panels, and signs
- Credits board
- Final combined scene using prefabs from the other members

Important rule: do not build every interaction yourself. Members B, C, and D create their parts as prefabs in their own test scenes. You place those prefabs into the main scene.

## 2. Install the Required Tools

Install these before starting:

1. Git
2. GitHub Desktop
3. Unity Hub
4. Unity Editor `6000.5.11f1`
5. Windows Build Support module for Unity

In Unity Hub:

1. Open `Installs`.
2. Click `Install Editor`.
3. Choose Unity `6000.5.11f1`.
4. Add `Windows Build Support`.
5. Finish installation.

If the version is not shown in Unity Hub, use the Unity download archive and open it through Unity Hub.

## 3. Get the Project from GitHub

Use GitHub Desktop if you are new to Git.

1. Open GitHub Desktop.
2. Sign in to GitHub.
3. Choose `File > Clone Repository`.
4. Select `panchaliSam/station-orion-vr-escape-room`.
5. Choose a folder on your computer.
6. Click `Clone`.

If using Terminal:

```bash
git clone https://github.com/panchaliSam/station-orion-vr-escape-room.git
cd station-orion-vr-escape-room
```

## 4. Open the Project in Unity

1. Open Unity Hub.
2. Click `Add`.
3. Select the `station-orion-vr-escape-room` folder.
4. Open it using Unity `6000.5.11f1`.

If Unity asks to upgrade or change version, stop and check with the team first. Everyone must use the same Unity version.

## 5. Unity Settings for Git

In Unity:

1. Go to `Edit > Project Settings > Editor`.
2. Set `Version Control Mode` to `Visible Meta Files`.
3. Set `Asset Serialization Mode` to `Force Text`.

This makes Unity safer for Git.

## 6. Folder Structure You Should Create

Inside Unity's `Assets` folder, create:

```text
Assets/
  Scenes/
  Prefabs/
    Environment/
    Doors/
  Materials/
  Models/
  Audio/
  Scripts/
  UI/
```

Your main scene should be:

```text
Assets/Scenes/Main_StationOrion.unity
```

## 7. Create Your Git Branch

Before working, create your own branch.

In GitHub Desktop:

1. Click `Current Branch`.
2. Click `New Branch`.
3. Name it `feature/station-environment`.
4. Click `Create Branch`.

In Terminal:

```bash
git checkout -b feature/station-environment
```

## 8. Build the Main Scene

Start simple. Use cubes first.

1. Create a new scene.
2. Save it as `Assets/Scenes/Main_StationOrion.unity`.
3. Create three small connected rooms.
4. Add doors between the rooms.
5. Add a shelf and socket area in Room 1.
6. Add canister sockets and scanner area in Room 2.
7. Add emergency button area and escape pod zone in Room 3.
8. Add basic lights:
   - Red lights for danger
   - White or blue lights for normal station lighting
   - Green lights for success

Do not worry about beautiful assets first. A working greybox is better than a pretty broken scene.

## 9. How to Add Other Members' Work

Other members should give you prefabs, not random scene changes.

Examples:

- Member B gives `XR_Player.prefab`
- Member C gives `PowerCellTask.prefab`, `CoolantTask.prefab`, `KeycardTask.prefab`
- Member D gives `MissionSystem.prefab`, `StatusPanel.prefab`, `TimerUI.prefab`

To add them:

1. Pull the latest Git changes.
2. Open `Main_StationOrion.unity`.
3. Drag their prefabs into the correct room.
4. Press Play.
5. Check that the full flow still works.

## 10. Daily Git Workflow

Before starting:

```bash
git pull
```

After finishing:

```bash
git status
git add .
git commit -m "Build station room layout"
git push
```

In GitHub Desktop:

1. Click `Fetch origin`.
2. Click `Pull origin` if shown.
3. Make your Unity changes.
4. Return to GitHub Desktop.
5. Review changed files.
6. Write a short commit message.
7. Click `Commit to feature/station-environment`.
8. Click `Push origin`.

## 11. What Not to Commit

Never commit these folders:

```text
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
```

Commit these:

```text
Assets/
Packages/
ProjectSettings/
```

Always commit `.meta` files. Unity needs them.

## 12. Your Completion Checklist

Your part is done when:

- The main scene opens without errors.
- The player starts in Room 1 facing the screen.
- The three rooms are connected in story order.
- Doors are placed and ready to be controlled.
- Lighting changes can be connected to the mission system.
- All teammate prefabs are placed correctly.
- The credits board exists.
- The game can be played from start to finish.
