// The MIT License (MIT)
//
// Copyright (c) 2026 Victor Matia (vitimiti)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the “Software”), to deal in the Software without
// restriction, including without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;
using Chip8.Abstractions;
using Chip8.Common;
using Chip8.Common.Configurations;
using Chip8.Sdl3.Logging;
using Microsoft.Extensions.Logging;
using static Chip8.Sdl3.NativeImports.Ffi;

namespace Chip8.Sdl3;

public class SdlNativeDisplay : INativeDisplay
{
    private readonly ILogger<SdlNativeDisplay> _logger;
    private readonly InterpreterOptions _options;
    private readonly RomSelector _romSelector = new();
    private float[] _phosphor;
    private Size _displaySize;

    // csharpier-ignore
    private readonly Dictionary<SDL_Scancode, bool> _scanCodes = new()
    {
        { SDL_SCANCODE_1, false }, { SDL_SCANCODE_2, false }, { SDL_SCANCODE_3, false }, { SDL_SCANCODE_4, false }, // 1, 2, 3, C
        { SDL_SCANCODE_Q, false }, { SDL_SCANCODE_W, false }, { SDL_SCANCODE_E, false }, { SDL_SCANCODE_R, false }, // 4, 5, 6, D
        { SDL_SCANCODE_A, false }, { SDL_SCANCODE_S, false }, { SDL_SCANCODE_D, false }, { SDL_SCANCODE_F, false }, // 7, 8, 9, E
        { SDL_SCANCODE_Z, false }, { SDL_SCANCODE_X, false }, { SDL_SCANCODE_C, false }, { SDL_SCANCODE_V, false }, // A, 0, B, F
    };

    private SDL_Window? _window;
    private SDL_Renderer? _renderer;
    private bool _romSelectorShown;
    private bool _romSelectionInProgress;
    private bool _romReloadRequested;
    private bool _disposedValue;

    public SdlNativeDisplay(ILogger<SdlNativeDisplay> logger, InterpreterOptions options)
    {
        _logger = logger;
        _options = options;
        _displaySize = GetDisplaySize(options.Type);

        _phosphor = new float[_displaySize.Width * _displaySize.Height];

        _romSelector.RomSelected += (_, args) =>
        {
            RomSelected = true;
            SelectedRomPath = args.RomPath;
            _romReloadRequested = true;
            GeneralLog.SelectedRom(_logger, args.RomPath);
        };

        _romSelector.SelectionCompleted += (_, _) => _romSelectionInProgress = false;
    }

    public bool RomSelected { get; private set; }

    public string? SelectedRomPath { get; private set; }

    public bool IsRomSelectionInProgress => _romSelectionInProgress;

    public bool RomReloadRequested => _romReloadRequested;

    public void SetInterpreterType(InterpreterType interpreterType)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        _displaySize = GetDisplaySize(interpreterType);
        _phosphor = new float[_displaySize.Width * _displaySize.Height];

        if (
            _renderer is not null
            && !SDL_SetRenderLogicalPresentation(
                _renderer,
                _displaySize.Width,
                _displaySize.Height,
                SDL_LOGICAL_PRESENTATION_LETTERBOX
            )
        )
        {
            throw new InvalidOperationException(
                $"Failed to set SDL renderer logical presentation: {SDL_GetError()}."
            );
        }

        if (
            _window is not null
            && !SDL_SetWindowSize(
                _window,
                _displaySize.Width * _options.DisplaySizeMultiplier,
                _displaySize.Height * _options.DisplaySizeMultiplier
            )
        )
        {
            throw new InvalidOperationException($"Failed to resize SDL window: {SDL_GetError()}.");
        }
    }

    [MemberNotNull(nameof(_window), nameof(_renderer))]
    public void Initialize()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        _window = SDL_CreateWindow(
            "CHIP-8 Interpreter",
            _displaySize.Width * _options.DisplaySizeMultiplier,
            _displaySize.Height * _options.DisplaySizeMultiplier,
            SDL_WINDOW_RESIZABLE
        );
        if (_window.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to create SDL window: {SDL_GetError()}.");
        }

        _renderer = SDL_CreateRenderer(_window, null);
        if (_renderer.IsInvalid)
        {
            throw new InvalidOperationException(
                $"Failed to create SDL renderer: {SDL_GetError()}."
            );
        }

        if (!SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255))
        {
            throw new InvalidOperationException(
                $"Failed to set SDL renderer draw color: {SDL_GetError()}."
            );
        }

        if (
            !SDL_SetRenderLogicalPresentation(
                _renderer,
                _displaySize.Width,
                _displaySize.Height,
                SDL_LOGICAL_PRESENTATION_LETTERBOX
            )
        )
        {
            throw new InvalidOperationException(
                $"Failed to set SDL renderer logical presentation: {SDL_GetError()}."
            );
        }
    }

    public bool[] SyncKeypad()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        var keyboardState = SDL_GetKeyboardState();
        foreach (var scanCode in _scanCodes.Keys)
        {
            _scanCodes[scanCode] = keyboardState[scanCode];
        }

        return
        [
            _scanCodes[SDL_SCANCODE_X], // 0
            _scanCodes[SDL_SCANCODE_1], // 1
            _scanCodes[SDL_SCANCODE_2], // 2
            _scanCodes[SDL_SCANCODE_3], // 3
            _scanCodes[SDL_SCANCODE_Q], // 4
            _scanCodes[SDL_SCANCODE_W], // 5
            _scanCodes[SDL_SCANCODE_E], // 6
            _scanCodes[SDL_SCANCODE_A], // 7
            _scanCodes[SDL_SCANCODE_S], // 8
            _scanCodes[SDL_SCANCODE_D], // 9
            _scanCodes[SDL_SCANCODE_Z], // A
            _scanCodes[SDL_SCANCODE_C], // B
            _scanCodes[SDL_SCANCODE_4], // C
            _scanCodes[SDL_SCANCODE_R], // D
            _scanCodes[SDL_SCANCODE_F], // E
            _scanCodes[SDL_SCANCODE_V], // F
        ];
    }

    [MemberNotNull(nameof(_window))]
    public void BeginRomSelection()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        if (_window is null)
        {
            throw new InvalidOperationException("SDL window is not initialized.");
        }

        if (_romSelectionInProgress)
        {
            return;
        }

        _romSelectorShown = true;
        _romSelectionInProgress = true;
        _romSelector.Show(_window);
    }

    public void ClearRomReloadRequest() => _romReloadRequested = false;

    public void Update(GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(gameTime);
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        if (!RomSelected && !_romSelectorShown)
        {
            BeginRomSelection();
        }
    }

    public void Draw(GameTime gameTime, byte[] displayBuffer, EmulatorDebugSnapshot debugSnapshot)
    {
        ArgumentNullException.ThrowIfNull(gameTime);
        ArgumentNullException.ThrowIfNull(displayBuffer);
        ArgumentNullException.ThrowIfNull(debugSnapshot);
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        if (_renderer is null)
        {
            throw new InvalidOperationException("SDL renderer is not initialized.");
        }

        if (!SDL_RenderClear(_renderer))
        {
            throw new InvalidOperationException($"Failed to clear SDL renderer: {SDL_GetError()}.");
        }

        if (RomSelected)
        {
            UpdateScreen(gameTime, displayBuffer);
        }

        if (debugSnapshot.ShowDebugOverlay)
        {
            RenderDebugPanels(debugSnapshot);
        }

        if (!SDL_RenderPresent(_renderer))
        {
            throw new InvalidOperationException(
                $"Failed to present SDL renderer: {SDL_GetError()}."
            );
        }
    }

    private static (float Rise, float Decay) CalculatePhosphorBlends(GameTime gameTime)
    {
        TimeSpan riseTime = TimeSpan.FromSeconds(.02F);
        TimeSpan decayTime = TimeSpan.FromSeconds(.18F);

        var dt = float.Min((float)gameTime.DeltaTime.TotalSeconds, .1F);
        var riseBlend = 1F - float.Exp(-dt / (float)riseTime.TotalSeconds);
        var decayBlend = 1F - float.Exp(-dt / (float)decayTime.TotalSeconds);

        return (riseBlend, decayBlend);
    }

    private void CalculatePhosphorDecay(float target, (float Rise, float Decay) blends, int index)
    {
        // Take _phosphor[index] as the intensity of the pixel at (x, y)
        var blend = target > _phosphor[index] ? blends.Rise : blends.Decay;
        _phosphor[index] += (target - _phosphor[index]) * blend;
        _phosphor[index] = float.Clamp(_phosphor[index], 0F, 1F);
    }

    private byte CalculateColorIntensity(int index)
    {
        var gammaCorrected = float.Pow(_phosphor[index], .55F);
        return (byte)(float.Clamp(gammaCorrected, 0F, 1F) * 255F);
    }

    private void UpdateScreen(GameTime gameTime, byte[] displayBuffer)
    {
        ArgumentNullException.ThrowIfNull(gameTime);
        ArgumentNullException.ThrowIfNull(displayBuffer);
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        if (_renderer is null)
        {
            throw new InvalidOperationException("SDL renderer is not initialized.");
        }

        var (riseBlend, decayBlend) = CalculatePhosphorBlends(gameTime);
        for (var y = 0; y < _displaySize.Height; y++)
        {
            for (var x = 0; x < _displaySize.Width; x++)
            {
                var index = (y * _displaySize.Width) + x;
                var pixel = displayBuffer[index];
                var target = pixel != 0 ? 1F : 0F;

                CalculatePhosphorDecay(target, (riseBlend, decayBlend), index);
                var color = CalculateColorIntensity(index);
                if (!SDL_SetRenderDrawColor(_renderer, color, color, color, 255))
                {
                    throw new InvalidOperationException(
                        $"Failed to set SDL renderer draw color: {SDL_GetError()}."
                    );
                }

                if (!SDL_RenderPoint(_renderer, x, y))
                {
                    throw new InvalidOperationException(
                        $"Failed to draw point on SDL renderer: {SDL_GetError()}."
                    );
                }
            }
        }
    }

    private static Size GetDisplaySize(InterpreterType interpreterType) =>
        interpreterType is InterpreterType.Classic ? new Size(64, 32) : new Size(128, 64);

    private void RenderDebugPanels(EmulatorDebugSnapshot debugSnapshot)
    {
        if (_renderer is null || _window is null)
        {
            throw new InvalidOperationException("SDL renderer or window is not initialized.");
        }

        if (!SDL_SetRenderLogicalPresentation(_renderer, 0, 0, SDL_LOGICAL_PRESENTATION_DISABLED))
        {
            throw new InvalidOperationException(
                $"Failed to disable SDL logical presentation: {SDL_GetError()}."
            );
        }

        if (!SDL_GetWindowSize(_window, out var windowWidth, out var windowHeight))
        {
            throw new InvalidOperationException(
                $"Failed to get SDL window size: {SDL_GetError()}."
            );
        }

        var margin = 8F;
        var gap = 8F;
        var leftWidth = (windowWidth - (margin * 3)) * 0.45F;
        var topHeight = (windowHeight - (margin * 3)) * 0.45F;
        var bottomHeight = windowHeight - (margin * 3) - topHeight;
        var rightWidth = windowWidth - (margin * 3) - leftWidth;

        var keybindingsBox = new SDL_FRect
        {
            X = margin,
            Y = margin,
            W = leftWidth,
            H = topHeight,
        };

        var optionsBox = new SDL_FRect
        {
            X = margin,
            Y = margin + topHeight + gap,
            W = leftWidth,
            H = bottomHeight,
        };

        var registersBox = new SDL_FRect
        {
            X = margin + leftWidth + gap,
            Y = margin,
            W = rightWidth,
            H = windowHeight - (margin * 2),
        };

        RenderDebugBox(keybindingsBox, "KEYBINDINGS", GetKeybindingLines());
        RenderDebugBox(optionsBox, "ACTIVE OPTIONS", GetOptionLines(debugSnapshot));
        RenderDebugBox(registersBox, "REGISTERS", GetRegisterLines(debugSnapshot));

        if (
            !SDL_SetRenderLogicalPresentation(
                _renderer,
                _displaySize.Width,
                _displaySize.Height,
                SDL_LOGICAL_PRESENTATION_LETTERBOX
            )
        )
        {
            throw new InvalidOperationException(
                $"Failed to restore SDL logical presentation: {SDL_GetError()}."
            );
        }
    }

    private void RenderDebugBox(SDL_FRect box, string title, IReadOnlyList<string> lines)
    {
        if (_renderer is null)
        {
            throw new InvalidOperationException("SDL renderer is not initialized.");
        }

        if (!SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 230))
        {
            throw new InvalidOperationException(
                $"Failed to set SDL renderer draw color: {SDL_GetError()}."
            );
        }

        if (!SDL_RenderFillRect(_renderer, in box))
        {
            throw new InvalidOperationException($"Failed to draw SDL box fill: {SDL_GetError()}.");
        }

        if (!SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255))
        {
            throw new InvalidOperationException(
                $"Failed to set SDL renderer draw color: {SDL_GetError()}."
            );
        }

        if (!SDL_RenderRect(_renderer, in box))
        {
            throw new InvalidOperationException(
                $"Failed to draw SDL box border: {SDL_GetError()}."
            );
        }

        if (!SDL_RenderDebugText(_renderer, box.X + 6, box.Y + 6, title))
        {
            throw new InvalidOperationException(
                $"Failed to draw SDL debug text: {SDL_GetError()}."
            );
        }

        var textY = box.Y + 20;
        foreach (var line in lines)
        {
            if (textY + 8 > box.Y + box.H - 4)
            {
                break;
            }

            if (!SDL_RenderDebugText(_renderer, box.X + 6, textY, line))
            {
                throw new InvalidOperationException(
                    $"Failed to draw SDL debug text: {SDL_GetError()}."
                );
            }

            textY += 10;
        }
    }

    private static IReadOnlyList<string> GetKeybindingLines() =>
        [
            "EMU:",
            " ESC      quit",
            " SPACE    pause",
            " CTRL+O   open rom",
            " CTRL+R   reset rom",
            " F1..F4   mode",
            "QUIRKS:",
            " F5       fx1e vf",
            " F6       fx55/65 I",
            " F7       shift src",
            " F8       debug hud",
        ];

    private static IReadOnlyList<string> GetOptionLines(EmulatorDebugSnapshot debugSnapshot) =>
        [
            $"TYPE: {debugSnapshot.InterpreterType}",
            $"RES: {(debugSnapshot.IsHighResolution ? "HIRES" : "LORES")}",
            $"F5 FX1E VF: {(debugSnapshot.SetVfOnFx1EOverflow ? "ON" : "OFF")}",
            $"F6 FX55/65 I: {(debugSnapshot.IncrementIOnFx55Fx65 ? "ON" : "OFF")}",
            $"F7 SHIFT SRC: {(debugSnapshot.UseLegacyShiftSourceQuirk ? "VY" : "VX")}",
            $"SOUND: {(debugSnapshot.IsSoundOn ? "ON" : "OFF")}",
        ];

    private static IReadOnlyList<string> GetRegisterLines(EmulatorDebugSnapshot debugSnapshot)
    {
        var lines = new List<string>
        {
            $"I  : 0x{debugSnapshot.IRegister:X4}",
            $"SND: {(debugSnapshot.IsSoundOn ? "ON" : "OFF")}",
            string.Empty,
        };

        for (var i = 0; i < 16; i += 4)
        {
            var line = new StringBuilder();
            for (var offset = 0; offset < 4; offset++)
            {
                var registerIndex = i + offset;
                if (offset > 0)
                {
                    line.Append("  ");
                }

                line.Append($"V{registerIndex:X1}:0x{debugSnapshot.VRegisters[registerIndex]:X2}");
            }

            lines.Add(line.ToString());
        }

        return lines;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            _renderer?.Dispose();
            _window?.Dispose();
        }

        _renderer = null;
        _window = null;

        _disposedValue = true;
    }

    ~SdlNativeDisplay()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
