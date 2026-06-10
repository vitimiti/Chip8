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
        if (!Interpreter.TryBeginDraw())
        {
            Interpreter.ProgramCounter -= 2;
            return;
        }

        var displaySize =
            Interpreter.Options.Type is InterpreterType.Classic
                ? new Size(64, 32)
                : new Size(128, 64);

        var drawSchip16x16 = Interpreter.Options.Type is InterpreterType.SuperChipModern && N == 0;
        var clipAtEdges = Interpreter.Options.Type is InterpreterType.Classic;
        var spriteWidth = drawSchip16x16 ? 16 : 8;
        var spriteHeight = drawSchip16x16 ? 16 : N;

        Interpreter.V[0xF] = 0;

        // Start coordinates wrap to the display size.
        var (x, y) = CalculateCoordinates(displaySize);
        RowLoop(displaySize, x, y, spriteWidth, spriteHeight, drawSchip16x16, clipAtEdges);
    }

    public override string ToString() => $"(0x{OpCode:X4})\tDRW V{X:X}, V{Y:X}, 0x{N:X}";

    private (int X, int Y) CalculateCoordinates(Size displaySize)
    {
        var x = Interpreter.V[X] & (displaySize.Width - 1);
        var y = Interpreter.V[Y] & (displaySize.Height - 1);
        return (x, y);
    }

    private void ColumnLoop(
        Size displaySize,
        int x,
        int spriteWidth,
        int spriteRow,
        int screenY,
        bool clipAtEdges
    )
    {
        for (var col = 0; col < spriteWidth; col++)
        {
            var rawX = x + col;
            if (clipAtEdges && rawX >= displaySize.Width)
            {
                break;
            }

            var screenX = rawX & (displaySize.Width - 1);

            var spriteMask = 1 << (spriteWidth - 1 - col);
            if ((spriteRow & spriteMask) != 0)
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

    private void RowLoop(
        Size displaySize,
        int x,
        int y,
        int spriteWidth,
        int spriteHeight,
        bool drawSchip16x16,
        bool clipAtEdges
    )
    {
        for (var row = 0; row < spriteHeight; row++)
        {
            var rawY = y + row;
            if (clipAtEdges && rawY >= displaySize.Height)
            {
                break;
            }

            var screenY = rawY & (displaySize.Height - 1);

            var spriteRow = drawSchip16x16
                ? (Interpreter.Memory.Span[(ushort)(Interpreter.I + (row * 2))] << 8)
                    | Interpreter.Memory.Span[(ushort)(Interpreter.I + (row * 2) + 1)]
                : Interpreter.Memory.Span[(ushort)(Interpreter.I + row)];

            ColumnLoop(displaySize, x, spriteWidth, spriteRow, screenY, clipAtEdges);
        }
    }
}
