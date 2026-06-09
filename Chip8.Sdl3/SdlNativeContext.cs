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

using Chip8.Abstractions;
using Chip8.Common;
using Microsoft.Extensions.Logging;
using static Chip8.Sdl3.NativeImports.Ffi;

namespace Chip8.Sdl3;

public class SdlNativeContext : INativeContext
{
    private readonly ILogger<SdlNativeContext> _logger;

    private SafeLogObject? _logObject;
    private bool _disposedValue;

    public SdlNativeContext(ILogger<SdlNativeContext> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public void Initialize()
    {
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
    }

    public void Draw(GameTime gameTime)
    {
        throw new NotImplementedException();
    }

    public void Update(GameTime gameTime)
    {
        throw new NotImplementedException();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            _logObject?.Dispose();
        }

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
