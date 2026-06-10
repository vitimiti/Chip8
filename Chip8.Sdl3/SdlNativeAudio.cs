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
using Chip8.Common.Configurations;
using static Chip8.Sdl3.NativeImports.Ffi;

namespace Chip8.Sdl3;

public class SdlNativeAudio : INativeAudio
{
    private const int TargetQueuedBytes = 2048;
    private const string AudioNotInitializedError = "Audio stream is not initialized.";

    private readonly InterpreterOptions _options;

    private SDL_AudioStream? _audioStream;
    private readonly short[] _toneBuffer;
    private readonly int _toneBufferBytes;
    private bool _disposedValue;

    public SdlNativeAudio(InterpreterOptions options)
    {
        _options = options;
        _toneBuffer = CreateToneBuffer(_options.AudioSampleRate, _options.AudioFrequency);
        _toneBufferBytes = _toneBuffer.Length * sizeof(short);
    }

    [MemberNotNull(nameof(_audioStream))]
    public void Initialize()
    {
        SDL_AudioSpec spec = new()
        {
            Format = SDL_AUDIO_FORMAT_S16,
            Channels = 1,
            Frequency = 44100,
        };

        unsafe
        {
            _audioStream = SDL_OpenAudioDeviceStream(
                SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK,
                spec,
                null,
                null
            );
        }

        if (_audioStream.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to open audio stream: {SDL_GetError()}");
        }

        var streamDeviceId = SDL_GetAudioStreamDevice(_audioStream);
        if (streamDeviceId.IsInvalid)
        {
            throw new InvalidOperationException(
                $"Failed to get audio stream device ID: {SDL_GetError()}"
            );
        }

        if (!SDL_SetAudioDeviceGain(streamDeviceId, _options.AudioVolume))
        {
            throw new InvalidOperationException(
                $"Failed to set audio device gain: {SDL_GetError()}"
            );
        }
    }

    public bool IsPaused()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        return _audioStream is null
            ? throw new InvalidOperationException(AudioNotInitializedError)
            : SDL_AudioStreamDevicePaused(_audioStream!);
    }

    public void Resume()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        if (_audioStream is null)
        {
            throw new InvalidOperationException(AudioNotInitializedError);
        }

        if (!SDL_ResumeAudioStreamDevice(_audioStream))
        {
            throw new InvalidOperationException(
                $"Failed to resume audio stream device: {SDL_GetError()}"
            );
        }
    }

    public void Pause()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        if (_audioStream is null)
        {
            throw new InvalidOperationException(AudioNotInitializedError);
        }

        if (!SDL_PauseAudioStreamDevice(_audioStream))
        {
            throw new InvalidOperationException(
                $"Failed to pause audio stream device: {SDL_GetError()}"
            );
        }
    }

    public void RefillQueue()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        if (_audioStream is null)
        {
            throw new InvalidOperationException(AudioNotInitializedError);
        }

        var queuedBytes = SDL_GetAudioStreamQueued(_audioStream);
        if (queuedBytes < 0)
        {
            throw new InvalidOperationException($"Failed to query queued audio: {SDL_GetError()}");
        }

        unsafe
        {
            fixed (short* bufferPtr = _toneBuffer)
            {
                while (queuedBytes < TargetQueuedBytes)
                {
                    if (!SDL_PutAudioStreamData(_audioStream, bufferPtr, _toneBufferBytes))
                    {
                        throw new InvalidOperationException(
                            $"Failed to queue audio stream data: {SDL_GetError()}"
                        );
                    }

                    queuedBytes += _toneBufferBytes;
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
            _audioStream?.Dispose();
        }

        _audioStream = null;

        _disposedValue = true;
    }

    ~SdlNativeAudio()
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

    private static short[] CreateToneBuffer(int sampleRate, int frequency)
    {
        var sampleCount = int.Max(32, sampleRate / 120);
        var tone = new short[sampleCount];

        var samplesPerCycle = int.Max(1, sampleRate / int.Max(1, frequency));
        var halfCycle = int.Max(1, samplesPerCycle / 2);

        for (var i = 0; i < sampleCount; i++)
        {
            tone[i] = i % samplesPerCycle < halfCycle ? short.MaxValue : short.MinValue;
        }

        return tone;
    }
}
