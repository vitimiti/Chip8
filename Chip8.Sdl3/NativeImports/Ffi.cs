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
using System.Reflection;
using System.Runtime.InteropServices;

namespace Chip8.Sdl3.NativeImports;

[SuppressMessage(
    "csharpsquid",
    "S101:Types should be named in PascalCase",
    Justification = "Types are named after the C types they represent."
)]
internal static partial class Ffi
{
    private const string LibSdl3 = "SDL3";

    static Ffi() => NativeLibrary.SetDllImportResolver(typeof(Ffi).Assembly, ResolveLibrary);

    [SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Consecutive ternary operators would be less readable."
    )]
    private static nint ResolveLibrary(string name, Assembly assembly, DllImportSearchPath? path)
    {
        if (name != LibSdl3)
        {
            return NativeLibrary.Load(name, assembly, path);
        }

        if (OperatingSystem.IsWindows())
        {
            return NativeLibrary.Load("SDL3.dll", assembly, path);
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            if (TryLoadAnyFromDirectory("/app/lib64", out var handle))
            {
                return handle;
            }

            if (TryLoadAnyFromDirectory("/app/lib", out handle))
            {
                return handle;
            }

            if (NativeLibrary.TryLoad("/app/lib64/libSDL3.so.0", out handle))
            {
                return handle;
            }

            if (NativeLibrary.TryLoad("/app/lib/libSDL3.so.0", out handle))
            {
                return handle;
            }

            if (NativeLibrary.TryLoad("libSDL3.so.0", out handle))
            {
                return handle;
            }

            return NativeLibrary.Load("libSDL3.so.0", assembly, path);
        }

        if (OperatingSystem.IsMacOS())
        {
            if (NativeLibrary.TryLoad("libSDL3.dylib", out var handle))
            {
                return handle;
            }

            if (NativeLibrary.TryLoad("libSDL3.0.dylib", out handle))
            {
                return handle;
            }

            return NativeLibrary.Load("libSDL3.dylib", assembly, path);
        }

        return NativeLibrary.Load(name, assembly, path);
    }

    private static bool TryLoadAnyFromDirectory(string directory, out nint handle)
    {
        handle = 0;

        if (!Directory.Exists(directory))
        {
            return false;
        }

        foreach (var candidate in Directory.GetFiles(directory, "libSDL3.so.0*"))
        {
            if (NativeLibrary.TryLoad(candidate, out handle))
            {
                return true;
            }
        }

        return false;
    }
}
