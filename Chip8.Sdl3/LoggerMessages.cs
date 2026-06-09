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

using Microsoft.Extensions.Logging;
using static Chip8.Sdl3.NativeImports.Ffi;

namespace Chip8.Sdl3;

internal static partial class LoggerMessages
{
    [LoggerMessage(EventId = 9000, Level = LogLevel.Trace, Message = "[Sdl.{Category}] {Message}")]
    public static partial void SdlTrace(ILogger logger, SDL_LogCategory category, string message);

    [LoggerMessage(EventId = 9001, Level = LogLevel.Debug, Message = "[Sdl.{Category}] {Message}")]
    public static partial void SdlDebug(ILogger logger, SDL_LogCategory category, string message);

    [LoggerMessage(
        EventId = 9002,
        Level = LogLevel.Information,
        Message = "[Sdl.{Category}] {Message}"
    )]
    public static partial void SdlInformation(
        ILogger logger,
        SDL_LogCategory category,
        string message
    );

    [LoggerMessage(
        EventId = 9003,
        Level = LogLevel.Warning,
        Message = "[Sdl.{Category}] {Message}"
    )]
    public static partial void SdlWarning(ILogger logger, SDL_LogCategory category, string message);

    [LoggerMessage(EventId = 9004, Level = LogLevel.Error, Message = "[Sdl.{Category}] {Message}")]
    public static partial void SdlError(ILogger logger, SDL_LogCategory category, string message);

    [LoggerMessage(
        EventId = 9005,
        Level = LogLevel.Critical,
        Message = "[Sdl.{Category}] {Message}"
    )]
    public static partial void SdlCritical(
        ILogger logger,
        SDL_LogCategory category,
        string message
    );
}
