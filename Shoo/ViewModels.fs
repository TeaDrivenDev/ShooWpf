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

    let asFst second first = first, second
    let asSnd first second = first, second

type CopyOperation =
    {
        Source : string
        Destination : string
        Extension : string
        WebClient : WebClient
    }

type Replacement = { ToReplace : string; ReplaceWith : string }

type FileToMoveViewModel(fileInfo : FileInfo) =

    let progress = new ReactiveProperty<_>(0)

    member __.Name = fileInfo.Name
    member __.FullName = fileInfo.FullName
    member __.Time = fileInfo.LastWriteTime
    member __.Size = fileInfo.Length
    member __.MoveProgress = progress

type MainWindowViewModel() =
    [<Literal>]
    let shooFileNameExtension = ".__shoo__"

    let sourceDirectory = new ReactiveProperty<_>("")
    let destinationDirectory = new ReactiveProperty<_>("")

    let readReplacements replacementsFilePath =
        if File.Exists replacementsFilePath
        then
            File.ReadAllLines replacementsFilePath
            |> Array.map (fun s ->
                let [| toReplace; replaceWith |] = s.Split '|'

                { ToReplace = toReplace; ReplaceWith = replaceWith })
            |> Array.toList
        else []

    let getDestinationFileName filePath extension =
        let directory = Path.GetDirectoryName filePath
        let fileName = Path.GetFileNameWithoutExtension filePath

        let rec getFileName count =
            let name =
                match count with
                | 1 -> fileName + extension
                | _ -> sprintf "%s (%i)%s" fileName count extension
                |> asSnd directory
                |> Path.Combine

            if File.Exists name
            then getFileName (count + 1)
            else name

        getFileName 1

    // File copy with progress: https://stackoverflow.com/a/19755317/236507
    let copy reportProgress onDownloadComplete replacements source destinationDirectory =
        let client = new WebClient()

        client.DownloadProgressChanged
        |> Observable.map (fun (e : DownloadProgressChangedEventArgs) -> e.ProgressPercentage)
        |> Observable.distinctUntilChanged
        |> Observable.subscribe reportProgress
        |> ignore

        client.DownloadFileCompleted
        |> Observable.observeOn RxApp.MainThreadScheduler
        |> Observable.subscribe (fun (e : AsyncCompletedEventArgs) ->
            let copyOperation = e.UserState :?> CopyOperation

            let time = (FileInfo copyOperation.Source).LastWriteTimeUtc

            File.SetLastWriteTimeUtc(copyOperation.Destination, time)
            File.Move(
                copyOperation.Destination,
                getDestinationFileName copyOperation.Destination copyOperation.Extension)

            copyOperation.WebClient.Dispose()

            File.Delete copyOperation.Source

            onDownloadComplete())
        |> ignore

        let destinationFileName =
            (Path.GetFileNameWithoutExtension source, replacements)
            ||> List.fold (fun acc current -> acc.Replace(current.ToReplace, current.ReplaceWith))

        let destination = Path.Combine(destinationDirectory, destinationFileName + shooFileNameExtension)

        client.DownloadFileAsync(
            Uri source,
            destination,
            {
                Source = source
                Destination = destination
                Extension = Path.GetExtension source
                WebClient = client
            })

    let mutable isSourceDirectoryValid = Unchecked.defaultof<ReadOnlyReactiveProperty<_>>
    let mutable isDestinationDirectoryValid = Unchecked.defaultof<ReadOnlyReactiveProperty<_>>

    let fileExtensions = new ReactiveProperty<_>("")

    let replacementsFileName = new ReactiveProperty<_>("")

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

        let replacements =
            replacementsFileName
            |> Observable.map readReplacements
            |> Observable.startWith []
            |> toReadOnlyReactiveProperty

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
                    files.Remove vm |> ignore)
                replacements.Value
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

    member __.ReplacementsFileName = replacementsFileName

    member __.EnableProcessing = enableProcessing

    member __.Files = files