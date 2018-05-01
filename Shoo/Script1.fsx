
#I @"..\packages"
#r @"System.Reactive.Core\lib\net46\System.Reactive.Core.dll"
#r @"System.Reactive.Interfaces\lib\net45\System.Reactive.Interfaces.dll"
#r @"System.Reactive.Linq\lib\net45\System.Reactive.Linq.dll"
#r @"System.Reactive.PlatformServices\lib\net46\System.Reactive.PlatformServices.dll"
#r @"System.Reactive.Windows.Threading\lib\net45\System.Reactive.Windows.Threading.dll"
#r @"FSharp.Control.Reactive\lib\net45\FSharp.Control.Reactive.dll"

open System
open System.IO
open System.Net
open System.ComponentModel

// ----------------

type Download = { Source : string; Destination : string }

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


let source = @"E:\Trine_Setup_109.zip"
let destinationDirectory = @"D:\"

copy source destinationDirectory

// ---------------


open FSharp.Control.Reactive

let watching = @"D:\Development\Staging\Shoo\Watching"


let watcher = new FileSystemWatcher(watching)
watcher.Filter <- "*.fs"
watcher.Renamed
|> Observable.subscribe(fun e ->
    printfn "%s -> %s" e.OldName e.Name)

watcher.EnableRaisingEvents <- true
