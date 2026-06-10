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

using Chip8.Common.Configurations;

namespace Chip8.Instructions;

internal record StoreInstruction(Interpreter Interpreter, ushort OpCode)
    : BaseInstruction(Interpreter, OpCode)
{
    public override void Execute()
    {
        byte[] bytesToStore = new byte[X + 1];
        for (byte i = 0; i <= X; i++)
        {
            bytesToStore[i] = Interpreter.V[i];
        }

        for (byte i = 0; i <= X; i++)
        {
            Interpreter.Memory.Span[Interpreter.I + i] = bytesToStore[i];
        }

        if (Interpreter.Options.Type is InterpreterType.Classic)
        {
            Interpreter.I += (ushort)(X + 1);
        }
    }

    public override string ToString() => $"(0x{OpCode:X4})\tLD [I], V0-V{X:X}";
}
