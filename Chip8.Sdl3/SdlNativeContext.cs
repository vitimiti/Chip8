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
using Chip8.Abstractions;
using Chip8.Common;
using Chip8.Common.Configurations;
using Chip8.Common.Events;
using Chip8.Sdl3.Logging;
using Microsoft.Extensions.Logging;
using static Chip8.Sdl3.NativeImports.Ffi;

namespace Chip8.Sdl3;

public class SdlNativeContext : INativeContext
{
    public event EventHandler<QuitEventArgs>? QuitRequested;
    public event EventHandler? PauseToggleRequested;
    public event EventHandler? OpenRomRequested;
    public event EventHandler? ResetRomRequested;
    public event EventHandler<InterpreterModeChangedEventArgs>? InterpreterModeChanged;
    public event EventHandler? SetVfOnFx1EOverflowToggleRequested;
    public event EventHandler? IncrementIOnFx55Fx65ToggleRequested;
    public event EventHandler? UseLegacyShiftSourceQuirkToggleRequested;
    public event EventHandler? DebugOverlayToggleRequested;
    public event EventHandler<StatusMessageEventArgs>? StatusMessageRequested;

    private readonly ILogger<SdlNativeContext> _logger;

    private SafeLogObject? _logObject;
    private SdlNativeAudio? _audio;
    private SdlNativeDisplay? _display;
    private bool _disposedValue;

    public SdlNativeContext(
        ILogger<SdlNativeContext> logger,
        SdlNativeAudio audio,
        SdlNativeDisplay display
    )
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(display);

        _logger = logger;
        _audio = audio;
        _display = display;
    }

    public INativeDisplay? Display => _display;

    public INativeAudio? Audio => _audio;

    [MemberNotNull(nameof(_logObject), nameof(_audio), nameof(_display))]
    public void Initialize()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    GeneralLog.UnhandledException(_logger, ex);
                    _ = SDL_ShowSimpleMessageBox(
                        SDL_MESSAGEBOX_ERROR,
                        "Unhandled Exception",
                        $"An unhandled exception occurred: {ex}",
                        0
                    );
                }
                else
                {
                    GeneralLog.UnhandledNonException(_logger, e.ExceptionObject);
                    _ = SDL_ShowSimpleMessageBox(
                        SDL_MESSAGEBOX_ERROR,
                        "Unhandled Exception",
                        "An unhandled non-exception object was thrown.",
                        0
                    );
                }
            }
            catch
            {
                // Ignored
            }
        };

        if (_display is null)
        {
            throw new InvalidOperationException("Display is not initialized.");
        }

        if (_audio is null)
        {
            throw new InvalidOperationException("Audio is not initialized.");
        }

        SDL_SetMainReady();
        _logObject = new SafeLogObject(_logger);
        if (!SDL_SetAppMetadata("CHIP-8 Interpreter", "0.1.0", "io.github.vitimiti.chip8"))
        {
            throw new InvalidOperationException(
                $"Failed to set SDL application metadata: {SDL_GetError()}."
            );
        }

        if (!SDL_InitSubSystem(SDL_INIT_VIDEO | SDL_INIT_AUDIO))
        {
            throw new InvalidOperationException($"Failed to initialize SDL: {SDL_GetError()}.");
        }

        _audio.Initialize();
        _display.Initialize();
    }

    public void Update(GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(gameTime);
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        if (_display is null)
        {
            throw new InvalidOperationException("Display is not initialized.");
        }

        while (SDL_PollEvent(out var e))
        {
            HandleEvent(e, gameTime.TotalTime);
        }

        _display.Update(gameTime);
    }

    private void HandleEvent(SDL_Event @event, TimeSpan timestamp)
    {
        if (@event.Type == SDL_EVENT_QUIT)
        {
            QuitRequested?.Invoke(this, new QuitEventArgs(timestamp));
            return;
        }

        if (@event.Type == SDL_EVENT_KEY_DOWN)
        {
            HandleKeyDown(@event.Key.Scancode, timestamp);
        }
    }

    private void HandleKeyDown(SDL_Scancode scancode, TimeSpan timestamp)
    {
        if (HandleInterpreterFunctionKeys(scancode))
        {
            return;
        }

        if (HandleQuirkFunctionKeys(scancode))
        {
            return;
        }

        PublishKeypadStatusMessage(scancode);
        HandleEmulatorControlKeys(scancode, timestamp);
    }

    private bool HandleInterpreterFunctionKeys(SDL_Scancode scancode)
    {
        if (!TryGetInterpreterTypeFromFunctionKey(scancode, out var interpreterType))
        {
            return false;
        }

        InterpreterModeChanged?.Invoke(this, new InterpreterModeChangedEventArgs(interpreterType));
        return true;
    }

    private bool HandleQuirkFunctionKeys(SDL_Scancode scancode)
    {
        if (scancode == SDL_SCANCODE_F5)
        {
            SetVfOnFx1EOverflowToggleRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        if (scancode == SDL_SCANCODE_F6)
        {
            IncrementIOnFx55Fx65ToggleRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        if (scancode == SDL_SCANCODE_F7)
        {
            UseLegacyShiftSourceQuirkToggleRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        if (scancode == SDL_SCANCODE_F8)
        {
            DebugOverlayToggleRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    private void PublishKeypadStatusMessage(SDL_Scancode scancode)
    {
        if (!TryGetKeypadValueFromScancode(scancode, out var keypadValue))
        {
            return;
        }

        StatusMessageRequested?.Invoke(
            this,
            new StatusMessageEventArgs($"PRESSED KEY:{keypadValue:X}")
        );
    }

    private void HandleEmulatorControlKeys(SDL_Scancode scancode, TimeSpan timestamp)
    {
        if (scancode == SDL_SCANCODE_ESCAPE)
        {
            QuitRequested?.Invoke(this, new QuitEventArgs(timestamp));
            return;
        }

        if (scancode == SDL_SCANCODE_SPACE)
        {
            PauseToggleRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (scancode == SDL_SCANCODE_O)
        {
            if (IsCtrlPressed())
            {
                OpenRomRequested?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        if (scancode == SDL_SCANCODE_R && IsCtrlPressed())
        {
            ResetRomRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private static bool IsCtrlPressed()
    {
        var keyboardState = SDL_GetKeyboardState();
        return keyboardState[SDL_SCANCODE_LCTRL] || keyboardState[SDL_SCANCODE_RCTRL];
    }

    private static bool TryGetInterpreterTypeFromFunctionKey(
        SDL_Scancode scancode,
        out InterpreterType interpreterType
    )
    {
        interpreterType = scancode switch
        {
            _ when scancode == SDL_SCANCODE_F1 => InterpreterType.Classic,
            _ when scancode == SDL_SCANCODE_F2 => InterpreterType.SuperChipLegacy,
            _ when scancode == SDL_SCANCODE_F3 => InterpreterType.SuperChipModern,
            _ when scancode == SDL_SCANCODE_F4 => InterpreterType.XoChip,
            _ => default,
        };

        return scancode == SDL_SCANCODE_F1
            || scancode == SDL_SCANCODE_F2
            || scancode == SDL_SCANCODE_F3
            || scancode == SDL_SCANCODE_F4;
    }

    private static bool TryGetKeypadValueFromScancode(SDL_Scancode scancode, out byte keypadValue)
    {
        keypadValue = scancode switch
        {
            _ when scancode == SDL_SCANCODE_X => 0x0,
            _ when scancode == SDL_SCANCODE_1 => 0x1,
            _ when scancode == SDL_SCANCODE_2 => 0x2,
            _ when scancode == SDL_SCANCODE_3 => 0x3,
            _ when scancode == SDL_SCANCODE_Q => 0x4,
            _ when scancode == SDL_SCANCODE_W => 0x5,
            _ when scancode == SDL_SCANCODE_E => 0x6,
            _ when scancode == SDL_SCANCODE_A => 0x7,
            _ when scancode == SDL_SCANCODE_S => 0x8,
            _ when scancode == SDL_SCANCODE_D => 0x9,
            _ when scancode == SDL_SCANCODE_Z => 0xA,
            _ when scancode == SDL_SCANCODE_C => 0xB,
            _ when scancode == SDL_SCANCODE_4 => 0xC,
            _ when scancode == SDL_SCANCODE_R => 0xD,
            _ when scancode == SDL_SCANCODE_F => 0xE,
            _ when scancode == SDL_SCANCODE_V => 0xF,
            _ => 0,
        };

        return scancode == SDL_SCANCODE_X
            || scancode == SDL_SCANCODE_1
            || scancode == SDL_SCANCODE_2
            || scancode == SDL_SCANCODE_3
            || scancode == SDL_SCANCODE_Q
            || scancode == SDL_SCANCODE_W
            || scancode == SDL_SCANCODE_E
            || scancode == SDL_SCANCODE_A
            || scancode == SDL_SCANCODE_S
            || scancode == SDL_SCANCODE_D
            || scancode == SDL_SCANCODE_Z
            || scancode == SDL_SCANCODE_C
            || scancode == SDL_SCANCODE_4
            || scancode == SDL_SCANCODE_R
            || scancode == SDL_SCANCODE_F
            || scancode == SDL_SCANCODE_V;
    }

    public void Draw(GameTime gameTime, byte[] displayBuffer, EmulatorDebugSnapshot debugSnapshot)
    {
        ArgumentNullException.ThrowIfNull(gameTime);
        ArgumentNullException.ThrowIfNull(displayBuffer);
        ArgumentNullException.ThrowIfNull(debugSnapshot);
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        if (_display is null)
        {
            throw new InvalidOperationException("Display is not initialized.");
        }

        _display.Draw(gameTime, displayBuffer, debugSnapshot);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            _display?.Dispose();
            _audio?.Dispose();
            _logObject?.Dispose();
        }

        _display = null;
        _audio = null;

        SDL_Quit();
        _logObject = null;

        _disposedValue = true;
    }

    ~SdlNativeContext()
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
