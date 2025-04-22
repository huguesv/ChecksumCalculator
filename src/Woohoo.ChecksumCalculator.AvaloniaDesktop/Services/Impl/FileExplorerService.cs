// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop.Services;

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

internal class FileExplorerService : IFileExplorerService
{
    public void OpenInExplorer(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("explorer", $"/select,{filePath}");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", $"-R \"{filePath}\"");
        }
        else
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = Path.GetDirectoryName(filePath),
                UseShellExecute = true,
            });
        }
    }
}
