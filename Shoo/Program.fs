namespace Shoo

open System
open System.IO
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

        if not <| String.IsNullOrWhiteSpace previous && Directory.Exists previous
        then
            folderBrowserDialog.SelectedPath <- previous

        let win = Win32Window(System.Windows.Interop.WindowInteropHelper(this).Handle)
        let result = folderBrowserDialog.ShowDialog win

        match result with
        | Forms.DialogResult.OK -> Some folderBrowserDialog.SelectedPath
        | _ -> None

    let selectFile previous =
        let openFileDialog = Microsoft.Win32.OpenFileDialog()
        openFileDialog.Multiselect <- false
        openFileDialog.CheckFileExists <- true

        if not <| String.IsNullOrWhiteSpace previous
        then
            let dir = try Path.GetDirectoryName previous with _ -> ""

            if Directory.Exists dir
            then openFileDialog.InitialDirectory <- dir

        let result =
            openFileDialog.ShowDialog this
            |> Option.ofNullable
            |> Option.bind (function
                | true -> Some openFileDialog.FileName
                | false -> None)

        result

    member __.ViewModel : MainWindowViewModel = __.DataContext :?> MainWindowViewModel

    override __.SelectSourceDirectory_Click (_ : obj, e : RoutedEventArgs) =
        selectFolder __.ViewModel.SourceDirectory.Value
        |> Option.iter (fun directory -> __.ViewModel.SourceDirectory.Value <- directory)

    override __.SelectDestinationDirectory_Click (_ : obj, e : RoutedEventArgs) =
        selectFolder __.ViewModel.DestinationDirectory.Value
        |> Option.iter (fun directory -> __.ViewModel.DestinationDirectory.Value <- directory)

    override __.SelectReplacementsFile_Click (_ : obj, e : RoutedEventArgs) =
        selectFile __.ViewModel.ReplacementsFileName.Value
        |> Option.iter (fun fileName -> __.ViewModel.ReplacementsFileName.Value <- fileName)

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