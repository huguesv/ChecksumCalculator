// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop.Views;

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Woohoo.ChecksumCalculator.AvaloniaDesktop.ViewModels;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        this.AddHandler(DragDrop.DropEvent, this.OnDrop);
    }

    public void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles() ?? Array.Empty<IStorageItem>();
            var filePaths = new List<string>();

            foreach (var item in files)
            {
                if (item is IStorageFile file)
                {
                    string? path = file.TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        filePaths.Add(path);
                    }
                }
                else if (item is IStorageFolder folder)
                {
                    string? path = folder.TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        filePaths.AddRange(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
                    }
                }
            }

            if (filePaths.Count > 0)
            {
                (this.DataContext as MainWindowViewModel)?.CalculateChecksums(filePaths);
            }
        }
    }

    private void Exit_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Environment.Exit(0);
    }
}
