namespace Shoo.ViewModels

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.IO
open System.Net
open System.Reactive.Concurrency
open System.Reactive.Subjects
open System.Windows

open FSharp.Control.Reactive

open Reactive.Bindings
open Reactive.Bindings.Notifiers
open ReactiveUI

[<AutoOpen>]
module Utility =
    let toReadOnlyReactiveProperty (observable : IObservable<_>) =
        observable.ToReadOnlyReactiveProperty()

    let trim (s : string) = s.Trim()

type CopyOperation =
    {
        Source : string
        Destination : string
        WebClient : WebClient
    }

type FileToMoveViewModel(fileInfo : FileInfo) =

    let progress = new ReactiveProperty<_>(0)

    member __.Name = fileInfo.Name
    member __.FullName = fileInfo.FullName
    member __.Time = fileInfo.LastWriteTime
    member __.Size = fileInfo.Length
    member __.MoveProgress = progress

type MainWindowViewModel() =
    let sourceDirectory = new ReactiveProperty<_>("")
    let destinationDirectory = new ReactiveProperty<_>("")

    // see https://stackoverflow.com/a/19755317/236507
    let copy reportProgress onDownloadComplete source destinationDirectory =
        let client = new WebClient()
        client.DownloadProgressChanged
        |> Observable.map (fun (e : DownloadProgressChangedEventArgs) -> e.ProgressPercentage)
        |> Observable.distinctUntilChanged
        |> Observable.subscribe reportProgress
        |> ignore

        client.DownloadFileCompleted
        |> Observable.observeOn RxApp.MainThreadScheduler
        |> Observable.subscribe (fun (e : AsyncCompletedEventArgs) ->
            let download = e.UserState :?> CopyOperation

            let time = (FileInfo download.Source).LastWriteTimeUtc

            File.SetLastWriteTimeUtc(download.Destination, time)
            
            download.WebClient.Dispose()
            
            onDownloadComplete())
        |> ignore

        let destination = Path.Combine(destinationDirectory, Path.GetFileName source)

        client.DownloadFileAsync(Uri source,
                                 destination,
                                 { Source = source; Destination = destination; WebClient = client })

    let mutable isSourceDirectoryValid = Unchecked.defaultof<ReadOnlyReactiveProperty<_>>
    let mutable isDestinationDirectoryValid = Unchecked.defaultof<ReadOnlyReactiveProperty<_>>

    let fileExtensions = new ReactiveProperty<_>("")

    let enableProcessing = new ReactiveProperty<_>(false)

    let files = ObservableCollection<_>()
    
    let canMoveFileSwitch = new BooleanNotifier(true)
    let canMoveFile = canMoveFileSwitch |> Observable.startWith [ true ]

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

        let fileToMoveViewModelsObservable =
            watcher.Renamed
            |> Observable.map (fun e -> FileInfo e.FullPath)
            |> Observable.filter (fun fileInfo ->
                String.IsNullOrWhiteSpace fileExtensions.Value
                || fileExtensions.Value.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
                   |> Array.contains (fileInfo.Extension.Substring 1))
            |> Observable.map FileToMoveViewModel

        let fileToMoveViewModels = new ReplaySubject<_>()
        
        fileToMoveViewModelsObservable
        |> Observable.subscribeObserver fileToMoveViewModels
        |> ignore

        [
            canMoveFile |> Observable.map id
            isDestinationDirectoryValid |> Observable.map id
            enableProcessing |> Observable.map id
        ]
        |> Observable.combineLatestSeq
        |> Observable.map (Seq.toList >> List.forall id)
        |> Observable.filter id
        |> Observable.zip fileToMoveViewModels
        |> Observable.map fst
        |> Observable.subscribe (fun vm -> 
            canMoveFileSwitch.TurnOff()

            copy
                (fun progress -> vm.MoveProgress.Value <- progress)
                (fun () ->
                    canMoveFileSwitch.TurnOn()
                    files.Remove vm |> ignore
                    
                    printfn "Grr - %s" vm.Name)
                vm.FullName
                destinationDirectory.Value)
        |> ignore

        fileToMoveViewModels
        |> Observable.observeOn RxApp.MainThreadScheduler
        |> Observable.subscribe files.Add
        |> ignore

    member __.SourceDirectory = sourceDirectory
    member __.DestinationDirectory = destinationDirectory
    member __.IsSourceDirectoryValid = isSourceDirectoryValid
    member __.IsDestinationDirectoryValid = isDestinationDirectoryValid

    member __.FileExtensions = fileExtensions

    member __.EnableProcessing = enableProcessing

    member __.Files = files
