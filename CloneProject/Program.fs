namespace CloneProject

open System
open System.IO
open System.Text.RegularExpressions
open System.Threading
open CloneProject
open TextCopy

module Program =

    let readOutputDirectory output =
        Regex.Match(output, "\'(?<OutputDirectory>.+?)\'").Groups["OutputDirectory"].Value

    let private getConfiguration () =

        let configurationFile =
            ConfigurationLocator.tryLocateFile Environment.CurrentDirectory [
                "Targets.local.json"
                "Targets.json"
            ]
            |> Option.defaultWith (fun () -> failwith "No configuration file found (Targets.local.json or Targets.json)")

        let configuration = ConfigurationReader.readFromFile configurationFile
        configuration

    [<EntryPoint>]
    let main args =
        try
            let gitUrl =
                match args with
                | [| arg |] -> arg
                | [||] -> ClipboardService.GetText()
                | _ -> failwith "Only one argument (git URL) is expected"
                |> GitUrlFixer.fix

            let gitUrl =
                match Uri.TryCreate(gitUrl, UriKind.Absolute) with
                | false, gitUrl -> failwith $"Not a valid URL: %A{gitUrl}"
                | true, gitUrl -> gitUrl

            let configuration = getConfiguration ()
            let targetDirectory = TargetDirectoryService.find configuration gitUrl

            printfn $"Cloning %s{string gitUrl} into %s{targetDirectory}"

            let outputDirectory =
                (Process.gitClone gitUrl targetDirectory) |> readOutputDirectory

            let path = Path.Combine(targetDirectory, outputDirectory)

            Process.riderFixConfig path
            Process.explorer path
            Process.fork path
            Process.rider path
            Process.wt path

            printfn $"Project cloned into %s{path}"

            10. |> TimeSpan.FromSeconds |> Thread.Sleep

            0

        with ex ->
            printfn $"Error: %s{ex.Message}"
            printfn "Press Enter to exit..."
            Console.ReadLine() |> ignore
            1
