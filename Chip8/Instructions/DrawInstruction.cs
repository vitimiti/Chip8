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
    private readonly record struct DrawContext(
        Size LogicalDisplaySize,
        Size PhysicalDisplaySize,
        bool ClipAtEdges,
        int PixelScale
    );

    public override void Execute()
    {
        if (!Interpreter.TryBeginDraw())
        {
            Interpreter.ProgramCounter -= 2;
            return;
        }

        var logicalDisplaySize = new Size(64, 32);
        if (Interpreter.Options.Type is not InterpreterType.Classic && Interpreter.IsHighResolution)
        {
            logicalDisplaySize = new Size(128, 64);
        }

        var physicalDisplaySize =
            Interpreter.Options.Type is InterpreterType.Classic
                ? new Size(64, 32)
                : new Size(128, 64);

        var isSuperChip =
            Interpreter.Options.Type
            is InterpreterType.SuperChipLegacy
                or InterpreterType.SuperChipModern;
        var drawSchip16x16 = isSuperChip && N == 0;

        var pixelScale =
            Interpreter.Options.Type is not InterpreterType.Classic && !Interpreter.IsHighResolution
                ? 2
                : 1;

        var clipAtEdges = Interpreter.Options.Type is not InterpreterType.XoChip;
        var spriteWidth = drawSchip16x16 ? 16 : 8;
        var spriteHeight = drawSchip16x16 ? 16 : N;

        var drawContext = new DrawContext(
            logicalDisplaySize,
            physicalDisplaySize,
            clipAtEdges,
            pixelScale
        );

        Interpreter.V[0xF] = 0;

        // Start coordinates wrap to the display size.
        var (x, y) = CalculateCoordinates(logicalDisplaySize);
        RowLoop(drawContext, x, y, spriteWidth, spriteHeight, drawSchip16x16);
    }

    public override string ToString() => $"(0x{OpCode:X4})\tDRW V{X:X}, V{Y:X}, 0x{N:X}";

    private (int X, int Y) CalculateCoordinates(Size displaySize)
    {
        var x = Interpreter.V[X] & (displaySize.Width - 1);
        var y = Interpreter.V[Y] & (displaySize.Height - 1);
        return (x, y);
    }

    private void ColumnLoop(
        DrawContext drawContext,
        int x,
        int spriteWidth,
        int spriteRow,
        int screenY
    )
    {
        for (var col = 0; col < spriteWidth; col++)
        {
            var rawX = x + col;
            if (drawContext.ClipAtEdges && rawX >= drawContext.LogicalDisplaySize.Width)
            {
                break;
            }

            var screenX = rawX & (drawContext.LogicalDisplaySize.Width - 1);

            var spriteMask = 1 << (spriteWidth - 1 - col);
            if ((spriteRow & spriteMask) != 0)
            {
                DrawScaledPixel(drawContext, screenX, screenY);
            }
        }
    }

    private void DrawScaledPixel(DrawContext drawContext, int logicalX, int logicalY)
    {
        var pixelScale = drawContext.PixelScale;
        var baseX = logicalX * pixelScale;
        var baseY = logicalY * pixelScale;

        for (var dy = 0; dy < pixelScale; dy++)
        {
            for (var dx = 0; dx < pixelScale; dx++)
            {
                var physicalX = baseX + dx;
                var physicalY = baseY + dy;
                var displayIndex = (physicalY * drawContext.PhysicalDisplaySize.Width) + physicalX;

                if (Interpreter.DisplayBuffer[displayIndex] == 1)
                {
                    Interpreter.V[0xF] = 1;
                }

                Interpreter.DisplayBuffer[displayIndex] ^= 1;
            }
        }
    }

    private void RowLoop(
        DrawContext drawContext,
        int x,
        int y,
        int spriteWidth,
        int spriteHeight,
        bool drawSchip16x16
    )
    {
        for (var row = 0; row < spriteHeight; row++)
        {
            var rawY = y + row;
            if (drawContext.ClipAtEdges && rawY >= drawContext.LogicalDisplaySize.Height)
            {
                break;
            }

            var screenY = rawY & (drawContext.LogicalDisplaySize.Height - 1);

            var spriteRow = drawSchip16x16
                ? (Interpreter.Memory.Span[(ushort)(Interpreter.I + (row * 2))] << 8)
                    | Interpreter.Memory.Span[(ushort)(Interpreter.I + (row * 2) + 1)]
                : Interpreter.Memory.Span[(ushort)(Interpreter.I + row)];

            ColumnLoop(drawContext, x, spriteWidth, spriteRow, screenY);
        }
    }
}
