namespace CloneProject

open System.IO

[<RequireQualifiedAccess>]
module ConfigurationLocator =

    let tryLocateFile directory files =
        files
        |> List.tryPick (fun name ->
            let fileInfo = Path.Combine(directory, name) |> FileInfo
            if fileInfo.Exists then Some fileInfo else None
        )

