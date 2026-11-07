# Windstille (wind calm)

> **Now part of [Kaan's QoL](https://github.com/Kaanori/spt-kaansqol)** – this module is included there (with your settings migrated automatically). This stand-alone version is no longer updated.

Makes calm wind in EFT actually silent.

## How this mod was made (AI disclosure)

This mod was **programmed by Claude**, an AI assistant made by Anthropic.
**Kaan was the tester**: Kaan has no programming background, played with the mod
on SPT 4.1.6, reported what went wrong, and Claude fixed it. No human programmer
has reviewed the code. It is open source so anyone can check it — if you find a
bug, please open an [issue](https://github.com/Kaanori/spt-windstille/issues).

## Requirements

- SPT 4.1.6 (EFT 0.16.9). BepInEx ships with SPT.

## Installation

1. Download `Windstille-1.0.0.zip` from the [Releases](https://github.com/Kaanori/spt-windstille/releases) page.
2. Extract it **into your SPT game folder** (the folder that contains `EscapeFromTarkov.exe`).
   The ZIP already has the right layout; the mod ends up in `BepInEx/plugins/`.
   On Windows, make sure "Extract All" does not add an extra subfolder.
3. Restart the game.

## Uninstall

Delete the mod's files from `BepInEx/plugins/` (settings: `BepInEx/config/` for client mods) and restart the game.

## Why it is needed

`Audio.AmbientSubsystem.WindAmbientBlender` hard-clamps the target volume
of the wind ambience loop:

```csharp
public float method_9() { return Mathf.Clamp(Vector2_0.magnitude, 0.1f, 1f); }   // lower bound 0.1
public float method_7() { return Mathf.Clamp(method_9(), 0.2f, 1f) * float_5; }  // lower bound 0.2
```

So even at wind strength 0 the loop keeps playing at 20%. This cannot be
turned off via the server's weather configuration — `windSpeed` there is
already at 0 for 90% of the time anyway.

## How it works

Harmony postfix on `WindAmbientBlender.ManualUpdate`. After every tick the
volume of `_windSource` and `_windMixSource` is capped, depending on the
real wind value from `WeatherController.Instance.WeatherCurve.Wind`.

Deliberately **capping instead of multiplying**: a factor would creep
toward zero frame by frame, because the game does not reset the volume
between two crossfades.

Only names that EFT does not obfuscate are addressed
(`ManualUpdate`, `_windSource`, `_windMixSource`, `WeatherController`) —
`method_7` is deliberately *not* patched, its name shifts with every update.

## Settings (F12)

| Setting | Default | Meaning |
|---|---|---|
| **Eingeschaltet** (enabled) | on | |
| **Schwelle fuer Flaute** (calm threshold) | 0.15 | up to which wind strength it counts as calm |
| **Lautstaerke bei Flaute** (volume when calm) | 0 | 0 = completely silent |
| **Lautstaerke bei Wind** (volume when windy) | 1 | Upper limit at real wind, 1 = unchanged |

Between the threshold and full wind strength the volume is faded up gently,
so there is no audible jump.

## Building from source

Needs the .NET 10 SDK and an SPT 4.1.6 installation to build against.

```bash
./build.sh             # Linux: build only
./build.sh --install   # build and copy into the game folder
```
```powershell
.\build.ps1            # Windows
.\build.ps1 -Install
```

The game folder is set in `Directory.Build.props` (Windows default `C:/SPT`)
or via the environment variable `SPT_DIR`.

## Support

Bugs and questions: [GitHub issues](https://github.com/Kaanori/spt-windstille/issues).

## License

MIT — see [LICENSE](LICENSE).
