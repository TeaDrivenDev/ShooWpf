namespace Shoo

open System.Windows

open FsXaml

type MainWindowBase = XAML<"MainWindow.xaml">

type MainWindow() as this =
    inherit MainWindowBase()


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
