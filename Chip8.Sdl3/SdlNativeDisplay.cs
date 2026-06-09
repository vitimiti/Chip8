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
using Chip8.Sdl3.Logging;
using Microsoft.Extensions.Logging;
using static Chip8.Sdl3.NativeImports.Ffi;

namespace Chip8.Sdl3;

public class SdlNativeDisplay(ILogger<SdlNativeDisplay> logger) : INativeDisplay
{
    private const int DisplayWidth = 64;
    private const int DisplayHeight = 32;
    private const int PixelSize = 10;

    private readonly ILogger<SdlNativeDisplay> _logger = logger;
    private readonly RomSelector _romSelector = new();
    private readonly float[] _phosphor = new float[DisplayWidth * DisplayHeight];

    private SDL_Window? _window;
    private SDL_Renderer? _renderer;
    private bool _romSelectorShown;
    private bool _disposedValue;

    public bool RomSelected { get; private set; }

    public string? SelectedRomPath { get; private set; }

    [MemberNotNull(nameof(_window), nameof(_renderer))]
    public void Initialize()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        _window = SDL_CreateWindow(
            "CHIP-8 Interpreter",
            DisplayWidth * PixelSize,
            DisplayHeight * PixelSize,
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
                DisplayWidth,
                DisplayHeight,
                SDL_LOGICAL_PRESENTATION_LETTERBOX
            )
        )
        {
            throw new InvalidOperationException(
                $"Failed to set SDL renderer logical presentation: {SDL_GetError()}."
            );
        }
    }

    public void Update(GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(gameTime);
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        if (_renderer is null)
        {
            throw new InvalidOperationException("SDL renderer is not initialized.");
        }

        if (!RomSelected && !_romSelectorShown)
        {
            if (_window is null)
            {
                throw new InvalidOperationException("SDL window is not initialized.");
            }

            _romSelectorShown = true;
            _romSelector.Show(_window);
            _romSelector.RomSelected += (_, args) =>
            {
                RomSelected = true;
                SelectedRomPath = args.RomPath;
                GeneralLog.SelectedRom(_logger, args.RomPath);
            };
        }

        if (!RomSelected)
        {
            return;
        }

        if (!SDL_RenderClear(_renderer))
        {
            throw new InvalidOperationException($"Failed to clear SDL renderer: {SDL_GetError()}.");
        }
    }

    public void Draw(GameTime gameTime, byte[] displayBuffer)
    {
        ArgumentNullException.ThrowIfNull(gameTime);
        ArgumentNullException.ThrowIfNull(displayBuffer);
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        if (_renderer is null)
        {
            throw new InvalidOperationException("SDL renderer is not initialized.");
        }

        if (!RomSelected)
        {
            return;
        }

        UpdateScreen(gameTime, displayBuffer);

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
        var decayBlend = float.Exp(-dt / (float)decayTime.TotalSeconds);

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
        for (var y = 0; y < DisplayHeight; y++)
        {
            for (var x = 0; x < DisplayWidth; x++)
            {
                var index = (y * DisplayWidth) + x;
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
