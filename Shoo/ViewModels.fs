namespace Shoo.ViewModels

open System
open System.IO

open FSharp.Control.Reactive

open Reactive.Bindings

[<AutoOpen>]
module Utility =
    let toReadOnlyReactiveProperty (observable : IObservable<_>) =
        observable.ToReadOnlyReactiveProperty()

    
type MainWindowViewModel() =
    let sourceDirectory = new ReactiveProperty<_>("")
    let destinationDirectory = new ReactiveProperty<_>("")

    let mutable isSourceDirectoryValid = Unchecked.defaultof<ReadOnlyReactiveProperty<_>>
    let mutable isDestinationDirectoryValid = Unchecked.defaultof<ReadOnlyReactiveProperty<_>>

    do
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
    
    member __.SourceDirectory = sourceDirectory
    member __.DestinationDirectory = destinationDirectory
    member __.IsSourceDirectoryValid = isSourceDirectoryValid
    member __.IsDestinationDirectoryValid = isDestinationDirectoryValid
