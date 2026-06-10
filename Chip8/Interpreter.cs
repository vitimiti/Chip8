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

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Chip8.Abstractions;
using Chip8.Common;
using Chip8.Common.Configurations;
using Chip8.Instructions;
using Chip8.Logging;
using Microsoft.Extensions.Logging;

namespace Chip8;

internal class Interpreter : IDisposable
{
    private const string NativeContextNotInitializedMessage = "Native context is not initialized.";
    private const string NativeAudioNotInitializedMessage = "Native audio is not initialized.";

    public const ushort GlyphStartAddress = 0x0050;
    public const ushort GlyphHeight = 5;

    private static readonly TimeSpan TimerTick = TimeSpan.FromSeconds(1.0 / 60.0);

    // csharpier-ignore
    private static readonly byte[] Font = [
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    ];

    private readonly ILogger<Interpreter> _logger;
    private readonly INativeContext _nativeContext1;
    private TimeSpan _instructionTick;

    private GameTime? _gameTime;
    private INativeContext? _nativeContext;
    private bool _running;
    private bool _romLoaded;
    private TimeSpan _instructionAccumulator;
    private TimeSpan _timerAccumulator;
    private bool _paused;
    private bool _waitingForRomSelection;
    private bool _pausedBeforeRomSelection;
    private long _nextDrawAllowedTimestamp;
    private bool _disposedValue;

    public Interpreter(
        ILogger<Interpreter> logger,
        INativeContext nativeContext,
        InterpreterOptions options
    )
    {
        _nativeContext1 = nativeContext;
        _logger = logger;
        Options = options;

        _instructionTick = GetInstructionTick(options.Type);

        DisplayBuffer = new byte[128 * 64];
        IsHighResolution = false;
        _nextDrawAllowedTimestamp = 0;
    }

    internal InterpreterOptions Options { get; }

    internal Memory<byte> Memory { get; } = new byte[4096];

    internal ushort ProgramCounter { get; set; } = 0x0200;

    internal byte[] DisplayBuffer { get; }

    internal byte[] V { get; } = new byte[16];

    internal ushort I { get; set; }

    internal Stack<ushort> Stack { get; } = new(16);

    internal byte DelayTimer { get; set; }

    internal byte SoundTimer { get; set; }

    internal bool[] Keypad { get; private set; } = new bool[0x10];

    internal byte? Fx0AKeyCandidate { get; set; }

    internal bool IsHighResolution { get; private set; }

    public void Run()
    {
        Initialize();

        var gameTime =
            _gameTime ?? throw new InvalidOperationException("Game time is not initialized.");
        while (_running)
        {
            gameTime.Update();
            Update(gameTime);
            Draw(gameTime);
        }
    }

    [MemberNotNull(nameof(_gameTime), nameof(_nativeContext))]
    private void Initialize()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        CommonLogging.InterpreterInitialized(_logger, Options);

        _running = true;
        _gameTime = new GameTime();

        _nativeContext = _nativeContext1;
        _nativeContext.Initialize();
        _nativeContext.QuitRequested += (_, _) => _running = false;
        _nativeContext.PauseToggleRequested += (_, _) => _paused = !_paused;
        _nativeContext.OpenRomRequested += (_, _) => BeginRomSelection();
        _nativeContext.ResetRomRequested += (_, _) => ResetCurrentRomExecution();
        _nativeContext.InterpreterModeChanged += (_, args) =>
            ApplyInterpreterType(args.InterpreterType);

        Font.CopyTo(Memory.Span[GlyphStartAddress..]);
    }

    private void BeginRomSelection()
    {
        if (_nativeContext?.Display is null || _waitingForRomSelection)
        {
            return;
        }

        _pausedBeforeRomSelection = _paused;
        _paused = true;
        _waitingForRomSelection = true;

        if (_nativeContext.Audio is not null && !_nativeContext.Audio.IsPaused())
        {
            _nativeContext.Audio.Pause();
        }

        _nativeContext.Display.BeginRomSelection();
    }

    private void ResetStateForNewRom()
    {
        Memory.Span.Clear();
        Font.CopyTo(Memory.Span[GlyphStartAddress..]);

        DisplayBuffer.AsSpan().Clear();
        Array.Clear(V);
        Stack.Clear();

        ProgramCounter = 0x0200;
        I = 0;
        DelayTimer = 0;
        SoundTimer = 0;
        Fx0AKeyCandidate = null;
        Keypad = new bool[0x10];

        _instructionAccumulator = TimeSpan.Zero;
        _timerAccumulator = TimeSpan.Zero;
        _nextDrawAllowedTimestamp = 0;
        IsHighResolution = false;
        _romLoaded = false;
    }

    private void ResetCurrentRomExecution()
    {
        if (_waitingForRomSelection || _nativeContext?.Display is null)
        {
            return;
        }

        if (!_nativeContext.Display.RomSelected)
        {
            return;
        }

        ResetStateForNewRom();
    }

    private void Update(GameTime gameTime)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        if (_nativeContext is null)
        {
            throw new InvalidOperationException(NativeContextNotInitializedMessage);
        }

        _nativeContext.Update(gameTime);
        if (_nativeContext.Display is null)
        {
            throw new InvalidOperationException("Native display is not initialized.");
        }

        if (_waitingForRomSelection)
        {
            if (_nativeContext.Display.IsRomSelectionInProgress)
            {
                return;
            }

            _waitingForRomSelection = false;

            if (_nativeContext.Display.RomReloadRequested)
            {
                _nativeContext.Display.ClearRomReloadRequest();
                ResetStateForNewRom();
            }

            _paused = _pausedBeforeRomSelection;
        }

        if (!_nativeContext.Display.RomSelected)
        {
            return;
        }
        else if (!_romLoaded)
        {
            var romPath =
                _nativeContext.Display.SelectedRomPath
                ?? throw new InvalidOperationException("ROM path is null.");

            var romBytes = File.ReadAllBytes(romPath);
            if (romBytes.Length > Memory.Length - 0x0200)
            {
                throw new InvalidOperationException("ROM size exceeds available memory.");
            }

            romBytes.CopyTo(Memory.Span[0x0200..]);
            _romLoaded = true;
            CommonLogging.LoadedRom(_logger, romPath);
        }

        if (_paused)
        {
            return;
        }

        Keypad = _nativeContext.Display.SyncKeypad();
        UpdateTimers(gameTime);
        ExecutePendingInstructions(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        if (_nativeContext is null)
        {
            throw new InvalidOperationException("Native context is not initialized.");
        }

        _nativeContext.Draw(gameTime, DisplayBuffer);
    }

    private void ExecutePendingInstructions(GameTime gameTime)
    {
        // Execute CPU cycles at a fixed frequency independent from render/update cadence.
        _instructionAccumulator += gameTime.DeltaTime;
        while (_instructionAccumulator >= _instructionTick)
        {
            Execute(Decode(Fetch()));
            _instructionAccumulator -= _instructionTick;
        }
    }

    private static void Execute(BaseInstruction instruction)
    {
        instruction.Execute();
    }

    private bool ShouldThrottleDrawWait() =>
        Options.Type is InterpreterType.Classic
        || (Options.Type is InterpreterType.SuperChipLegacy && !IsHighResolution);

    private static TimeSpan GetInstructionTick(InterpreterType interpreterType)
    {
        var instructionsPerSecond = interpreterType is InterpreterType.Classic ? 500 : 700;
        return TimeSpan.FromSeconds(1.0 / instructionsPerSecond);
    }

    private void ApplyInterpreterType(InterpreterType interpreterType)
    {
        if (Options.Type == interpreterType)
        {
            return;
        }

        var previousType = Options.Type;
        Options.Type = interpreterType;
        _instructionTick = GetInstructionTick(interpreterType);

        // Preserve the last ROM-selected display mode when moving to SCHIP/XO.
        // Classic does not use 00FE/00FF at runtime but we still remember the intent.
        if (interpreterType is InterpreterType.Classic)
        {
            IsHighResolution = false;
        }

        _nextDrawAllowedTimestamp = 0;
        _instructionAccumulator = TimeSpan.Zero;

        RemapDisplayBuffer(previousType, interpreterType);
        _nativeContext?.Display?.SetInterpreterType(interpreterType);
    }

    private void RemapDisplayBuffer(InterpreterType fromType, InterpreterType toType)
    {
        var sourceDimensions = GetDisplayBufferDimensions(fromType);
        var destinationDimensions = GetDisplayBufferDimensions(toType);

        var source = (byte[])DisplayBuffer.Clone();
        DisplayBuffer.AsSpan().Clear();

        // When switching between 64x32 and 128x64 physical buffers, scale pixels to keep
        // static content visible instead of clearing and waiting for ROM redraw.
        if (sourceDimensions == destinationDimensions)
        {
            CopySameSizeBuffer(source, destinationDimensions);
            return;
        }

        if (CanUpscale2x(sourceDimensions, destinationDimensions))
        {
            Upscale2xBuffer(source, sourceDimensions, destinationDimensions);
            return;
        }

        if (CanDownscale2x(sourceDimensions, destinationDimensions))
        {
            Downscale2xBuffer(source, sourceDimensions, destinationDimensions);
            return;
        }

        CopyOverlappingRegion(source, sourceDimensions, destinationDimensions);
    }

    private static (int Width, int Height) GetDisplayBufferDimensions(
        InterpreterType interpreterType
    ) => interpreterType is InterpreterType.Classic ? (64, 32) : (128, 64);

    private void CopySameSizeBuffer(byte[] source, (int Width, int Height) destinationDimensions)
    {
        Array.Copy(
            source,
            DisplayBuffer,
            destinationDimensions.Width * destinationDimensions.Height
        );
    }

    private static bool CanUpscale2x(
        (int Width, int Height) sourceDimensions,
        (int Width, int Height) destinationDimensions
    ) =>
        sourceDimensions.Width == 64
        && sourceDimensions.Height == 32
        && destinationDimensions.Width == 128
        && destinationDimensions.Height == 64;

    private void Upscale2xBuffer(
        byte[] source,
        (int Width, int Height) sourceDimensions,
        (int Width, int Height) destinationDimensions
    )
    {
        for (var y = 0; y < sourceDimensions.Height; y++)
        {
            for (var x = 0; x < sourceDimensions.Width; x++)
            {
                if (source[(y * sourceDimensions.Width) + x] == 0)
                {
                    continue;
                }

                var dstX = x * 2;
                var dstY = y * 2;
                DisplayBuffer[(dstY * destinationDimensions.Width) + dstX] = 1;
                DisplayBuffer[(dstY * destinationDimensions.Width) + dstX + 1] = 1;
                DisplayBuffer[((dstY + 1) * destinationDimensions.Width) + dstX] = 1;
                DisplayBuffer[((dstY + 1) * destinationDimensions.Width) + dstX + 1] = 1;
            }
        }
    }

    private static bool CanDownscale2x(
        (int Width, int Height) sourceDimensions,
        (int Width, int Height) destinationDimensions
    ) =>
        sourceDimensions.Width == 128
        && sourceDimensions.Height == 64
        && destinationDimensions.Width == 64
        && destinationDimensions.Height == 32;

    private void Downscale2xBuffer(
        byte[] source,
        (int Width, int Height) sourceDimensions,
        (int Width, int Height) destinationDimensions
    )
    {
        for (var y = 0; y < destinationDimensions.Height; y++)
        {
            for (var x = 0; x < destinationDimensions.Width; x++)
            {
                var srcX = x * 2;
                var srcY = y * 2;
                var merged =
                    source[(srcY * sourceDimensions.Width) + srcX]
                    | source[(srcY * sourceDimensions.Width) + srcX + 1]
                    | source[((srcY + 1) * sourceDimensions.Width) + srcX]
                    | source[((srcY + 1) * sourceDimensions.Width) + srcX + 1];

                DisplayBuffer[(y * destinationDimensions.Width) + x] =
                    merged != 0 ? (byte)1 : (byte)0;
            }
        }
    }

    private void CopyOverlappingRegion(
        byte[] source,
        (int Width, int Height) sourceDimensions,
        (int Width, int Height) destinationDimensions
    )
    {
        // Fallback: keep overlapping region if new modes are introduced.
        var copyWidth = Math.Min(sourceDimensions.Width, destinationDimensions.Width);
        var copyHeight = Math.Min(sourceDimensions.Height, destinationDimensions.Height);
        for (var y = 0; y < copyHeight; y++)
        {
            Array.Copy(
                source,
                y * sourceDimensions.Width,
                DisplayBuffer,
                y * destinationDimensions.Width,
                copyWidth
            );
        }
    }

    internal bool TryBeginDraw()
    {
        if (!ShouldThrottleDrawWait())
        {
            return true;
        }

        var now = Stopwatch.GetTimestamp();
        if (now < _nextDrawAllowedTimestamp)
        {
            return false;
        }

        _nextDrawAllowedTimestamp = now + (Stopwatch.Frequency / 60);
        return true;
    }

    internal void SetDisplayMode(bool highResolution)
    {
        // Always remember the ROM-requested mode, even when in Classic.
        // This allows switching into SCHIP/XO and restoring the intended lores/hires state.
        IsHighResolution = highResolution;

        // Avoid carrying over a blocked draw when switching modes.
        _nextDrawAllowedTimestamp = 0;
    }

    internal void ScrollDisplayDown(byte lines)
    {
        var (displayWidth, displayHeight) = GetPhysicalDisplaySize();
        var amount = GetVerticalScrollAmount(lines);
        if (amount <= 0)
        {
            return;
        }

        if (amount >= displayHeight)
        {
            DisplayBuffer.AsSpan().Clear();
            return;
        }

        for (var y = displayHeight - 1; y >= amount; y--)
        {
            var dst = y * displayWidth;
            var src = (y - amount) * displayWidth;
            Array.Copy(DisplayBuffer, src, DisplayBuffer, dst, displayWidth);
        }

        DisplayBuffer.AsSpan(0, amount * displayWidth).Clear();
    }

    internal void ScrollDisplayUp(byte lines)
    {
        var (displayWidth, displayHeight) = GetPhysicalDisplaySize();
        var amount = GetVerticalScrollAmount(lines);
        if (amount <= 0)
        {
            return;
        }

        if (amount >= displayHeight)
        {
            DisplayBuffer.AsSpan().Clear();
            return;
        }

        for (var y = 0; y < displayHeight - amount; y++)
        {
            var dst = y * displayWidth;
            var src = (y + amount) * displayWidth;
            Array.Copy(DisplayBuffer, src, DisplayBuffer, dst, displayWidth);
        }

        DisplayBuffer
            .AsSpan((displayHeight - amount) * displayWidth, amount * displayWidth)
            .Clear();
    }

    internal void ScrollDisplayRight()
    {
        var (displayWidth, displayHeight) = GetPhysicalDisplaySize();
        var amount = GetHorizontalScrollAmount();
        if (amount <= 0)
        {
            return;
        }

        if (amount >= displayWidth)
        {
            DisplayBuffer.AsSpan().Clear();
            return;
        }

        for (var y = 0; y < displayHeight; y++)
        {
            var rowStart = y * displayWidth;
            for (var x = displayWidth - 1; x >= amount; x--)
            {
                DisplayBuffer[rowStart + x] = DisplayBuffer[rowStart + x - amount];
            }

            DisplayBuffer.AsSpan(rowStart, amount).Clear();
        }
    }

    internal void ScrollDisplayLeft()
    {
        var (displayWidth, displayHeight) = GetPhysicalDisplaySize();
        var amount = GetHorizontalScrollAmount();
        if (amount <= 0)
        {
            return;
        }

        if (amount >= displayWidth)
        {
            DisplayBuffer.AsSpan().Clear();
            return;
        }

        for (var y = 0; y < displayHeight; y++)
        {
            var rowStart = y * displayWidth;
            for (var x = 0; x < displayWidth - amount; x++)
            {
                DisplayBuffer[rowStart + x] = DisplayBuffer[rowStart + x + amount];
            }

            DisplayBuffer.AsSpan(rowStart + displayWidth - amount, amount).Clear();
        }
    }

    private (int Width, int Height) GetPhysicalDisplaySize() =>
        Options.Type is InterpreterType.Classic ? (64, 32) : (128, 64);

    private bool UseLegacyLoresHalfPixelScroll() =>
        Options.Type is InterpreterType.SuperChipLegacy && !IsHighResolution;

    private int GetVerticalScrollAmount(byte lines)
    {
        if (UseLegacyLoresHalfPixelScroll())
        {
            return lines;
        }

        var pixelScale = Options.Type is not InterpreterType.Classic && !IsHighResolution ? 2 : 1;
        return lines * pixelScale;
    }

    private int GetHorizontalScrollAmount()
    {
        if (UseLegacyLoresHalfPixelScroll())
        {
            return 4;
        }

        var pixelScale = Options.Type is not InterpreterType.Classic && !IsHighResolution ? 2 : 1;
        return 4 * pixelScale;
    }

    private ushort Fetch()
    {
        var opCode = BinaryPrimitives.ReadUInt16BigEndian(Memory.Span.Slice(ProgramCounter, 2));
        ProgramCounter += 2;
        return opCode;
    }

    private BaseInstruction Decode(ushort opCode)
    {
        BaseInstruction instruction = (opCode & 0xF000) switch
        {
            0x0000 => (opCode & 0x00FF) switch
            {
                0x00E0 => new ClearScreenInstruction(this, opCode),
                0x00EE => new ReturnFromSubroutineInstruction(this, opCode),
                0x00FB => new ScrollRightInstruction(this, opCode),
                0x00FC => new ScrollLeftInstruction(this, opCode),
                0x00FE => new SetLowResolutionInstruction(this, opCode),
                0x00FF => new SetHighResolutionInstruction(this, opCode),
                _ when (opCode & 0x00F0) == 0x00C0 => new ScrollDownInstruction(this, opCode),
                _ when (opCode & 0x00F0) == 0x00D0 => new ScrollUpInstruction(this, opCode),
                _ => new UnknownInstruction(_logger, this, opCode),
            },
            0x1000 => new JumpInstruction(this, opCode),
            0x2000 => new CallSubroutineInstruction(this, opCode),
            0x3000 => new SkipIfEqualInstruction(this, opCode),
            0x4000 => new SkipIfNotEqualInstruction(this, opCode),
            0x5000 => new SkipIfRegistersEqualInstruction(this, opCode),
            0x6000 => new SetRegisterInstruction(this, opCode),
            0x7000 => new AddValueToRegisterInstruction(this, opCode),
            0x8000 => (opCode & 0x000F) switch
            {
                0x0000 => new SetRegisterToRegisterInstruction(this, opCode),
                0x0001 => new BinaryOrInstruction(this, opCode),
                0x0002 => new BinaryAndInstruction(this, opCode),
                0x0003 => new BinaryXorInstruction(this, opCode),
                0x0004 => new AddRegistersInstruction(this, opCode),
                0x0005 => new SubtractYFromXInstruction(this, opCode),
                0x0006 => new ShiftRightInstruction(this, opCode),
                0x0007 => new SubtractXFromYInstruction(this, opCode),
                0x000E => new ShiftLeftInstruction(this, opCode),
                _ => new UnknownInstruction(_logger, this, opCode),
            },
            0x9000 => new SkipIfRegistersNotEqualInstruction(this, opCode),
            0xA000 => new SetIndexRegisterInstruction(this, opCode),
            0xB000 => new JumpWithOffsetInstruction(this, opCode),
            0xC000 => new RandomInstruction(this, opCode),
            0xD000 => new DrawInstruction(this, opCode),
            0xE000 => (opCode & 0x00FF) switch
            {
                0x009E => new SkipIfKeyPressedInstruction(this, opCode),
                0x00A1 => new SkipIfKeyNotPressedInstruction(this, opCode),
                _ => new UnknownInstruction(_logger, this, opCode),
            },
            0xF000 => (opCode & 0x00FF) switch
            {
                0x0007 => new LoadDelayTimerInstruction(this, opCode),
                0x000A => new GetKeyInstruction(this, opCode),
                0x0015 => new SetDelayTimerInstruction(this, opCode),
                0x0018 => new SetSoundTimerInstruction(this, opCode),
                0x001E => new AddToIndexInstruction(this, opCode),
                0x0029 => new FontCharacterInstruction(this, opCode),
                0x0033 => new BinaryCodedDecimalConversionInstruction(this, opCode),
                0x0055 => new StoreInstruction(this, opCode),
                0x0065 => new LoadInstruction(this, opCode),
                _ => new UnknownInstruction(_logger, this, opCode),
            },
            _ => new UnknownInstruction(_logger, this, opCode),
        };

        InstructionLogging.ExecutingInstruction(_logger, ProgramCounter, instruction);
        return instruction;
    }

    private void UpdateSoundTimer()
    {
        if (_nativeContext is null)
        {
            throw new InvalidOperationException(NativeContextNotInitializedMessage);
        }

        if (_nativeContext.Audio is null)
        {
            throw new InvalidOperationException(NativeAudioNotInitializedMessage);
        }

        if (SoundTimer > 0)
        {
            SoundTimer--;

            _nativeContext.Audio.RefillQueue();
            if (_nativeContext.Audio.IsPaused())
            {
                _nativeContext.Audio.Resume();
            }
        }
        else if (!_nativeContext.Audio.IsPaused())
        {
            _nativeContext.Audio.Pause();
        }
    }

    private void UpdateTimers(GameTime gameTime)
    {
        if (_nativeContext is null)
        {
            throw new InvalidOperationException(NativeContextNotInitializedMessage);
        }

        if (_nativeContext.Audio is null)
        {
            throw new InvalidOperationException(NativeAudioNotInitializedMessage);
        }

        // CHIP-8 delay/sound timers tick at 60Hz, independent of CPU instruction rate.
        _timerAccumulator += gameTime.DeltaTime;
        while (_timerAccumulator >= TimerTick)
        {
            if (_paused)
            {
                if (!_nativeContext.Audio.IsPaused())
                {
                    _nativeContext.Audio.Pause();
                }

                break;
            }

            if (DelayTimer > 0)
            {
                DelayTimer--;
            }

            UpdateSoundTimer();

            _timerAccumulator -= TimerTick;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            _nativeContext?.Dispose();
            _gameTime?.Dispose();
        }

        _nativeContext = null;
        _gameTime = null;

        _disposedValue = true;
    }

    ~Interpreter()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
