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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Chip8.Abstractions;
using Chip8.Common.Events;
using static Chip8.Sdl3.NativeImports.Ffi;

namespace Chip8.Sdl3;

public class RomSelector : IRomSelector
{
    public event EventHandler<RomSelectedEventArgs>? RomSelected;

    public event EventHandler? SelectionCompleted;

    private delegate void DialogFileCallback(List<string?>? fileList, int filter);

    internal void Show(SDL_Window window)
    {
        var filters = new[]
        {
            new SDL_DialogFileFilter { Name = "CHIP-8 ROMs", Pattern = "ch8;c8;rom" },
            new SDL_DialogFileFilter { Name = "All Files", Pattern = "*" },
        };

        unsafe
        {
            SDL_ShowOpenFileDialog(
                &UnmanagedDialogFileCallback,
                (void*)GCHandle.ToIntPtr(GCHandle.Alloc((DialogFileCallback)DialogFile)),
                window,
                filters,
                filters.Length,
                defaultLocation: null,
                allowMany: false
            );
        }
    }

    private void DialogFile(List<string?>? fileList, int filter)
    {
        if (fileList is { Count: > 0 } && fileList[0] is { } selectedRomPath)
        {
            RomSelected?.Invoke(this, new RomSelectedEventArgs(selectedRomPath));
        }

        SelectionCompleted?.Invoke(this, EventArgs.Empty);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void UnmanagedDialogFileCallback(
        void* userData,
        byte** fileList,
        int filter
    )
    {
        if (userData is null)
        {
            return;
        }

        var handle = GCHandle.FromIntPtr((nint)userData);
        if (!handle.IsAllocated)
        {
            return;
        }

        try
        {
            if (handle.Target is not DialogFileCallback callback)
            {
                return;
            }

            if (fileList is null)
            {
                callback(null, filter);
                return;
            }

            var files = new List<string?>();
            for (var i = 0; fileList[i] is not null; i++)
            {
                var filePath = Utf8StringMarshaller.ConvertToManaged(fileList[i]);
                files.Add(filePath);
            }

            if (files.Count == 0)
            {
                files.Add(null);
            }

            callback(files, filter);
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }
}
