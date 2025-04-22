// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop.Services;

using System.Threading.Tasks;

public interface IFilePickerService
{
    Task<string[]> GetFilePathsAsync(bool multiselect);
}
