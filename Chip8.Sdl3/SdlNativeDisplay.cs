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
using static Chip8.Sdl3.NativeImports.Ffi;

namespace Chip8.Sdl3;

public class SdlNativeDisplay : INativeDisplay
{
    private SDL_Window? _window;
    private SDL_Renderer? _renderer;
    private bool _disposedValue;

    [MemberNotNull(nameof(_window), nameof(_renderer))]
    public void Initialize()
    {
        _window = SDL_CreateWindow("CHIP-8 Interpreter", 64 * 10, 32 * 10, SDL_WINDOW_RESIZABLE);
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
            !SDL_SetRenderLogicalPresentation(_renderer, 64, 32, SDL_LOGICAL_PRESENTATION_LETTERBOX)
        )
        {
            throw new InvalidOperationException(
                $"Failed to set SDL renderer logical presentation: {SDL_GetError()}."
            );
        }
    }

    public void Update(GameTime gameTime)
    {
        if (_renderer is null)
        {
            throw new InvalidOperationException("SDL renderer is not initialized.");
        }

        if (!SDL_RenderClear(_renderer))
        {
            throw new InvalidOperationException($"Failed to clear SDL renderer: {SDL_GetError()}.");
        }
    }

    public void Draw(GameTime gameTime)
    {
        if (_renderer is null)
        {
            throw new InvalidOperationException("SDL renderer is not initialized.");
        }

        if (!SDL_RenderPresent(_renderer))
        {
            throw new InvalidOperationException(
                $"Failed to present SDL renderer: {SDL_GetError()}."
            );
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
