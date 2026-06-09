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

namespace Chip8.Sdl3.Logging;

internal static partial class GeneralLog
{
    [LoggerMessage(
        EventId = 8000,
        Level = LogLevel.Information,
        Message = "Selected ROM: {RomPath}"
    )]
    public static partial void SelectedRom(ILogger logger, string romPath);

    [LoggerMessage(EventId = 8001, Level = LogLevel.Information, Message = "Quit requested.")]
    public static partial void QuitRequested(ILogger logger);

    [LoggerMessage(
        EventId = 8002,
        Level = LogLevel.Critical,
        Message = "Unhandled exception detected."
    )]
    public static partial void UnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 8003,
        Level = LogLevel.Critical,
        Message = "Unhandled non-exception object detected: {Obj}"
    )]
    public static partial void UnhandledNonException(ILogger logger, object obj);
}
