using System.Text;
using Ookii.Dialogs.Wpf;
using ReactiveUI;
using Reloaded.Mod.Launcher.Controls.Dialogs;
using Reloaded.Mod.Launcher.Lib.Remix.Interactions;
using Reloaded.Mod.Launcher.Lib.Remix.ViewModels;
using Reloaded.Mod.Loader.Update.Providers.Web;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Extractors.SevenZipSharp;
using static Reloaded.Mod.Launcher.Lib.Static.Resources;

namespace Reloaded.Mod.Launcher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : ReloadedWindow
{
    private string[] SupportedDropFormats = [".7z", ".zip", ".rar"];
    
    public Lib.Models.ViewModel.WindowViewModel RealViewModel { get; set; }

    public MainWindow()
    {
        // Make viewmodel of this window available.
        RealViewModel = Lib.IoC.GetConstant<Lib.Models.ViewModel.WindowViewModel>();

        // Initialize XAML.
        InitializeComponent();

        // Bind other models.
        Lib.IoC.BindToConstant((WPF.Theme.Default.WindowViewModel)DataContext);// Controls window properties.
        Lib.IoC.BindToConstant(this);

        // Easily allows DragDrop app wide.
        this.MouseEnter += MainWindow_MouseEnter;
        this.MouseLeave += MainWindow_MouseLeave;

#if DEBUG
        this.Border_DragDropCapturer.Visibility = Visibility.Collapsed;
#endif

        // Interactions.
        CommonInteractions.PromptTextInput.RegisterHandler(HandleTextInput);
        CommonInteractions.SelectFile.RegisterHandler(HandleSelectFile);
        CommonInteractions.SelectFolder.RegisterHandler(HandleSelectFolder);
        CommonInteractions.SaveFile.RegisterHandler(HandleSaveFile);
    }

    private void HandleSaveFile(IInteractionContext<SaveFileConfig, string?> context)
    {
        var config = context.Input;
        var saveFile = new VistaSaveFileDialog()
        {
            Title = config.Title,
            OverwritePrompt = config.OverwritePrompt,
            Filter = config.Filter,
            FileName = config.FileName,
        };

        if (saveFile.ShowDialog() == true)
        {
            context.SetOutput(saveFile.FileName);
        }
        else
        {
            context.SetOutput(null);
        }
    }

    private void HandleSelectFolder(IInteractionContext<SelectFolderConfig, string[]?> context)
    {
        var config = context.Input;
        var openFolder = new VistaFolderBrowserDialog()
        {
            Description = config.Title,
            Multiselect = config.AllowMultiple,
            UseDescriptionForTitle = true,
        };

        if (openFolder.ShowDialog() == true)
        {
            context.SetOutput(openFolder.SelectedPaths);
        }
        else
        {
            context.SetOutput(null);
        }
    }

    private void HandleSelectFile(IInteractionContext<SelectFileConfig, string[]?> context)
    {
        var config = context.Input;
        using var openFile = new System.Windows.Forms.OpenFileDialog()
        {
            Title = config.Title,
            Filter = config.Filter,
            Multiselect = config.AllowMultiple,
        };

        if (openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            context.SetOutput(openFile.FileNames);
        }
        else
        {
            context.SetOutput(null);
        }
    }

    private void HandleTextInput(IInteractionContext<TextInputViewModel, string?> context)
    {
        var textInputDialog = new TextInputDialog() { ViewModel = context.Input };
        textInputDialog.Owner = this;

        if (textInputDialog.ShowDialog() == true && !string.IsNullOrEmpty(textInputDialog.ViewModel.Text))
        {
            context.SetOutput(textInputDialog.ViewModel.Text);
        }
        else
        {
            context.SetOutput(null);
        }
    }

    private void MainWindow_MouseEnter(object sender, MouseEventArgs e)
    {
        //Trace.WriteLine(nameof(MainWindow_MouseEnter));
        this.Border_DragDropCapturer.IsHitTestVisible = false;
    }

    private void MainWindow_MouseLeave(object sender, MouseEventArgs e)
    {
        //Trace.WriteLine(nameof(MainWindow_MouseLeave));
        this.Border_DragDropCapturer.IsHitTestVisible = true;
    }

    private void InstallMod_DragOver(object sender, DragEventArgs e)
    {
        //Trace.WriteLine(nameof(InstallMod_DragOver));
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            
            // Check if the file is a .zip file
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file);
                if (!SupportedDropFormats.Contains(extension)) 
                    continue;

                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
                return;
            }
        }
        else
        {
            this.Border_DragDropCapturer.IsHitTestVisible = false;
        }

        e.Handled = true;
    }

    private async void InstallMod_Drop(object sender, DragEventArgs e)
    {
        //Trace.WriteLine(nameof(InstallMod_Drop));
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) 
            return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        var config = Lib.IoC.GetConstant<LoaderConfig>();
        var modsFolder = config.GetModConfigDirectory();

        // Get list of installed mods before
        var modConfigService = Lib.IoC.GetConstant<ModConfigService>();
        var modsBefore = new Dictionary<string, PathTuple<ModConfig>>(modConfigService.ItemsById);

        // Install mods.
        foreach (var file in files)
        {
            var extension = Path.GetExtension(file);
            if (!SupportedDropFormats.Contains(extension)) 
                continue;

            /* Extract to Temp Directory */
            using var tempFolder = new TemporaryFolderAllocation();
            var archiveExtractor = new SevenZipSharpExtractor();
            await archiveExtractor.ExtractPackageAsync(file, tempFolder.FolderPath, new Progress<double>(), default);

            /* Get name of package. */
            WebDownloadablePackage.CopyPackagesFromExtractFolderToTargetDir(modsFolder!, tempFolder.FolderPath, default);
        }
        
        // Find the new mods
        modConfigService.ForceRefresh();
        var newConfigs = new List<ModConfig>();
        foreach (var item in modConfigService.ItemsById.ToArray())
        {
            if (!modsBefore.ContainsKey(item.Key))
                newConfigs.Add(item.Value.Config);
        }

        if (newConfigs.Count <= 0)
            return;
        
        // Print loaded mods.
        var installedMods = new StringBuilder();
        foreach (var conf in newConfigs)
            installedMods.AppendLine($"{conf.ModName} ({conf.ModId})");
        
        var loadedMods = string.Format(DragDropInstalledModsDescription.Get(), newConfigs.Count);
        Actions.DisplayMessagebox?.Invoke(DragDropInstalledModsTitle.Get(), 
            $"{loadedMods}\n\n{installedMods}",
            new Actions.DisplayMessageBoxParams() { StartupLocation = Actions.WindowStartupLocation.CenterScreen });
    }
}