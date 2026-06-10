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
    private readonly TimeSpan _instructionTick;

    private GameTime? _gameTime;
    private INativeContext? _nativeContext;
    private bool _running;
    private bool _romLoaded;
    private TimeSpan _instructionAccumulator;
    private TimeSpan _timerAccumulator;
    private bool _paused;
    private bool _waitingForRomSelection;
    private bool _pausedBeforeRomSelection;
    private bool _legacyDrawAllowed;
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

        var instructionsPerSecond = Options.Type is InterpreterType.Legacy ? 500 : 700;
        _instructionTick = TimeSpan.FromSeconds(1.0 / instructionsPerSecond);

        DisplayBuffer = new byte[Options.Type is InterpreterType.Legacy ? 64 * 32 : 128 * 64];
        _legacyDrawAllowed = true;
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
        _legacyDrawAllowed = true;
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

    internal bool TryBeginDraw()
    {
        if (Options.Type is not InterpreterType.Legacy)
        {
            return true;
        }

        if (!_legacyDrawAllowed)
        {
            return false;
        }

        _legacyDrawAllowed = false;
        return true;
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

            if (Options.Type is InterpreterType.Legacy)
            {
                _legacyDrawAllowed = true;
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
