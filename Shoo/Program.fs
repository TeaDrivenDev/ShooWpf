namespace Shoo

open System
open System.Windows

open FsXaml

open Shoo.ViewModels

type Win32Window(handle : IntPtr) =
    interface System.Windows.Forms.IWin32Window with
        member __.Handle : IntPtr = handle

type MainWindowBase = XAML<"MainWindow.xaml">

type MainWindow() as this =
    inherit MainWindowBase()

    let selectFolder previous =
        use folderBrowserDialog = new Forms.FolderBrowserDialog()
        folderBrowserDialog.ShowNewFolderButton <- false

        if not <| String.IsNullOrWhiteSpace previous && System.IO.Directory.Exists previous
        then
            folderBrowserDialog.SelectedPath <- previous

        let win = Win32Window(System.Windows.Interop.WindowInteropHelper(this).Handle)
        let result = folderBrowserDialog.ShowDialog win

        if result = Forms.DialogResult.OK
        then Some folderBrowserDialog.SelectedPath
        else None

    member __.ViewModel : MainWindowViewModel = __.DataContext :?> MainWindowViewModel

    override __.SelectSourceDirectory_Click (_ : obj, e : RoutedEventArgs) =
        selectFolder __.ViewModel.SourceDirectory.Value
        |> Option.iter (fun directory -> __.ViewModel.SourceDirectory.Value <- directory)

    override __.SelectDestinationDirectory_Click (_ : obj, e : RoutedEventArgs) =
        selectFolder __.ViewModel.DestinationDirectory.Value
        |> Option.iter (fun directory -> __.ViewModel.DestinationDirectory.Value <- directory)

type AppBase = XAML<"App.xaml">

type App() =
    inherit AppBase()

    member __.Application_Startup (_ : obj, _ : StartupEventArgs) =
        MainWindow().Show()

module Program =
    open System

    [<STAThread>]
    [<EntryPoint>]
    let main argv =
        App().Run()
