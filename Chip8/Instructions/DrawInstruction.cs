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

internal record DrawInstruction(ushort OpCode) : BaseInstruction(OpCode)
{
    public override void Execute(Interpreter interpreter)
    {
        var x = interpreter.V[X] % 64;
        var y = interpreter.V[Y] % 32;
        var height = N;

        interpreter.V[0xF] = 0;

        for (var row = 0; row < height; row++)
        {
            var spriteRow = interpreter.Memory.Span[(ushort)(interpreter.I + row)];
            for (var col = 0; col < 8; col++)
            {
                if ((spriteRow & (0x80 >> col)) != 0)
                {
                    var displayIndex = ((y + row) % 32 * 64) + ((x + col) % 64);
                    if (interpreter.DisplayBuffer[displayIndex] == 1)
                    {
                        interpreter.V[0xF] = 1;
                    }

                    interpreter.DisplayBuffer[displayIndex] ^= 1;
                }
            }
        }
    }

    public override string ToString() => $"(0x{OpCode:X4})\tDRW V{X:X}, V{Y:X}, 0x{N:X}";
}
