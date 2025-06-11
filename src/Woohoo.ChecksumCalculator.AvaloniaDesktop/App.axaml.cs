// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.AvaloniaDesktop;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Woohoo.ChecksumCalculator.AvaloniaDesktop.Services;
using Woohoo.ChecksumCalculator.AvaloniaDesktop.ViewModels;
using Woohoo.ChecksumCalculator.AvaloniaDesktop.Views;

public partial class App : Application
{
    private const string DataGridStyle = "avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml";

    public static TopLevel? GetTopLevel(Application app)
    {
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        if (app.ApplicationLifetime is ISingleViewApplicationLifetime viewApp)
        {
            var visualRoot = viewApp.MainView?.GetVisualRoot();
            return visualRoot as TopLevel;
        }

        return null;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        if (this.Styles.Count == 1 && this.Styles[0] is FluentTheme)
        {
            this.Styles.Add(new StyleInclude(new System.Uri(DataGridStyle))
            {
                Source = new System.Uri(DataGridStyle),
            });
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // If you use CommunityToolkit, line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // Register all the services needed for the application to run
        var collection = new ServiceCollection();
        AddServices(collection);

        // Creates a ServiceProvider containing services from the provided IServiceCollection
        var services = collection.BuildServiceProvider();

        var vm = services.GetRequiredService<MainWindowViewModel>();

        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void AddServices(ServiceCollection collection)
    {
        collection.AddTransient<MainWindowViewModel>();
        collection.AddTransient<IClipboardService, ClipboardService>();
        collection.AddTransient<IFileExplorerService, FileExplorerService>();
        collection.AddTransient<IFilePickerService, FilePickerService>();
        collection.AddTransient<ITaskbarService, TaskbarService>();
    }
}
