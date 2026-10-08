# Station Orion – Realistic Station (one-click build)

## How to build it

1. Open the project in Unity (6000.5.11f1). Wait for the scripts to compile, and check that the Console has no red errors.
2. In the top menu, choose **Station Orion → Build Realistic Station (new scene)** and click **Build**.
   - The first build generates the textures. This can take up to a minute.
3. The new scene `Assets/Scenes/StationOrion_Realistic.unity` opens. Press **Play**.
4. Running the menu again rebuilds the scene from scratch. Don't hand-edit this scene; change the builder scripts instead, or make a copy of the scene.

The old `Main_StationOrion` scene is not touched.

## What gets built

| Area | Contents |
|---|---|
| Command Deck (start) | Big window with Earth, solar wings and stars outside. Navigation console with live telemetry screens, pilot seats, holotable with a spinning hologram, crew lockers and a sealed airlock hatch. Mission screen and HOW TO PLAY screen. Spare-parts rack with the **power cell** and the **power distribution unit** (power slot). |
| Corridor A | Portholes. Locked by Door 1 until power is restored. |
| Engine Room | Fusion generator with a spinning fan, and the coolant manifold with **red / blue / green pads**. The **code panel** shows the code after the coolant is stable. Workbench (blue canister), crate stack (red canister) and storage rack (green canister). Security crate with the **keycard**. **ID scanner** beside Door 2. |
| Corridor B | Portholes. Locked by Door 2. |
| Reactor Room (5 m high) | Reactor core in a glass containment column with three spinning rings and a safety railing. Shutdown console with the **keypad**, the **emergency button** and a reactor telemetry screen. Mission screen. Credits board. |
| Escape Pod | Seats and a porthole. The hatch opens only after shutdown. Teleport inside to win. |

## Realism features (all generated in code, no third-party assets)

- Procedural PBR textures with normal maps: sci-fi wall panels, tread-plate floor, perforated ceiling, vents, hazard stripes, cargo crates, screen UI and solar cells.
- A procedural Earth (oceans, continents, ice caps, clouds) and a star-field sky with a Milky Way band.
- Ceiling light panels, cove light strips, structural ribs, beams, pipes and cables.
- Sliding double doors with red/green status lights and hazard frames.
- Lighting: URP post-processing (bloom, ACES tonemapping, colour grading, vignette) and realtime reflection probes.
- Emergency mode: rooms dim and rotating red beacons sweep the walls. When a room's power comes back, its lights flicker on.
- Spatial audio: alarms, reactor hum, generator, ventilation, doors, beeps and buzzes.

## Scripts added

- `RoomPower`: dims a room's lights and panels; `PowerUp()` flickers them back on.
- `TelemetryText`: live-looking numbers on the console screens.
- `AmbientLoop`: machine hum loops.
- `DoorController`: now supports two sliding panels and status lights, and still works with the old single-cube doors.
- `MissionManager`: new optional fields for room power, the escape-pod door and the pod teleport floor.
- `Editor/StationTextures`, `Editor/StationBuilder*.cs`: the generator.

## If something looks wrong

- **No movement / no hands:** the `XR Origin (XR Rig)` prefab could not be found. The Console says so. Drag it into the scene at (0, 0, -2.6).
- **Can't grab or snap items:** check the Interaction Layers in Project Settings → XR Interaction Toolkit. You need PowerCell, CoolantRed, CoolantBlue, CoolantGreen, Keycard and Teleport.
- **Too dark or too bright:** select `PostProcessing` and change *Color Adjustments → Post Exposure*, or raise the intensity of the `CeilingLight` objects.
