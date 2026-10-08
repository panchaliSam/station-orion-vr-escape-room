# Station Orion – Game Systems (scripts)

All scripts are in `Assets/Scripts/StationOrion`. They are added to the scene with one click:
**Station Orion > Build Game Systems (open scene)** (open `Main_StationOrion` first, then save with Ctrl+S).
Running it again rebuilds everything under `_StationOrion_Generated`. Ctrl+Z undoes it.

## Game flow

| Stage | Player does | Game responds |
|---|---|---|
| Intro (7 s) | Reads the wall screen | Alarm siren, red flashing lights, rooms dimmed |
| Power | Puts the yellow power cell on the yellow slot | Lights on in Room 1, Engine Room door opens, Engine Room floor unlocked for teleport |
| Coolant | Finds the 3 canisters and puts each on the pad of the same colour | Pads turn green, counter 1/3 → 3/3, Room 2 lights on, **shutdown code is revealed on the wall panel** |
| Access | Finds the keycard (on a crate) and places it on the scanner beside the Reactor Room door | Reactor Room door opens, Room 3 floor unlocked |
| Code | Types the 4-digit code on the keypad (random every game) | Wrong code = buzz + DENIED, right code = ACCEPTED |
| Shutdown | Presses the red emergency button | Alarm stops, lights go green, reactor core turns calm blue and slows down |
| Escape | Teleports into the glowing escape pod | MISSION COMPLETE with time left |
| Fail | Timer reaches 00:00 | REACTOR MELTDOWN, game restarts after 10 s |

F5 restarts at any time (keyboard).

## Scripts (who explains what)

| Script | What it does | Lecture link |
|---|---|---|
| `MissionManager` | Story stages, 10-min timer, wall screens, alarm, lights, doors, teleport locking, objective marker, idle hints, endings | Advanced feature; clear beginning/middle/end (brief §4) |
| `SocketTask` | Turns an XR Socket Interactor into a task; pad glow (idle / pulsing / green / red), clunk + beep, wrong-item buzz | Discovery → Action → Feedback loop (Week 2); affordances |
| `PressButton` | Physical 3D button pressed with point + GRIP; moves in and clicks | Physical modality, feedback |
| `Keypad` | 4-digit code puzzle, random code each game | Interactivity; puzzle cohesion |
| `DoorController` | Slides doors open with a sound | Feedback; spatial audio |
| `Glow` | Emissive glow / pulse to show "do this next" | Visual affordance |
| `Billboard` | Labels always turn to face the player | Spatial UI readability |
| `Spin` | Reactor ring rotation, marker bobbing | Environmental feedback |
| `SOAudio` | All sounds are generated in code and played as 3D sound | Spatial audio requirement; no licensing needed |
| `Editor/StationOrionSetup` | One-click builder for all of the above | – |

## Guidance for a first-time player (first minute)

1. Wall screen: "WARNING: REACTOR OVERLOAD" + what to do, alarm sound, red lights.
2. HOW TO PLAY panel near the start: teleport / grab / press.
3. A floating cyan diamond hovers over the next thing to grab; when you pick it up it moves to where it goes.
4. The target item and its slot pulse; done slots turn green; wrong items flash red with a buzz.
5. After 25 s with no progress, a HINT appears on the screens with a beep.
