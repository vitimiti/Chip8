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

using System.ComponentModel.DataAnnotations;

namespace Chip8.Common.Configurations;

public sealed record InterpreterOptions
{
    [Required]
    public InterpreterType Type { get; set; } = InterpreterType.Legacy;

    [Required]
    [Range(1, 50, ErrorMessage = "The display size multiplier must be between 1 and 50.")]
    public int DisplaySizeMultiplier { get; set; } = 10;

    [Required]
    [Range(0F, 1F, ErrorMessage = "The audio volume must be between 0.0 and 1.0.")]
    public float AudioVolume { get; set; } = 0.01F;

    [Required]
    [Range(1, 192000, ErrorMessage = "The audio sample rate must be between 1 and 192000Hz.")]
    public int AudioSampleRate { get; set; } = 44_100;

    [Required]
    [Range(1, 192000, ErrorMessage = "The audio frequency must be between 1 and 192000Hz.")]
    public int AudioFrequency { get; set; } = 440;

    public bool SetVfOnFx1EOverflow { get; set; }

    public override string ToString() =>
        $"Interpreter Type: {Type}, Display Size Multiplier: {DisplaySizeMultiplier}, Audio Volume: {AudioVolume:P4}, Set VF on FX1E overflow: {SetVfOnFx1EOverflow}";
}
