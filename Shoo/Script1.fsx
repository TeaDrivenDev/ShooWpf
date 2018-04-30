
open System
open System.IO
open System.Net
open System.ComponentModel

type Download = { Source : string; Destination : string }

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
