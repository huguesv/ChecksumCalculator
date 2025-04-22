// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop.Services;

using System;

internal class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        var window = App.GetTopLevel(App.Current ?? throw new InvalidOperationException());
        window?.Clipboard?.SetTextAsync(text);
    }
}
