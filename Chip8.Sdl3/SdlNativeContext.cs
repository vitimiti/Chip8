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
using Chip8.Common.Events;
using Microsoft.Extensions.Logging;
using static Chip8.Sdl3.NativeImports.Ffi;

namespace Chip8.Sdl3;

public class SdlNativeContext : INativeContext
{
    public event EventHandler<QuitEventArgs>? QuitRequested;

    private readonly ILogger<SdlNativeContext> _logger;

    private SafeLogObject? _logObject;
    private SdlNativeDisplay? _display;
    private bool _disposedValue;

    public SdlNativeContext(ILogger<SdlNativeContext> logger, SdlNativeDisplay display)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(display);

        _logger = logger;
        _display = display;
    }

    [MemberNotNull(nameof(_logObject), nameof(_display))]
    public void Initialize()
    {
        if (_display is null)
        {
            throw new InvalidOperationException("Display is not initialized.");
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

        _display.Initialize();
    }

    public void Update(GameTime gameTime)
    {
        if (_display is null)
        {
            throw new InvalidOperationException("Display is not initialized.");
        }

        while (SDL_PollEvent(out var e))
        {
            if (e.Type == SDL_EVENT_QUIT)
            {
                QuitRequested?.Invoke(this, new QuitEventArgs(gameTime.TotalTime));
            }
        }

        _display.Update(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        if (_display is null)
        {
            throw new InvalidOperationException("Display is not initialized.");
        }

        _display.Draw(gameTime);
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
            _logObject?.Dispose();
        }

        _display = null;

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
