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

namespace Chip8.Instructions;

internal record DrawInstruction(Interpreter Interpreter, ushort OpCode)
    : BaseInstruction(Interpreter, OpCode)
{
    public override void Execute()
    {
        var x = Interpreter.V[X] % 64;
        var y = Interpreter.V[Y] % 32;
        var height = N;

        Interpreter.V[0xF] = 0;

        for (var row = 0; row < height; row++)
        {
            var spriteRow = Interpreter.Memory.Span[(ushort)(Interpreter.I + row)];
            for (var col = 0; col < 8; col++)
            {
                if ((spriteRow & (0x80 >> col)) != 0)
                {
                    var displayIndex = ((y + row) % 32 * 64) + ((x + col) % 64);
                    if (Interpreter.DisplayBuffer[displayIndex] == 1)
                    {
                        Interpreter.V[0xF] = 1;
                    }

                    Interpreter.DisplayBuffer[displayIndex] ^= 1;
                }
            }
        }
    }

    public override string ToString() => $"(0x{OpCode:X4})\tDRW V{X:X}, V{Y:X}, 0x{N:X}";
}
