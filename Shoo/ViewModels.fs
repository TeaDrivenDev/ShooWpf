namespace Shoo.ViewModels

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.IO
open System.Net
open System.Reactive.Concurrency
open System.Windows

open FSharp.Control.Reactive

open Reactive.Bindings
open Reactive.Bindings.Notifiers
open ReactiveUI

[<AutoOpen>]
module Utility =
    let toReadOnlyReactiveProperty (observable : IObservable<_>) =
        observable.ToReadOnlyReactiveProperty()

type Download = { Source : string; Destination : string }

type FileToMoveViewModel(fileInfo : FileInfo) =

    let progress = new ReactiveProperty<_>(0)

    member __.Name = fileInfo.Name
    member __.Time = fileInfo.LastWriteTime
    member __.Size = fileInfo.Length
    member __.MoveProgress = progress

type MainWindowViewModel() =
    let sourceDirectory = new ReactiveProperty<_>("")
    let destinationDirectory = new ReactiveProperty<_>("")

    // see https://stackoverflow.com/a/19755317/236507
    let copy source destinationDirectory =
        let client = new WebClient()
        client.DownloadProgressChanged.Add (fun (e : DownloadProgressChangedEventArgs) ->
            if e.ProgressPercentage % 10 = 0
            then
                printfn "%i %%" e.ProgressPercentage)
        client.DownloadFileCompleted.Add (fun (e : AsyncCompletedEventArgs) ->
            let download = e.UserState :?> Download

            let time = (FileInfo download.Source).LastWriteTimeUtc

            File.SetLastWriteTimeUtc(download.Destination, time))

        let destination = Path.Combine(destinationDirectory, Path.GetFileName source)

        client.DownloadFileAsync(Uri source, destination, { Source = source; Destination = destination })

    let mutable isSourceDirectoryValid = Unchecked.defaultof<ReadOnlyReactiveProperty<_>>
    let mutable isDestinationDirectoryValid = Unchecked.defaultof<ReadOnlyReactiveProperty<_>>

    let files = ObservableCollection<_>()
    
    let canMoveFile = new BooleanNotifier(true)

    let watcher = new FileSystemWatcher()

    do
        RxApp.MainThreadScheduler <- DispatcherScheduler(Application.Current.Dispatcher)

        let directories = Observable.combineLatest sourceDirectory destinationDirectory

        isSourceDirectoryValid <-
            directories
            |> Observable.map (fun (source, destination) ->
                Directory.Exists source && source <> destination)
            |> toReadOnlyReactiveProperty

        isDestinationDirectoryValid <-
            directories
            |> Observable.map (fun (source, destination) ->
                Directory.Exists destination && source <> destination)
            |> toReadOnlyReactiveProperty

        isSourceDirectoryValid
        |> Observable.distinctUntilChanged
        |> Observable.subscribe(fun isValid ->
            watcher.EnableRaisingEvents <- false

            if isValid
            then
                watcher.Path <- sourceDirectory.Value
                watcher.EnableRaisingEvents <- true)
        |> ignore

        let fileToMoveViewModels =
            watcher.Renamed
            |> Observable.map (fun e -> FileInfo e.FullPath |> FileToMoveViewModel)

        fileToMoveViewModels
        |> Observable.observeOn RxApp.MainThreadScheduler
        |> Observable.subscribe files.Add
        |> ignore

        

    member __.SourceDirectory = sourceDirectory
    member __.DestinationDirectory = destinationDirectory
    member __.IsSourceDirectoryValid = isSourceDirectoryValid
    member __.IsDestinationDirectoryValid = isDestinationDirectoryValid

    member __.Files = files
