# Vitimiti's CHIP-8 Interpreter

A CHIP-8 interpreter written in modern C#.

## Automation

This repository includes GitHub Actions automation for continuous integration and release packaging.

- Pushes and pull requests run build validation for Debug, Internal, and Release configurations.
- Tags that start with `v` (for example `v1.0.0`) trigger release artifact builds.

### Release artifacts on `v*` tags

The release workflow publishes the app with runtime-specific output for:

- `win-x86`
- `win-x64`
- `win-arm64`
- `linux-x64`
- `linux-arm64`
- `osx-arm64` (also used as the optional macOS DMG input)

Additional packaging artifacts:

- Windows installers (`.exe`) are generated with Inno Setup and include `SDL3.dll` v3.4.10.
- Linux Flatpak bundle (`.flatpak`) is generated for `linux-x64`, includes access to `~/Games`, and builds against SDL3 v3.4.10.
- macOS DMG (`.dmg`) is built as an optional best-effort artifact.
