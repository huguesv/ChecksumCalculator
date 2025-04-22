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

    [ObservableProperty]
    private bool isCalculating = false;

    [ObservableProperty]
    private bool showAbout = false;

    [ObservableProperty]
    private bool showAsGrid = true;

    [ObservableProperty]
    private bool showToolbar = false;

    [ObservableProperty]
    private int fileProgress;

    [ObservableProperty]
    private int totalProgress;

    [ObservableProperty]
    private bool hashCrc32 = true;

    [ObservableProperty]
    private bool hashMd5 = true;

    [ObservableProperty]
    private bool hashSha1 = true;

    [ObservableProperty]
    private bool hashSha256 = false;

    [ObservableProperty]
    private bool hashSha384 = false;

    [ObservableProperty]
    private bool hashSha512 = false;

    [ObservableProperty]
    private FileResultViewModel? selectedItem = null;

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

        this.SelectFilesCommand = new AsyncRelayCommand(this.SelectFilesAsync, () => !this.IsCalculating);
        this.ToggleAlgorithmCommand = new RelayCommand<string>(this.ToggleAlgorithm, (string? algo) => !this.IsCalculating);
        this.CollapseAllCommand = new RelayCommand(this.CollapseAll, () => this.Results.Count > 0 && !this.ShowAsGrid);
        this.ClearCommand = new RelayCommand(this.Clear, () => !this.IsCalculating && this.Results.Count > 0);
        this.ToggleShowAsGridCommand = new RelayCommand(this.ToggleShowAsGrid, () => !this.IsCalculating);
        this.ToggleShowToolbarCommand = new RelayCommand(this.ToggleShowToolbar);
        this.AboutCommand = new RelayCommand(this.About, () => !this.IsCalculating);
        this.CancelCommand = new RelayCommand(this.Cancel, () => this.IsCalculating);
        this.CopySelectedCommand = new RelayCommand(this.CopySelected, () => this.SelectedItem is not null);
    }

    public ObservableCollection<FileResultViewModel> Results { get; set; } = [];

    public IAsyncRelayCommand SelectFilesCommand { get; }

    public IRelayCommand ToggleAlgorithmCommand { get; }

    public IRelayCommand CollapseAllCommand { get; }

    public IRelayCommand ClearCommand { get; }

    public IRelayCommand ToggleShowAsGridCommand { get; }

    public IRelayCommand ToggleShowToolbarCommand { get; }

    public IRelayCommand AboutCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public IRelayCommand CopySelectedCommand { get; }

    public bool ShowMenu { get; } = !RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public string AboutMessage => $"Version {Assembly.GetExecutingAssembly().GetName().Version} by Hugues Valois";

    public void CopySelected()
    {
        if (this.SelectedItem is not null)
        {
            this.SelectedItem.Copy();
        }
    }

    public async Task SelectFilesAsync()
    {
        var filePaths = await this.filePickerService.GetFilePathsAsync(true);
        if (filePaths.Length > 0)
        {
            this.CalculateChecksums(filePaths);
        }
    }

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

    public void CollapseAll()
    {
        foreach (var result in this.Results)
        {
            result.IsExpanded = false;
        }
    }

    public void Clear()
    {
        this.Results.Clear();
        this.CollapseAllCommand.NotifyCanExecuteChanged();
        this.ClearCommand.NotifyCanExecuteChanged();
    }

    public void ToggleShowAsGrid()
    {
        this.ShowAsGrid = !this.ShowAsGrid;
    }

    public void ToggleShowToolbar()
    {
        this.ShowToolbar = !this.ShowToolbar;
    }

    public void About()
    {
        // Dismissing it does not reset it to false,
        // so cycle false/true to ensure it is shown.
        this.ShowAbout = false;
        this.ShowAbout = true;
    }

    public void Cancel()
    {
        if (!this.IsCalculating)
        {
            return;
        }

        this.worker.CancelAsync();
    }

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

        this.NotifyCalculatingChanged();

        this.worker.RunWorkerAsync(new Tuple<string[], string[]>(hashNames, existingFilePaths));
    }

    private void NotifyCalculatingChanged()
    {
        this.SelectFilesCommand.NotifyCanExecuteChanged();
        this.ClearCommand.NotifyCanExecuteChanged();
        this.AboutCommand.NotifyCanExecuteChanged();
        this.CancelCommand.NotifyCanExecuteChanged();
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

        this.NotifyCalculatingChanged();
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
