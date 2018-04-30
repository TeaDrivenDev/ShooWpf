
open System
open System.IO
open System.Net


let copy source destinationDirectory =
    let client = new WebClient()
    client.DownloadProgressChanged.Add (fun (e : DownloadProgressChangedEventArgs) ->
        if e.ProgressPercentage % 10 = 0
        then
            printfn "%i %%" e.ProgressPercentage)
    client.DownloadDataCompleted.Add (fun (e : DownloadDataCompletedEventArgs) ->
        printfn "\nDone")

    let destination = Path.Combine(destinationDirectory, Path.GetFileName source)

    client.DownloadFileAsync(Uri source, destination)


let source = @"E:\Trine_Setup_109.zip"
let destinationDirectory = @"D:\"

copy source destinationDirectory
