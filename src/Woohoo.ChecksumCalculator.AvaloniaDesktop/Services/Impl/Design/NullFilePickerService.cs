// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop.Services;

using System;
using System.Threading.Tasks;

internal class NullFilePickerService : IFilePickerService
{
    public Task<string[]> GetFilePathsAsync(bool multiselect)
    {
        return Task.FromResult(Array.Empty<string>());
    }
}
