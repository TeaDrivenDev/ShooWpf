namespace Shoo.ViewModels

open System
open System.Collections.ObjectModel
open System.IO
open System.Reactive.Concurrency
open System.Windows

open FSharp.Control.Reactive

open Reactive.Bindings
open ReactiveUI

[<AutoOpen>]
module Utility =
    let toReadOnlyReactiveProperty (observable : IObservable<_>) =
        observable.ToReadOnlyReactiveProperty()

type FileToMoveViewModel(fileInfo : FileInfo) =

    member __.Name = fileInfo.Name
    member __.Time = fileInfo.LastWriteTime
    member __.Size = fileInfo.Length

type MainWindowViewModel() =
    let sourceDirectory = new ReactiveProperty<_>("")
    let destinationDirectory = new ReactiveProperty<_>("")

    let mutable isSourceDirectoryValid = Unchecked.defaultof<ReadOnlyReactiveProperty<_>>
    let mutable isDestinationDirectoryValid = Unchecked.defaultof<ReadOnlyReactiveProperty<_>>

    let files = ObservableCollection<_>()

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

        watcher.Renamed
        |> Observable.observeOn RxApp.MainThreadScheduler
        |> Observable.subscribe (fun e ->
            e.FullPath
            |> FileInfo
            |> files.Add)
        |> ignore

        isSourceDirectoryValid
        |> Observable.distinctUntilChanged
        |> Observable.subscribe(fun isValid ->
            watcher.EnableRaisingEvents <- false

            if isValid
            then
                watcher.Path <- sourceDirectory.Value
                watcher.EnableRaisingEvents <- true)
        |> ignore

    member __.SourceDirectory = sourceDirectory
    member __.DestinationDirectory = destinationDirectory
    member __.IsSourceDirectoryValid = isSourceDirectoryValid
    member __.IsDestinationDirectoryValid = isDestinationDirectoryValid

    member __.Files = files
