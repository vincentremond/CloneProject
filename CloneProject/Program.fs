open System
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open System.Threading
open System.Windows.Forms
open RunProcess
open FSharpPlus

let write name contents = File.WriteAllText(name, contents)

module Process =
    let start name directory arguments =
        Process
            .Start(ProcessStartInfo(name, arguments, WorkingDirectory = directory))
            .WaitForExit()

let explorer (path: string) = Process.start "explorer.exe" path path

let fork (path: string) = Process.start "fork.exe" path path

let rider (path: string) =
    Process.start "powershell.exe" path "-Command rider.ps1"

let riderFixConfig (path: string) =
    Process.start "RiderFixConfig.exe" path ""

let clone gitUrl targetDirectory =
    use proc = new ProcessHost("git.exe", targetDirectory)

    proc.Start($"clone {gitUrl}")

    proc.WaitForExit(TimeSpan.MaxValue)
    |> ignore

    let stdErr = proc.StdErr.ReadAllText(Encoding.UTF8)
    let stdOut = proc.StdOut.ReadAllText(Encoding.UTF8)
    printfn $"%s{stdErr}"
    printfn $"%s{stdOut}"
    stdErr

let readOutputDirectory output =
    Regex
        .Match(output, "\'(?<OutputDirectory>.+?)\'")
        .Groups["OutputDirectory"]
        .Value

let rec tryLocateFile name directory =
    Directory.GetFiles(directory, name)
    |> Seq.tryExactlyOne
    |> Option.orElseWith (fun () ->
        directory
        |> Path.GetDirectoryName
        |> Option.ofObj
        |> Option.bind (tryLocateFile name)
    )

let config =
    lazy
        (let path = (tryLocateFile "Targets.local.json" Environment.CurrentDirectory) |> Option.defaultValue "Targets.json"
         use file = File.OpenRead(path)
         JsonSerializer.Deserialize<Map<string, string>>(file))

let (<!>) (a: 'a option) (b: unit -> 'a option) =
    match a with
    | Some a -> Some a
    | None -> b ()

[<RequireQualifiedAccess>]
module List =
    let tryRemove (value: 'a) (list: 'a list) : 'a list * bool =
        List.foldBack
            (fun item (acc, found) -> if item = value then (acc, true) else (item :: acc, found))
            list
            ([], false)

[<RequireQualifiedAccess>]
module Uri =
    let getHost (uri: Uri) = uri.Host

[<RequireQualifiedAccess>]
module Regex =
    let replace (pattern: string) (replacement: string) input =
        Regex.Replace(input, pattern, replacement)

[<RequireQualifiedAccess>]
module Map =
    let tryFind' m k = Map.tryFind k m

let deductTargetDirectory (cliParam: string option) (url: Uri) : string option =

    let tryFindByDomain _ =
        let host = url |> Uri.getHost
        Map.tryFind host config.Value

    let defaultValue () =
        "Default"
        |> (config.Value |> Map.tryFind')

    cliParam
    <!> tryFindByDomain
    <!> defaultValue

let runAsSTAThread<'a> (f: unit -> 'a): 'a =
    let mutable result = Unchecked.defaultof<'a>
    let autoResetEvent = new AutoResetEvent(false)
    let thread = Thread(ThreadStart(fun () ->
        result <- f()
        autoResetEvent.Set() |> ignore
    ))
    thread.SetApartmentState(ApartmentState.STA)
    thread.Start()
    autoResetEvent.WaitOne() |> ignore
    
    result

[<EntryPoint>]
let main argv =

    let argv, mergeRequest =
        argv
        |> Seq.toList
        |> List.tryRemove "--merge-request"

    let gitUrl = runAsSTAThread Clipboard.GetText
    let gitUrl =
        match Uri.TryCreate(gitUrl, UriKind.Absolute) with
        | false, gitUrl -> failwith $"No valid URL found in clipboard %A{gitUrl}"
        | true, gitUrl ->
            printfn $"Cloning project from %s{gitUrl.ToString()}"
            gitUrl

    let gitUrl =
        if mergeRequest then
            gitUrl.ToString()
            |> Regex.replace "/-/merge_requests/.*$" ".git"
            |> Uri
        else
            gitUrl

    let targetDirectory =
        deductTargetDirectory (argv |> List.tryHead) gitUrl
        |> Option.defaultWith (fun () -> failwith "No target directory found")

    printfn $"Cloning %s{string gitUrl} into %s{targetDirectory}"

    let outputDirectory =
        (clone gitUrl targetDirectory)
        |> readOutputDirectory

    let path = Path.Combine(targetDirectory, outputDirectory)
    
    

    if mergeRequest then
        rider path
    else

        explorer path
        fork path

    printfn $"Project cloned into %s{path}"

    10.
    |> TimeSpan.FromSeconds
    |> Thread.Sleep

    0
