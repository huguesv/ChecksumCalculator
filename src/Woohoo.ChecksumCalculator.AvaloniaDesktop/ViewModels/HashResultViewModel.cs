// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class HashResultViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Algorithm { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;
}
