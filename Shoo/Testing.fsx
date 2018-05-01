open System
open System.IO

let copySource = @"E:\Staging\Shoo\Source"
let watching = @"E:\Staging\Shoo\Watching"

//copySource
//|> Directory.GetFiles
//|> Seq.iter (fun name -> File.Move(name, name + "_"))

watching
|> Directory.GetFiles
|> Seq.iter File.Delete

copySource
|> Directory.GetFiles
|> Seq.truncate 4
|> Seq.map (fun filePath ->
    let fileName = Path.GetFileName filePath
    let newFilePath = Path.Combine(watching, fileName)
    
    filePath, newFilePath)
|> Seq.iter (fun (old, newP) ->
    File.Copy(old, newP)
    File.Move(newP, newP.Substring(0, newP.Length - 1)))
