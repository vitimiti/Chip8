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

using System.Drawing;
using Chip8.Common.Configurations;

namespace Chip8.Instructions;

internal record DrawInstruction(Interpreter Interpreter, ushort OpCode)
    : BaseInstruction(Interpreter, OpCode)
{
    public override void Execute()
    {
        var displaySize =
            Interpreter.Options.Type is InterpreterType.Legacy
                ? new Size(64, 32)
                : new Size(128, 64);

        // Start coordinates wrap to the display size.
        var (x, y, height) = CalculateCoordinates(displaySize);
        RowLoop(displaySize, x, y, height);

        Interpreter.V[0xF] = 0;
    }

    public override string ToString() => $"(0x{OpCode:X4})\tDRW V{X:X}, V{Y:X}, 0x{N:X}";

    private (int X, int Y, int Height) CalculateCoordinates(Size displaySize)
    {
        var x = Interpreter.V[X] & (displaySize.Width - 1);
        var y = Interpreter.V[Y] & (displaySize.Height - 1);
        var height = N;
        return (x, y, height);
    }

    private void ColumnLoop(Size displaySize, int x, int spriteRow, int screenY)
    {
        for (var col = 0; col < 8; col++)
        {
            var screenX = x + col;
            if (screenX >= displaySize.Width)
            {
                break;
            }

            if ((spriteRow & (0x80 >> col)) != 0)
            {
                var displayIndex = (screenY * displaySize.Width) + screenX;
                if (Interpreter.DisplayBuffer[displayIndex] == 1)
                {
                    Interpreter.V[0xF] = 1;
                }

                Interpreter.DisplayBuffer[displayIndex] ^= 1;
            }
        }
    }

    private void RowLoop(Size displaySize, int x, int y, int height)
    {
        for (var row = 0; row < height; row++)
        {
            var screenY = y + row;
            if (screenY >= displaySize.Height)
            {
                break;
            }

            var spriteRow = Interpreter.Memory.Span[(ushort)(Interpreter.I + row)];
            ColumnLoop(displaySize, x, spriteRow, screenY);
        }
    }
}
