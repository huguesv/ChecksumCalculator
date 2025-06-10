// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Woohoo.ChecksumCalculator.AvaloniaDesktop.Services;
using Woohoo.Security.Cryptography;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IFileExplorerService fileExplorerService;
    private readonly IClipboardService clipboardService;
    private readonly IFilePickerService filePickerService;
    private readonly ITaskbarService taskbarService;
    private readonly BackgroundWorker worker;

    public MainWindowViewModel(
        IFileExplorerService fileExplorerService,
        IClipboardService clipboardService,
        IFilePickerService filePickerService,
        ITaskbarService taskbarService)
    {
        this.fileExplorerService = fileExplorerService;
        this.clipboardService = clipboardService;
        this.filePickerService = filePickerService;
        this.taskbarService = taskbarService;

        this.worker = new BackgroundWorker();
        this.worker.DoWork += this.Worker_DoWork;
        this.worker.ProgressChanged += this.Worker_ProgressChanged;
        this.worker.RunWorkerCompleted += this.Worker_RunWorkerCompleted;
        this.worker.WorkerReportsProgress = true;
        this.worker.WorkerSupportsCancellation = true;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectFilesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleAlgorithmCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool IsCalculating { get; set; } = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopySelectedCommand))]
    public partial FileResultViewModel? SelectedItem { get; set; } = null;

    [ObservableProperty]
    public partial bool ShowAbout { get; set; } = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CollapseAllCommand))]
    public partial bool ShowAsGrid { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowToolbar { get; set; } = false;

    [ObservableProperty]
    public partial int FileProgress { get; set; }

    [ObservableProperty]
    public partial int TotalProgress { get; set; }

    [ObservableProperty]
    public partial bool HashCrc32 { get; set; } = true;

    [ObservableProperty]
    public partial bool HashMd5 { get; set; } = true;

    [ObservableProperty]
    public partial bool HashSha1 { get; set; } = true;

    [ObservableProperty]
    public partial bool HashSha256 { get; set; } = false;

    [ObservableProperty]
    public partial bool HashSha384 { get; set; } = false;

    [ObservableProperty]
    public partial bool HashSha512 { get; set; } = false;

    public ObservableCollection<FileResultViewModel> Results { get; set; } = [];

    public bool ShowMenu { get; } = !RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public string AboutMessage => string.Format(Localized.AboutInfo_VersionFormat, Assembly.GetExecutingAssembly().GetName().Version);

    [RelayCommand(CanExecute = nameof(CanCopySelected))]
    public void CopySelected()
    {
        if (this.SelectedItem is not null)
        {
            this.SelectedItem.Copy();
        }
    }

    public bool CanCopySelected() => this.SelectedItem is not null;

    [RelayCommand(CanExecute = nameof(CanSelectFiles))]
    public async Task SelectFilesAsync()
    {
        var filePaths = await this.filePickerService.GetFilePathsAsync(true);
        if (filePaths.Length > 0)
        {
            this.CalculateChecksums(filePaths);
        }
    }

    public bool CanSelectFiles() => !this.IsCalculating;

    [RelayCommand(CanExecute = nameof(CanToggleAlgorithm))]
    public void ToggleAlgorithm(string? algo)
    {
        switch (algo?.ToLower())
        {
            case "crc32":
                this.HashCrc32 = !this.HashCrc32;
                break;
            case "md5":
                this.HashMd5 = !this.HashMd5;
                break;
            case "sha1":
                this.HashSha1 = !this.HashSha1;
                break;
            case "sha256":
                this.HashSha256 = !this.HashSha256;
                break;
            case "sha384":
                this.HashSha384 = !this.HashSha384;
                break;
            case "sha512":
                this.HashSha512 = !this.HashSha512;
                break;
            default:
                Debug.Assert(false, $"Unknown algorithm: {algo}");
                break;
        }
    }

    public bool CanToggleAlgorithm(string? algo) => !this.IsCalculating;

    [RelayCommand(CanExecute = nameof(CanCollapseAll))]
    public void CollapseAll()
    {
        foreach (var result in this.Results)
        {
            result.IsExpanded = false;
        }
    }

    public bool CanCollapseAll() => this.Results.Count > 0 && !this.ShowAsGrid;

    [RelayCommand(CanExecute = nameof(CanClear))]
    public void Clear()
    {
        this.Results.Clear();
        this.CollapseAllCommand.NotifyCanExecuteChanged();
        this.ClearCommand.NotifyCanExecuteChanged();
    }

    public bool CanClear() => !this.IsCalculating && this.Results.Count > 0;

    [RelayCommand]
    public void ToggleShowAsGrid()
    {
        this.ShowAsGrid = !this.ShowAsGrid;
    }

    [RelayCommand]
    public void ToggleShowToolbar()
    {
        this.ShowToolbar = !this.ShowToolbar;
    }

    [RelayCommand]
    public void About()
    {
        // Dismissing it does not reset it to false,
        // so cycle false/true to ensure it is shown.
        this.ShowAbout = false;
        this.ShowAbout = true;
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    public void Cancel()
    {
        if (!this.IsCalculating)
        {
            return;
        }

        this.worker.CancelAsync();
    }

    public bool CanCancel() => this.IsCalculating;

    public IEnumerable<string> GetHashNames()
    {
        if (this.HashCrc32)
        {
            yield return "CRC32";
        }

        if (this.HashMd5)
        {
            yield return "MD5";
        }

        if (this.HashSha1)
        {
            yield return "SHA1";
        }

        if (this.HashSha256)
        {
            yield return "SHA256";
        }

        if (this.HashSha384)
        {
            yield return "SHA384";
        }

        if (this.HashSha512)
        {
            yield return "SHA512";
        }
    }

    public void CalculateChecksums(IEnumerable<string> filePaths)
    {
        var existingFilePaths = filePaths.Where(File.Exists).ToArray();
        if (!existingFilePaths.Any())
        {
            return;
        }

        var hashNames = this.GetHashNames().ToArray();

        this.IsCalculating = true;

        this.worker.RunWorkerAsync(new Tuple<string[], string[]>(hashNames, existingFilePaths));
    }

    private void Worker_DoWork(object? sender, DoWorkEventArgs e)
    {
        if (this.worker.CancellationPending)
        {
            e.Cancel = true;
            return;
        }

        if (e.Argument is null)
        {
            return;
        }

        var calc = new HashCalculator();
        var arg = (Tuple<string[], string[]>)e.Argument;
        var filePaths = arg.Item2;
        var fileInfos = filePaths.Select(f => new FileInfo(f)).ToArray();
        var fileIds = filePaths.ToDictionary(f => f, f => Guid.NewGuid(), StringComparer.OrdinalIgnoreCase);

        long totalBytes = fileInfos.Select(fi => fi.Length).Sum();
        long completedFilesBytes = 0;

        void ProgressHandler(object? sender, HashCalculatorProgressEventArgs ea)
        {
            if (this.worker.CancellationPending)
            {
                calc.Cancel();
                e.Cancel = true;
                return;
            }

            var progress = new ChecksumProgress()
            {
                Id = fileIds[ea.FilePath],
                FilePath = ea.FilePath,
                FileProgress = ea.ProgressBytes,
                FileTotal = ea.Length,
                BatchProgress = completedFilesBytes + ea.ProgressBytes,
                BatchTotal = totalBytes,
                Hashes = null,
            };

            this.worker.ReportProgress(0, progress);
        }

        calc.Progress += ProgressHandler;

        for (int i = 0; i < fileInfos.Length; i++)
        {
            var filePath = fileInfos[i].FullName;

            var sum = calc.Calculate(arg.Item1, filePath);

            if (this.worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            completedFilesBytes += fileInfos[i].Length;

            var progress = new ChecksumProgress()
            {
                Id = fileIds[filePath],
                FilePath = filePath,
                FileProgress = fileInfos[i].Length,
                FileTotal = fileInfos[i].Length,
                BatchProgress = completedFilesBytes,
                BatchTotal = totalBytes,
                Hashes = sum,
            };

            this.worker.ReportProgress(0, progress);
        }

        calc.Progress -= ProgressHandler;
    }

    private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        if (e.UserState is null)
        {
            return;
        }

        var progress = (ChecksumProgress)e.UserState;

        if (progress.FileTotal > 0)
        {
            this.FileProgress = (int)(progress.FileProgress * 100.0 / progress.FileTotal);
        }
        else
        {
            this.FileProgress = 100;
        }

        if (progress.BatchTotal > 0)
        {
            this.TotalProgress = (int)(progress.BatchProgress * 100.0 / progress.BatchTotal);
        }
        else
        {
            this.TotalProgress = 100;
        }

        this.taskbarService.SetProgressValue(this.TotalProgress, 100);

        var fileResultViewModel = this.Results.FirstOrDefault(vm => vm.Id == progress.Id);
        if (fileResultViewModel is null)
        {
            long length = progress.FileTotal;

            fileResultViewModel = new FileResultViewModel(this.fileExplorerService, this.clipboardService)
            {
                Id = progress.Id,
                FullPath = progress.FilePath,
                FileSize = length,
                IsCalculating = true,
            };

            this.Results.Add(fileResultViewModel);
            this.CollapseAllCommand.NotifyCanExecuteChanged();
            this.ClearCommand.NotifyCanExecuteChanged();
        }

        fileResultViewModel.FileProgress = this.FileProgress;

        var sum = progress.Hashes;
        if (sum != null)
        {
            double secs = sum.CalculationTime.TotalSeconds;

            fileResultViewModel.DurationSecs = secs;
            fileResultViewModel.IsCalculating = false;

            foreach (var hash in sum.Checksums)
            {
                string hashVal = HashCalculator.HexToString(sum.Checksums[hash.Key]);
                fileResultViewModel.AddHash(hash.Key, hashVal);
            }
        }
    }

    private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        this.IsCalculating = false;

        if (e.Cancelled)
        {
            foreach (var result in this.Results.Where(r => r.IsCalculating))
            {
                result.IsCalculating = false;
            }
        }
    }

    private class ChecksumProgress
    {
        public Guid Id { get; set; }

        public string FilePath { get; set; } = string.Empty;

        public long FileProgress { get; set; }

        public long FileTotal { get; set; }

        public long BatchProgress { get; set; }

        public long BatchTotal { get; set; }

        public HashCalculatorResult? Hashes { get; set; }
    }
}
