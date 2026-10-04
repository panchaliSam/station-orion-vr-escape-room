# Member D Guide: Mission System, UI, and Sound

Project: Station Orion: VR Escape Room  
Unity version: 6000.5.11f1  
Role: Build the countdown timer, station status system, UI screens, first-minute guidance, endings, alarms, beeps, and mission logic.

## 1. What You Are Responsible For

Your job is to connect the whole escape room into one playable story.

You are responsible for:

- 10-minute countdown timer
- Station status panel
- Power, Coolant, Access, and Reactor status
- First-minute instructions
- Success ending
- Reactor meltdown ending
- Alarm sound
- Beeps and task feedback sounds
- Mission complete message
- Try again button if possible

You should build this in your own test scene first, then provide prefabs and scripts to Member A.

## 2. Install the Required Tools

Install:

1. Git
2. GitHub Desktop
3. Unity Hub
4. Unity Editor `6000.5.11f1`
5. Windows Build Support module

Everyone must use Unity `6000.5.11f1`.

## 3. Get the Project from GitHub

Using GitHub Desktop:

1. Open GitHub Desktop.
2. Sign in.
3. Choose `File > Clone Repository`.
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
4. Open using Unity `6000.5.11f1`.

Do not upgrade or downgrade the project version without the team.

## 5. Create Your Git Branch

In GitHub Desktop:

1. Click `Current Branch`.
2. Click `New Branch`.
3. Name it `feature/mission-ui-sound`.

In Terminal:

```bash
git checkout -b feature/mission-ui-sound
```

## 6. Create Your Test Scene

Create:

```text
Assets/Scenes/Test_MissionSystem.unity
```

In the scene, add:

- A simple wall or screen
- Timer UI
- Status panel UI
- Buttons or test objects that simulate task completion
- Alarm audio source
- Success and failure panels

## 7. Mission Flow

The mission should follow this order:

1. Start game.
2. Timer begins at 10:00.
3. Status shows:
   - Power: Offline
   - Coolant: Offline
   - Access: Locked
   - Reactor: Critical
4. Player completes power task.
5. Power becomes OK.
6. Door 1 opens.
7. Player completes coolant and keycard tasks.
8. Coolant becomes OK and Access becomes OK.
9. Door 2 opens.
10. Player presses emergency button.
11. Player reaches escape pod.
12. Timer stops.
13. Success message appears.

If timer reaches zero:

```text
Reactor meltdown
Try again
```

## 8. First-Minute Guidance

The first minute is very important.

Use short instructions:

```text
Engineer, the reactor is overloading.
Restore power first.
Grab the power cell.
Place it in the glowing socket.
```

Rules:

- Spoken instruction should also appear as text.
- One goal should glow at a time.
- If the player does nothing for 20 seconds, repeat the instruction.
- Keep each instruction short.

## 9. UI Prefabs to Create

Create these prefabs:

```text
Assets/Prefabs/UI/TimerUI.prefab
Assets/Prefabs/UI/StatusPanel.prefab
Assets/Prefabs/UI/InstructionScreen.prefab
Assets/Prefabs/UI/EndingPanel.prefab
```

Create this mission prefab:

```text
Assets/Prefabs/Mission/MissionSystem.prefab
```

If the `Mission` folder does not exist, create it.

## 10. Sound Guidelines

Use sound for feedback:

- Alarm sound during danger
- Softer alarm after progress
- Beep when looking at or approaching a task
- Clunk when object is placed correctly
- Success sound at the end

Remember to log every downloaded sound in the credits table.

## 11. Beginner Git Workflow

Before working:

```bash
git pull
```

After finishing a useful step:

```bash
git status
git add .
git commit -m "Add countdown timer UI"
git push
```

Meaning:

- `pull` gets latest changes from GitHub.
- `add` prepares your files.
- `commit` saves your work locally.
- `push` uploads your work to GitHub.

In GitHub Desktop:

1. Fetch origin.
2. Pull origin.
3. Make Unity changes.
4. Review changed files.
5. Commit with a clear message.
6. Push origin.

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

Always commit `.meta` files.

## 13. How to Work with Other Members

You need signals from Member C's interaction prefabs.

Examples:

- Power cell placed correctly
- All coolant canisters placed correctly
- Keycard scanned
- Emergency button pressed

You need to tell Member A what your mission system controls:

- Door 1 opening
- Door 2 opening
- Red lights changing to normal lights
- Alarm volume reducing
- Success or failure ending

## 14. Your Completion Checklist

Your part is done when:

- Timer starts at 10:00.
- Timer counts down correctly.
- Status panel updates after each task.
- First-minute instructions work.
- Success ending appears when the mission is complete.
- Meltdown ending appears when time reaches zero.
- Audio feedback works.
- Mission system is saved as a prefab.
- Member A can place your prefabs in the main scene.
