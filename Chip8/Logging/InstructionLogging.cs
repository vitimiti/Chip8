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

using Chip8.Instructions;
using Microsoft.Extensions.Logging;

namespace Chip8.Logging;

internal static partial class InstructionLogging
{
    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Debug,
        Message = "Executing instruction: ${ProgramCounter:X4}:{Instruction}"
    )]
    public static partial void ExecutingInstruction(
        ILogger logger,
        ushort programCounter,
        BaseInstruction instruction
    );

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "Unknown instruction: ${ProgramCounter:X4}:{Instruction}"
    )]
    public static partial void UnknownInstruction(
        ILogger logger,
        ushort programCounter,
        BaseInstruction instruction
    );
}
