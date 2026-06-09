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

namespace Chip8;

internal class Interpreter(INativeContext nativeContext) : IDisposable
{
    private GameTime? _gameTime;
    private INativeContext? _nativeContext;

    private bool _running;
    private bool _disposedValue;

    public void Run()
    {
        Initialize();
        while (_running)
        {
            Update(_gameTime);
            Draw(_gameTime);
        }
    }

    [MemberNotNull(nameof(_gameTime), nameof(_nativeContext))]
    private void Initialize()
    {
        _running = true;
        _gameTime = new GameTime();
        _nativeContext = nativeContext;
        _nativeContext.Initialize();
        _nativeContext.QuitRequested += (_, _) => _running = false;
    }

    private void Update(GameTime gameTime) => _nativeContext?.Update(gameTime);

    public void Draw(GameTime gameTime) => _nativeContext?.Draw(gameTime);

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            _nativeContext?.Dispose();
            _gameTime?.Dispose();
        }

        _nativeContext = null;
        _gameTime = null;

        _disposedValue = true;
    }

    ~Interpreter()
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
