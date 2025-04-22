// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop.ViewModels.DesignTime;

using Woohoo.ChecksumCalculator.AvaloniaDesktop.Services;

public class DesignFileResultViewModel : FileResultViewModel
{
    public DesignFileResultViewModel()
        : base(new NullFileExplorerService(), new NullClipboardService())
    {
        this.FullPath = @"C:\FolderName\LICENSE";
        this.FileSize = 1096;
        this.AddHash("CRC32", "44b8c62b");
        this.AddHash("MD5", "35978c843cd1e203a55a0f866a964827");
        this.AddHash("SHA1", "648dc68133834d28611bd4c5398d4a4bfca101c3");
    }
}
