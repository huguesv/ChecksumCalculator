﻿// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Woohoo.ChecksumCalculator.AvaloniaDesktop.Services;

public partial class FileResultViewModel : ObservableObject
{
    private readonly IFileExplorerService fileExplorerService;
    private readonly IClipboardService clipboardService;

    [ObservableProperty]
    private bool isCalculating = false;

    [ObservableProperty]
    private bool isExpanded = true;

    [ObservableProperty]
    private int fileProgress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FolderPath))]
    [NotifyPropertyChangedFor(nameof(Name))]
    private string fullPath = string.Empty;

    [ObservableProperty]
    private string crc32Hash = string.Empty;

    [ObservableProperty]
    private string md5Hash = string.Empty;

    [ObservableProperty]
    private string sha1Hash = string.Empty;

    [ObservableProperty]
    private string sha256Hash = string.Empty;

    [ObservableProperty]
    private string sha384Hash = string.Empty;

    [ObservableProperty]
    private string sha512Hash = string.Empty;

    [ObservableProperty]
    private long fileSize = 0;

    [ObservableProperty]
    private double durationSecs = 0;

    public FileResultViewModel(IFileExplorerService fileExplorerService, IClipboardService clipboardService)
    {
        this.fileExplorerService = fileExplorerService;
        this.clipboardService = clipboardService;

        this.BrowseCommand = new RelayCommand(this.Browse);
        this.CopyCommand = new RelayCommand(this.Copy);
    }

    public string Name => Path.GetFileName(this.FullPath) ?? string.Empty;

    public string FolderPath => Path.GetDirectoryName(this.FullPath) ?? string.Empty;

    public ObservableCollection<HashResultViewModel> Hashes { get; set; } = new();

    public Guid Id { get; init; }

    public IRelayCommand BrowseCommand { get; }

    public IRelayCommand CopyCommand { get; }

    public void Browse()
    {
        this.fileExplorerService.OpenInExplorer(this.FullPath);
    }

    public void Copy()
    {
        var current = new StringBuilder();

        _ = current.AppendLine(string.Format(Localized.ResultFile, this.Name));
        _ = current.AppendLine(string.Format(Localized.ResultSize, this.FileSize));

        foreach (var hash in this.Hashes)
        {
            _ = current.AppendLine(string.Format(Localized.ResultChecksum, hash.Algorithm, hash.Value));
        }

        this.clipboardService.SetText(current.ToString());
    }

    public void AddHash(string algorithm, string value)
    {
        this.Hashes.Add(new() { Algorithm = algorithm, Value = value });

        if (string.Equals(algorithm, "CRC32", StringComparison.OrdinalIgnoreCase))
        {
            this.Crc32Hash = value;
        }
        else if (string.Equals(algorithm, "MD5", StringComparison.OrdinalIgnoreCase))
        {
            this.Md5Hash = value;
        }
        else if (string.Equals(algorithm, "SHA1", StringComparison.OrdinalIgnoreCase))
        {
            this.Sha1Hash = value;
        }
        else if (string.Equals(algorithm, "SHA256", StringComparison.OrdinalIgnoreCase))
        {
            this.Sha256Hash = value;
        }
        else if (string.Equals(algorithm, "SHA384", StringComparison.OrdinalIgnoreCase))
        {
            this.Sha384Hash = value;
        }
        else if (string.Equals(algorithm, "SHA512", StringComparison.OrdinalIgnoreCase))
        {
            this.Sha512Hash = value;
        }
    }
}
