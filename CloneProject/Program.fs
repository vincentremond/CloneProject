open System
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open System.Threading
open System.Windows.Forms
open CloneProject
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

let clone gitUrl targetDirectory =
    use proc = new ProcessHost("git.exe", targetDirectory)

    proc.Start($"clone {gitUrl}")
    proc.WaitForExit(TimeSpan.MaxValue) |> ignore
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

let showConsole =
    ConsoleConfiguration.AllocConsole() |> ignore

    let defaultStdout = nativeint 7

    match ConsoleConfiguration.GetStdHandle(ConsoleConfiguration.StdOutputHandle) = defaultStdout with
    | false -> ConsoleConfiguration.SetStdHandle(ConsoleConfiguration.StdOutputHandle, defaultStdout)
    | _ -> ()

let config =
    lazy
        (let path = "Targets.json"
         use file = File.OpenRead(path)
         JsonSerializer.Deserialize<Map<string, string>>(file))

let tryParseUri (value: string) : Uri option =
    let mutable uri = null

    match Uri.TryCreate(value, UriKind.Absolute, &uri) with
    | true -> Some uri
    | false -> None

let (<!>) (a: 'a option) (b: unit -> 'a option) =
    match a with
    | Some a -> Some a
    | None -> b ()

[<RequireQualifiedAccess>]
module Uri =
    let getHost (uri: Uri) = uri.Host

[<RequireQualifiedAccess>]
module Map =
    let tryFind' m k = Map.tryFind k m

let deductTargetDirectory (cliParam: string option) (url: string) : string option =

    let tryFindByDomain url _ =
        url
        |> tryParseUri
        |> Option.map Uri.getHost
        |> Option.bind (config.Value |> Map.tryFind')

    let defaultValue () =
        "Default" |> (config.Value |> Map.tryFind')

    cliParam <!> (tryFindByDomain url) <!> defaultValue

[<EntryPoint>]
[<STAThread>]
let main argv =
    showConsole
    printfn $"%b{ConsoleConfiguration.AllocConsole()}"

    let gitUrl = Clipboard.GetText()

    let targetDirectory =
        deductTargetDirectory (argv |> Array.tryHead) gitUrl
        |> Option.defaultWith (fun () -> failwith "No target directory found")



    printfn $"Cloning %s{gitUrl} into %s{targetDirectory}"

    let outputDirectory = (clone gitUrl targetDirectory) |> readOutputDirectory

    let path = Path.Combine(targetDirectory, outputDirectory)

    explorer path

    fork path

    printfn $"Project cloned into %s{path}"
    10. |> TimeSpan.FromSeconds |> Thread.Sleep

    0
