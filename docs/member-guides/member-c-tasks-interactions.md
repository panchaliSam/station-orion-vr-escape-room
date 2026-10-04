# Member C Guide: Tasks and Interactions

Project: Station Orion: VR Escape Room  
Unity version: 6000.5.11f1  
Role: Build the power cell, coolant canisters, keycard, sockets, emergency button interaction support, physics behavior, and interaction prefabs.

## 1. What You Are Responsible For

Your job is to create the physical puzzle actions.

You are responsible for:

- Power cell object
- Power cell socket
- Red, blue, and green coolant canisters
- Matching coolant sockets
- ID keycard
- Keycard scanner
- Physics and collision setup
- Grab interaction setup
- Feedback triggers for correct placement

You should build each task in a test scene first, then save it as a prefab for Member A.

## 2. Install the Required Tools

Install:

1. Git
2. GitHub Desktop
3. Unity Hub
4. Unity Editor `6000.5.11f1`
5. Windows Build Support module

Everyone must use Unity `6000.5.11f1` so scenes and prefabs stay compatible.

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

Do not open it in a different Unity version.

## 5. Create Your Git Branch

In GitHub Desktop:

1. Click `Current Branch`.
2. Click `New Branch`.
3. Name it `feature/interactions`.

In Terminal:

```bash
git checkout -b feature/interactions
```

## 6. Unity Package Setup

Check that XR Interaction Toolkit is installed:

1. Open `Window > Package Manager`.
2. Select `XR Interaction Toolkit`.
3. Import `Starter Assets`.
4. Import `XR Device Simulator` if it is not already imported.

You need these so grab interactions can be tested without a headset.

## 7. Create Your Test Scene

Create:

```text
Assets/Scenes/Test_Interactions.unity
```

In this scene, add:

- A floor
- Simple table
- XR player prefab from Member B if available
- Power cell and socket
- Coolant canisters and sockets
- Keycard and scanner

## 8. Build the Power Cell Task

The player should:

1. See the glowing power cell.
2. Grab it.
3. Move it to the matching socket.
4. Drop it into the socket.
5. Receive feedback.

Feedback should include:

- Object snaps or sits correctly
- Sound effect
- Light change or glow
- Status signal to mission system

Save as:

```text
Assets/Prefabs/Interactions/PowerCellTask.prefab
```

## 9. Build the Coolant Task

The player should place:

- Red canister into red socket
- Blue canister into blue socket
- Green canister into green socket

Each correct placement should count as progress:

```text
1/3 coolant restored
2/3 coolant restored
3/3 coolant restored
```

Save as:

```text
Assets/Prefabs/Interactions/CoolantTask.prefab
```

## 10. Build the Keycard Task

The player should:

1. Grab the keycard.
2. Place or tap it on the scanner.
3. Unlock access for the next door.

Save as:

```text
Assets/Prefabs/Interactions/KeycardTask.prefab
```

## 11. Beginner Git Workflow

Before starting work:

```bash
git pull
```

After finishing a useful step:

```bash
git status
git add .
git commit -m "Add power cell socket interaction"
git push
```

Meaning:

- `git status` shows changed files.
- `git add .` prepares files for saving.
- `git commit` saves a checkpoint.
- `git push` uploads to GitHub.

In GitHub Desktop, use:

1. `Fetch origin`
2. `Pull origin`
3. Make Unity changes
4. Write commit message
5. Commit
6. Push

## 12. Unity Files You Must Commit

Commit:

```text
Assets/
Packages/
ProjectSettings/
```

Do not commit:

```text
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
```

Always commit `.meta` files. They are important.

## 13. Your Completion Checklist

Your part is done when:

- The power cell can be grabbed and placed.
- The coolant canisters match their colored sockets.
- The keycard works with the scanner.
- Each interaction gives visual, audio, or physical feedback.
- All tasks are saved as prefabs.
- Your prefabs work inside `Test_Interactions.unity`.
- Member A can add your prefabs to the main scene.
- Member D can connect your task events to the mission system.
