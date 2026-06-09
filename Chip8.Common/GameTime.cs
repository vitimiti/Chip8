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

using System.Diagnostics;

namespace Chip8.Common;

public sealed class GameTime : IDisposable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private TimeSpan _lastElapsed = TimeSpan.Zero;
    private int _frameCount;
    private bool _disposedValue;

    public TimeSpan DeltaTime { get; private set; } = TimeSpan.Zero;

    public TimeSpan TotalTime { get; private set; } = TimeSpan.Zero;

    public TimeSpan ElapsedTime { get; private set; } = TimeSpan.Zero;

    public TimeSpan FrameTime { get; private set; } = TimeSpan.Zero;

    public int FramesPerSecond { get; private set; }

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            _stopwatch.Stop();
        }

        _disposedValue = true;
    }

    ~GameTime()
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

    public void Update()
    {
        ElapsedTime = _stopwatch.Elapsed;
        DeltaTime = ElapsedTime - _lastElapsed;
        _lastElapsed = ElapsedTime;
        TotalTime = ElapsedTime;
        FrameTime += DeltaTime;
        _frameCount++;

        if (FrameTime >= TimeSpan.FromSeconds(1))
        {
            // Publish measured FPS for the elapsed second and keep remainder to avoid drift.
            FramesPerSecond = _frameCount;
            _frameCount = 0;
            FrameTime -= TimeSpan.FromSeconds(1);
        }
    }
}
