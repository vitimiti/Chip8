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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace Chip8.Sdl3.NativeImports;

internal static unsafe partial class Ffi
{
    #region Marshallers

    [CustomMarshaller(
        typeof(string),
        MarshalMode.ManagedToUnmanagedOut,
        typeof(UnownedUtf8StringMarshaller)
    )]
    private static class UnownedUtf8StringMarshaller
    {
        public static string? ConvertToManaged(byte* unmanaged) =>
            Utf8StringMarshaller.ConvertToManaged(unmanaged);
    }

    [CustomMarshaller(typeof(SDL_DialogFileFilter), MarshalMode.ElementIn, typeof(ElementIn))]
    private static class DialogFilterMarshaller
    {
        public static class ElementIn
        {
            public static SDL_DialogFileFilterUnmanaged ConvertToUnmanaged(
                SDL_DialogFileFilter managed
            ) =>
                new()
                {
                    Name = Utf8StringMarshaller.ConvertToUnmanaged(managed.Name),
                    Pattern = Utf8StringMarshaller.ConvertToUnmanaged(managed.Pattern),
                };

            [SuppressMessage(
                "csharpsquid",
                "S1144:Unused private types or members should be removed",
                Justification = "Required by the marshalling system."
            )]
            public static SDL_DialogFileFilter ConvertToManaged(
                SDL_DialogFileFilterUnmanaged unmanaged
            ) =>
                new()
                {
                    Name = Utf8StringMarshaller.ConvertToManaged(unmanaged.Name),
                    Pattern = Utf8StringMarshaller.ConvertToManaged(unmanaged.Pattern),
                };

            public static void Free(SDL_DialogFileFilterUnmanaged unmanaged)
            {
                Utf8StringMarshaller.Free(unmanaged.Name);
                Utf8StringMarshaller.Free(unmanaged.Pattern);
            }
        }
    }

    #endregion // Marshallers

    #region SDL_audio.h

    public readonly record struct SDL_AudioFormat(uint Value);

    public static SDL_AudioFormat SDL_AUDIO_FORMAT_S16LE => new(0x0000_8010U);
    public static SDL_AudioFormat SDL_AUDIO_FORMAT_S16BE => new(0x0000_9010U);
    public static SDL_AudioFormat SDL_AUDIO_FORMAT_S16 =>
        BitConverter.IsLittleEndian ? SDL_AUDIO_FORMAT_S16LE : SDL_AUDIO_FORMAT_S16BE;

    public readonly record struct SDL_AudioDeviceID(uint Value)
    {
        public bool IsInvalid => Value == 0;
    }

    public static SDL_AudioDeviceID SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK => new(0xFFFF_FFFFU);

    [NativeMarshalling(typeof(SafeHandleMarshaller<SDL_AudioStream>))]
    public sealed class SDL_AudioStream : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SDL_AudioStream()
            : base(ownsHandle: true) => SetHandle(0);

        protected override bool ReleaseHandle()
        {
            SDL_DestroyAudioStream(handle);
            return true;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_AudioSpec
    {
        public SDL_AudioFormat Format;
        public int Channels;
        public int Frequency;
    }

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_AudioStream SDL_OpenAudioDeviceStream(
        SDL_AudioDeviceID devId,
        in SDL_AudioSpec spec,
        void* callback,
        void* userdata
    );

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_AudioDeviceID SDL_GetAudioStreamDevice(SDL_AudioStream stream);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_SetAudioDeviceGain(SDL_AudioDeviceID devId, float gain);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_AudioStreamDevicePaused(SDL_AudioStream stream);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int SDL_GetAudioStreamQueued(SDL_AudioStream stream);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_PutAudioStreamData(
        SDL_AudioStream stream,
        void* buffer,
        int length
    );

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_ResumeAudioStreamDevice(SDL_AudioStream stream);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_PauseAudioStreamDevice(SDL_AudioStream stream);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void SDL_DestroyAudioStream(nint stream);

    #endregion // SDL_audio.h

    #region SDL_dialog.h

    [NativeMarshalling(typeof(DialogFilterMarshaller))]
    public struct SDL_DialogFileFilter
    {
        public string? Name;
        public string? Pattern;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SDL_DialogFileFilterUnmanaged
    {
        public byte* Name;
        public byte* Pattern;
    }

    [LibraryImport(LibSdl3, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_ShowOpenFileDialog(
        delegate* unmanaged[Cdecl]<void*, byte**, int, void> callback,
        void* userData,
        SDL_Window window,
        [In]
        [MarshalUsing(
            typeof(ArrayMarshaller<SDL_DialogFileFilter, SDL_DialogFileFilterUnmanaged>),
            CountElementName = nameof(nFilters)
        )]
            SDL_DialogFileFilter[] filters,
        int nFilters,
        string? defaultLocation,
        [MarshalAs(UnmanagedType.I1)] bool allowMany
    );

    #endregion // SDL_dialog.h

    #region SDL_error.h

    [LibraryImport(
        LibSdl3,
        StringMarshalling = StringMarshalling.Custom,
        StringMarshallingCustomType = typeof(UnownedUtf8StringMarshaller)
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial string SDL_GetError();

    #endregion // SDL_error.h

    #region SDL_events.h

    public readonly record struct SDL_EventType(uint Value);

    public static SDL_EventType SDL_EVENT_QUIT => new(0x0000_0100U);

    [StructLayout(LayoutKind.Explicit)]
    public struct SDL_Event
    {
        [FieldOffset(0)]
        public SDL_EventType Type;

        [FieldOffset(0)]
        private fixed byte _padding[128];
    }

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_PollEvent(out SDL_Event @event);

    #endregion // SDL_events.h

    #region SDL_init.h

    public readonly record struct SDL_InitFlags(uint Value)
    {
        public static SDL_InitFlags operator |(SDL_InitFlags left, SDL_InitFlags right) =>
            new(left.Value | right.Value);
    }

    public static SDL_InitFlags SDL_INIT_AUDIO => new(0x0000_0010U);
    public static SDL_InitFlags SDL_INIT_VIDEO => new(0x0000_0020U);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_InitSubSystem(SDL_InitFlags flags);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_Quit();

    [LibraryImport(LibSdl3, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_SetAppMetadata(
        string? appName,
        string? appVersion,
        string? appIdentifier
    );

    #endregion // SDL_init.h

    #region SDL_keyboard.h

    public static bool[] SDL_GetKeyboardState()
    {
        var unmanaged = SDL_GetKeyboardStateNative(out var numKeys);
        if (unmanaged is null || numKeys <= 0)
        {
            return [];
        }

        var managed = new bool[numKeys];
        for (var i = 0; i < numKeys; i++)
        {
            managed[i] = unmanaged[i] != 0;
        }

        return managed;
    }

    [LibraryImport(LibSdl3, EntryPoint = "SDL_GetKeyboardState")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial sbyte* SDL_GetKeyboardStateNative(out int numKeys);

    #endregion // SDL_keyboard.h

    #region SDL_log.h

    public readonly record struct SDL_LogCategory(int Value)
    {
        [SuppressMessage(
            "Style",
            "IDE0046:Convert to conditional expression",
            Justification = "Nested conditional expressions are difficult to read."
        )]
        public override string ToString()
        {
            if (this == SDL_LOG_CATEGORY_APPLICATION)
            {
                return "Application";
            }
            if (this == SDL_LOG_CATEGORY_ERROR)
            {
                return "Error";
            }
            if (this == SDL_LOG_CATEGORY_ASSERT)
            {
                return "Assert";
            }
            if (this == SDL_LOG_CATEGORY_SYSTEM)
            {
                return "System";
            }
            if (this == SDL_LOG_CATEGORY_AUDIO)
            {
                return "Audio";
            }
            if (this == SDL_LOG_CATEGORY_VIDEO)
            {
                return "Video";
            }
            if (this == SDL_LOG_CATEGORY_RENDER)
            {
                return "Render";
            }
            if (this == SDL_LOG_CATEGORY_INPUT)
            {
                return "Input";
            }
            if (this == SDL_LOG_CATEGORY_TEST)
            {
                return "Test";
            }
            if (this == SDL_LOG_CATEGORY_GPU)
            {
                return "Gpu";
            }

            return $"Unknown({Value})";
        }
    }

    public static SDL_LogCategory SDL_LOG_CATEGORY_APPLICATION => new(0);
    public static SDL_LogCategory SDL_LOG_CATEGORY_ERROR => new(1);
    public static SDL_LogCategory SDL_LOG_CATEGORY_ASSERT => new(2);
    public static SDL_LogCategory SDL_LOG_CATEGORY_SYSTEM => new(3);
    public static SDL_LogCategory SDL_LOG_CATEGORY_AUDIO => new(4);
    public static SDL_LogCategory SDL_LOG_CATEGORY_VIDEO => new(5);
    public static SDL_LogCategory SDL_LOG_CATEGORY_RENDER => new(6);
    public static SDL_LogCategory SDL_LOG_CATEGORY_INPUT => new(7);
    public static SDL_LogCategory SDL_LOG_CATEGORY_TEST => new(8);
    public static SDL_LogCategory SDL_LOG_CATEGORY_GPU => new(9);

    public readonly record struct SDL_LogPriority(int Value)
    {
        [SuppressMessage(
            "Style",
            "IDE0046:Convert to conditional expression",
            Justification = "Nested conditional expressions are difficult to read."
        )]
        public static SDL_LogPriority FromLogger(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                return SDL_LOG_PRIORITY_TRACE;
            }
            if (logger.IsEnabled(LogLevel.Debug))
            {
                return SDL_LOG_PRIORITY_DEBUG;
            }
            if (logger.IsEnabled(LogLevel.Information))
            {
                return SDL_LOG_PRIORITY_INFO;
            }
            if (logger.IsEnabled(LogLevel.Warning))
            {
                return SDL_LOG_PRIORITY_WARN;
            }
            if (logger.IsEnabled(LogLevel.Error))
            {
                return SDL_LOG_PRIORITY_ERROR;
            }
            if (logger.IsEnabled(LogLevel.Critical))
            {
                return SDL_LOG_PRIORITY_CRITICAL;
            }

            return SDL_LOG_PRIORITY_INVALID;
        }
    }

    public static SDL_LogPriority SDL_LOG_PRIORITY_INVALID => new(0);
    public static SDL_LogPriority SDL_LOG_PRIORITY_TRACE => new(1);
    public static SDL_LogPriority SDL_LOG_PRIORITY_VERBOSE => new(2);
    public static SDL_LogPriority SDL_LOG_PRIORITY_DEBUG => new(3);
    public static SDL_LogPriority SDL_LOG_PRIORITY_INFO => new(4);
    public static SDL_LogPriority SDL_LOG_PRIORITY_WARN => new(5);
    public static SDL_LogPriority SDL_LOG_PRIORITY_ERROR => new(6);
    public static SDL_LogPriority SDL_LOG_PRIORITY_CRITICAL => new(7);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_SetLogPriorities(SDL_LogPriority priority);

    public static GCHandle SDL_LogOutputFunction { get; set; }

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_SetLogOutputFunction(
        delegate* unmanaged[Cdecl]<
            void*,
            SDL_LogCategory,
            SDL_LogPriority,
            byte*,
            void> logOutputFunction,
        void* userData
    );

    #endregion // SDL_log.h

    #region SDL_main.h

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_SetMainReady();

    #endregion // SDL_main.h

    #region SDL_messagebox.h

    public readonly record struct SDL_MessageBoxFlags(uint Value);

    public static SDL_MessageBoxFlags SDL_MESSAGEBOX_ERROR => new(0x0000_0010U);

    [LibraryImport(LibSdl3, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_ShowSimpleMessageBox(
        SDL_MessageBoxFlags flags,
        string title,
        string message,
        nint window
    );

    #endregion // SDL_messagebox.h

    #region SDL_render.h

    public readonly record struct SDL_RendererLogicalPresentation(int Value);

    public static SDL_RendererLogicalPresentation SDL_LOGICAL_PRESENTATION_LETTERBOX => new(2);

    [NativeMarshalling(typeof(SafeHandleMarshaller<SDL_Renderer>))]
    public sealed class SDL_Renderer : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SDL_Renderer()
            : base(ownsHandle: true) => SetHandle(0);

        protected override bool ReleaseHandle()
        {
            SDL_DestroyRenderer(handle);
            return true;
        }
    }

    [LibraryImport(LibSdl3, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_Renderer SDL_CreateRenderer(SDL_Window window, string? name);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_SetRenderLogicalPresentation(
        SDL_Renderer renderer,
        int w,
        int h,
        SDL_RendererLogicalPresentation mode
    );

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_SetRenderDrawColor(
        SDL_Renderer renderer,
        byte r,
        byte g,
        byte b,
        byte a
    );

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_RenderClear(SDL_Renderer renderer);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_RenderPresent(SDL_Renderer renderer);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_RenderPoint(SDL_Renderer renderer, float x, float y);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void SDL_DestroyRenderer(nint renderer);

    #endregion // SDL_render.h

    #region SDL_scancode.h

    public readonly record struct SDL_Scancode(int Value)
    {
        public static implicit operator int(SDL_Scancode scancode) => scancode.Value;
    }

    public static SDL_Scancode SDL_SCANCODE_A => new(4);
    public static SDL_Scancode SDL_SCANCODE_C => new(6);
    public static SDL_Scancode SDL_SCANCODE_D => new(7);
    public static SDL_Scancode SDL_SCANCODE_E => new(8);
    public static SDL_Scancode SDL_SCANCODE_F => new(9);
    public static SDL_Scancode SDL_SCANCODE_Q => new(20);
    public static SDL_Scancode SDL_SCANCODE_R => new(21);
    public static SDL_Scancode SDL_SCANCODE_S => new(22);
    public static SDL_Scancode SDL_SCANCODE_V => new(25);
    public static SDL_Scancode SDL_SCANCODE_W => new(26);
    public static SDL_Scancode SDL_SCANCODE_X => new(27);
    public static SDL_Scancode SDL_SCANCODE_Z => new(29);
    public static SDL_Scancode SDL_SCANCODE_1 => new(30);
    public static SDL_Scancode SDL_SCANCODE_2 => new(31);
    public static SDL_Scancode SDL_SCANCODE_3 => new(32);
    public static SDL_Scancode SDL_SCANCODE_4 => new(33);

    #endregion // SDL_scancode.h

    #region SDL_stdinc.h

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void* SDL_malloc(nuint size);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void SDL_free(void* mem);

    #endregion // SDL_stdinc.h

    #region SDL_video.h

    [NativeMarshalling(typeof(SafeHandleMarshaller<SDL_Window>))]
    public sealed class SDL_Window : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SDL_Window()
            : base(ownsHandle: true) => SetHandle(0);

        protected override bool ReleaseHandle()
        {
            SDL_DestroyWindow(handle);
            return true;
        }
    }

    public readonly record struct SDL_WindowFlags(ulong Value);

    public static SDL_WindowFlags SDL_WINDOW_RESIZABLE => new(0x0000_0000_0000_0020UL);

    [LibraryImport(LibSdl3, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_Window SDL_CreateWindow(
        string title,
        int w,
        int h,
        SDL_WindowFlags flags
    );

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void SDL_DestroyWindow(nint window);

    #endregion // SDL_video.h
}
