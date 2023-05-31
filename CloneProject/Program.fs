open System
open System.Diagnostics
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Threading
open System.Windows.Forms
open CloneProject
open RunProcess

let write name contents = File.WriteAllText(name, contents)

let explorer (path: string) =
    Process.Start("explorer", path).WaitForExit()

let clone gitUrl targetDirectory =
    use proc =
        new ProcessHost("git.exe", targetDirectory)

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

[<EntryPoint>]
[<STAThread>]
let main argv =
    showConsole
    printfn $"%b{ConsoleConfiguration.AllocConsole()}"

    let targetDirectory =
        Seq.tryHead argv |> Option.defaultValue @"D:\GIT\"

    let gitUrl = Clipboard.GetText()

    printfn $"Cloning %s{gitUrl} into %s{targetDirectory}"

    let outputDirectory =
        (clone gitUrl targetDirectory)
        |> readOutputDirectory

    let path =
        Path.Combine(targetDirectory, outputDirectory)

    explorer path

    printfn $"Project cloned into %s{path}"
    Thread.Sleep(TimeSpan.FromSeconds(10.))

    0
