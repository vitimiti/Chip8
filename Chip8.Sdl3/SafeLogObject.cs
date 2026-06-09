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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Extensions.Logging;
using static Chip8.Sdl3.NativeImports.Ffi;

namespace Chip8.Sdl3;

internal sealed class SafeLogObject : IDisposable
{
    private delegate void LogOutputFunction(
        SDL_LogCategory category,
        SDL_LogPriority priority,
        string message
    );

    private readonly ILogger _logger;

    private bool _disposedValue;

    public SafeLogObject(ILogger logger)
    {
        _logger = logger;
        SDL_SetLogPriorities(SDL_LogPriority.FromLogger(_logger));
        SDL_LogOutputFunction = GCHandle.Alloc(
            (Action<SDL_LogCategory, SDL_LogPriority, string>)LogOutput
        );

        unsafe
        {
            SDL_SetLogOutputFunction(
                &UnmanagedLogOutput,
                (void*)GCHandle.ToIntPtr(SDL_LogOutputFunction)
            );
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            // No managed resources to dispose.
        }

        if (SDL_LogOutputFunction.IsAllocated)
        {
            SDL_LogOutputFunction.Free();
        }

        _disposedValue = true;
    }

    ~SafeLogObject()
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

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void UnmanagedLogOutput(
        void* userData,
        SDL_LogCategory category,
        SDL_LogPriority priority,
        byte* message
    )
    {
        if (userData is null)
        {
            return;
        }

        var handle = GCHandle.FromIntPtr((nint)userData);
        if (!handle.IsAllocated || handle.Target is not LogOutputFunction callback)
        {
            return;
        }

        var messageString = Utf8StringMarshaller.ConvertToManaged(message) ?? string.Empty;
        callback(category, priority, messageString);
    }

    private void LogOutput(SDL_LogCategory category, SDL_LogPriority priority, string message)
    {
        if (priority == SDL_LOG_PRIORITY_TRACE)
        {
            LoggerMessages.SdlTrace(_logger, category, message);
        }
        else if (priority == SDL_LOG_PRIORITY_DEBUG)
        {
            LoggerMessages.SdlDebug(_logger, category, message);
        }
        else if (priority == SDL_LOG_PRIORITY_INFO)
        {
            LoggerMessages.SdlInformation(_logger, category, message);
        }
        else if (priority == SDL_LOG_PRIORITY_WARN)
        {
            LoggerMessages.SdlWarning(_logger, category, message);
        }
        else if (priority == SDL_LOG_PRIORITY_ERROR)
        {
            LoggerMessages.SdlError(_logger, category, message);
        }
        else if (priority == SDL_LOG_PRIORITY_CRITICAL)
        {
            LoggerMessages.SdlCritical(_logger, category, message);
        }
    }
}
