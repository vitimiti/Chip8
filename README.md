# Vitimiti's CHIP-8 Interpreter

A CHIP-8 interpreter written in modern C# and SDL3.

## Table of Contents

- [Vitimiti's CHIP-8 Interpreter](#vitimitis-chip-8-interpreter)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Before You Run](#before-you-run)
  - [Running the Emulator](#running-the-emulator)
  - [Keybinds](#keybinds)
  - [CHIP-8 Keypad Mapping](#chip-8-keypad-mapping)
  - [Interpreter Modes](#interpreter-modes)
  - [Runtime Quirk Toggles](#runtime-quirk-toggles)
  - [Configuration (appsettings)](#configuration-appsettings)
  - [Automation and Releases](#automation-and-releases)
    - [Tagged release outputs](#tagged-release-outputs)

## Overview

The emulator supports multiple CHIP-8 family execution modes and runtime quirk toggles so you can switch behavior without restarting.

It also includes:

- Optional debug overlay (HUD)
- 2-second status toasts for key actions
- ROM picker integration
- Automated CI and tagged release packaging

## Before You Run

Expected behavior while running:

- Different ROM test suites can require different quirks.
- You can switch mode and quirks at runtime with function keys.
- Status toasts appear briefly to confirm changes.
- Overlay panels are optional and disabled by default.

## Running the Emulator

From repository root:

```bash
dotnet run --project Chip8/Chip8.csproj -c Debug
```

Other configurations:

```bash
dotnet run --project Chip8/Chip8.csproj -c Internal
dotnet run --project Chip8/Chip8.csproj -c Release
```

## Keybinds

Emulator controls:

- `Esc`: Quit
- `Space`: Pause/Resume
- `Ctrl+O`: Open ROM
- `Ctrl+R`: Reset current ROM

Mode and options:

- `F1`: Classic
- `F2`: SuperChip Legacy
- `F3`: SuperChip Modern
- `F4`: XO-CHIP
- `F5`: Toggle `SetVfOnFx1EOverflow`
- `F6`: Toggle `IncrementIOnFx55Fx65`
- `F7`: Toggle `UseLegacyShiftSourceQuirk`
- `F8`: Toggle debug overlay

## CHIP-8 Keypad Mapping

Keyboard to CHIP-8 keypad mapping:

| Keyboard | CHIP-8 |
| --- | --- |
| `1 2 3 4` | `1 2 3 C` |
| `Q W E R` | `4 5 6 D` |
| `A S D F` | `7 8 9 E` |
| `Z X C V` | `A 0 B F` |

## Interpreter Modes

- **Classic**: Original 64x32 behavior.
- **SuperChip Legacy**: SCHIP legacy semantics.
- **SuperChip Modern**: SCHIP modern semantics.
- **XO-CHIP**: XO-CHIP mode behavior.

Modes can be switched while running; the emulator updates display behavior and timing accordingly.

## Runtime Quirk Toggles

- `SetVfOnFx1EOverflow`
  - `false`: VF unchanged on `FX1E`
  - `true`: VF set on index overflow behavior
- `IncrementIOnFx55Fx65`
  - `false`: `I` unchanged after `FX55`/`FX65`
  - `true`: `I` increments after `FX55`/`FX65`
- `UseLegacyShiftSourceQuirk`
  - `false`: shifts use Vx source semantics
  - `true`: shifts use Vy source semantics

These are runtime toggles and also configurable in `appsettings.json`.

## Configuration (appsettings)

Default options are defined in `Chip8/appsettings.json` under `InterpreterOptions`:

- `Type`
- `DisplaySizeMultiplier`
- `AudioVolume`
- `AudioSampleRate`
- `AudioFrequency`
- `SetVfOnFx1EOverflow`
- `IncrementIOnFx55Fx65`
- `UseLegacyShiftSourceQuirk`

Logging levels can be overridden by build configuration via:

- `Chip8/appsettings.Debug.json`
- `Chip8/appsettings.Internal.json`

## Automation and Releases

The repository includes GitHub Actions for CI and release packaging.

- Pushes and pull requests run build validation (`Debug`, `Internal`, `Release`).
- Tags matching `v*` (example: `v1.0.0`) trigger release packaging and GitHub Release publishing.
- The GitHub Release is created automatically with generated release notes/changelog.

### Tagged release outputs

Runtime publish outputs are generated for:

- `win-x86`
- `win-x64`
- `win-arm64`
- `linux-x64`
- `linux-arm64`
- `osx-arm64`

Runtime packages attached to the GitHub Release:

- Windows runtime packages: `.zip`
- Linux and macOS runtime packages: `.tar.gz`

Platform-specific release artifacts:

- Windows installers (`.exe`) including `SDL3.dll` v3.4.10
- Linux Flatpak (`.flatpak`) for `linux-x64` with `~/Games` filesystem access
- macOS DMG (`.dmg`) (required for tagged releases)

For macOS (`osx-arm64`), release artifacts also bundle SDL3 v3.4.10 as `libSDL3.dylib`.

SDL3 availability policy for release files:

- Windows release artifacts bundle `SDL3.dll` v3.4.10.
- macOS release artifacts bundle `libSDL3.dylib` v3.4.10.
- Linux Flatpak installs SDL3 v3.4.10 and includes `libSDL3.so` in the package.

All root `*.md` files are included in published output, so users can read documentation from release assets without cloning the repository.
