﻿// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

internal class FilePickerService : IFilePickerService
{
    public async Task<string[]> GetFilePathsAsync(bool multiselect)
    {
        var window = App.GetTopLevel(App.Current ?? throw new InvalidOperationException());
        if (window is null)
        {
            throw new InvalidOperationException();
        }

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Localized.BrowseDialogTitle,
            AllowMultiple = true,
        });

        var filePaths = new List<string>();
        foreach (var file in files)
        {
            string? path = file.TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
            {
                filePaths.Add(path);
            }
        }

        return filePaths.ToArray();
    }
}
