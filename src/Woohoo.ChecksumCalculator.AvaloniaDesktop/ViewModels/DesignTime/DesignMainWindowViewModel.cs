// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop.ViewModels.DesignTime;

using Woohoo.ChecksumCalculator.AvaloniaDesktop.Services;

public class DesignMainWindowViewModel : MainWindowViewModel
{
    public DesignMainWindowViewModel()
        : base(new NullFileExplorerService(), new NullClipboardService(), new NullFilePickerService(), new NullTaskbarService())
    {
    }
}
