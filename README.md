# Station Orion: VR Escape Room

A Unity XR escape room where the player repairs the reactor on Station Orion and escapes using VR interaction, physics, spatial UI, and audio feedback.

## Project Summary

Station Orion is a single-player VR escape room. The player is the last engineer on a space station during a reactor emergency. They have 10 minutes to restore station systems, unlock each room, and reach the escape pod.

Core flow:

1. Control Room: restore power by placing the power cell in its socket.
2. Engine Room: place red, blue, and green coolant canisters in matching sockets, then scan the ID keycard.
3. Reactor Room: press the emergency button and escape before the timer reaches zero.

## Unity Version

Use this exact Unity version:

```text
Unity 6000.5.11f1
```

Everyone in the group should use the same version to avoid scene, package, and prefab conflicts.

Required Unity modules:

- Windows Build Support
- XR Interaction Toolkit
- OpenXR
- XR Device Simulator

## Repository Setup

Clone the repository:

```bash
git clone https://github.com/panchaliSam/station-orion-vr-escape-room.git
cd station-orion-vr-escape-room
```

Open the project in Unity Hub using Unity `6000.5.11f1`.

In Unity, set:

```text
Edit > Project Settings > Editor
Version Control Mode: Visible Meta Files
Asset Serialization Mode: Force Text
```

## Git Rules

Always pull before starting work:

```bash
git pull
```

Commit after a useful working step:

```bash
git status
git add .
git commit -m "Describe the change clearly"
git push
```

Do not commit Unity generated folders such as:

```text
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
```

Do commit:

```text
Assets/
Packages/
ProjectSettings/
```

Always commit Unity `.meta` files.

## Team Workflow

Only one person should edit the main scene. Other members should build their work as prefabs in separate test scenes.

Recommended branches:

```text
feature/station-environment
feature/player-movement
feature/interactions
feature/mission-ui-sound
```

Recommended scene ownership:

- Member A edits `Assets/Scenes/Main_StationOrion.unity`.
- Member B works in `Assets/Scenes/Test_PlayerMovement.unity`.
- Member C works in `Assets/Scenes/Test_Interactions.unity`.
- Member D works in `Assets/Scenes/Test_MissionSystem.unity`.

## Member Guides

Each team member has a separate beginner guide:

- [Member A: Station Builder](docs/member-guides/member-a-station-builder.md)
- [Member B: Player and Movement](docs/member-guides/member-b-player-movement.md)
- [Member C: Tasks and Interactions](docs/member-guides/member-c-tasks-interactions.md)
- [Member D: Mission System, UI, and Sound](docs/member-guides/member-d-mission-ui-sound.md)

## Suggested Unity Folder Structure

```text
Assets/
  Scenes/
  Prefabs/
    Player/
    Environment/
    Interactions/
    Mission/
    UI/
  Scripts/
  Materials/
  Models/
  Audio/
```

## Demo Goal

The final demo should show:

- A clear first minute with spoken and written instructions.
- Teleport movement and snap turning.
- Grab and place interactions.
- Power, coolant, access, and reactor status updates.
- A 10-minute countdown timer.
- Success and meltdown endings.
- A Windows build that runs without Unity open.
