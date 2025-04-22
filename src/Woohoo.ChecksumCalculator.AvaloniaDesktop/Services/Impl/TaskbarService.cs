// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop.Services;

using System.Runtime.InteropServices;
using Woohoo.Platform.Windows.Taskbar;

internal class TaskbarService : ITaskbarService
{
    public void SetProgressValue(int currentValue, int maximumValue)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            TaskbarManager.Instance.SetProgressValue(currentValue, maximumValue);
        }
    }
}
