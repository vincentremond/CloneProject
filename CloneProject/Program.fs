open System
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open System.Threading
open Pinicola.FSharp
open RunProcess
open TextCopy

let write path (contents: string) = File.WriteAllText(path, contents)

module Process =
    let start exeShortName directory (arguments: string list) =
        let exe = Where.findInPath exeShortName
        Process.Start(ProcessStartInfo(exe.FullName, arguments, WorkingDirectory = directory)).WaitForExit()

let explorer (path: string) = Process.start "explorer" path [ path ]

let fork (path: string) = Process.start "fork" path []

let rider (path: string) = Process.start "rider" path [ path ]

let wt (path: string) = Process.start "wt" path []

let riderFixConfig (path: string) =
    Process.start "RiderFixConfig" path [ path ]

let clone gitUrl targetDirectory =
    use proc = new ProcessHost("git", targetDirectory)

    proc.Start($"clone {gitUrl}")

    proc.WaitForExit(TimeSpan.MaxValue) |> ignore

    let stdErr = proc.StdErr.ReadAllText(Encoding.UTF8)
    let stdOut = proc.StdOut.ReadAllText(Encoding.UTF8)
    printfn $"%s{stdErr}"
    printfn $"%s{stdOut}"
    stdErr

let readOutputDirectory output =
    Regex.Match(output, "\'(?<OutputDirectory>.+?)\'").Groups["OutputDirectory"].Value

let rec tryLocateFile name directory =
    Directory.GetFiles(directory, name)
    |> Seq.tryExactlyOne
    |> Option.orElseWith (fun () ->
        directory
        |> Path.GetDirectoryName
        |> Option.ofObj
        |> Option.bind (tryLocateFile name)
    )

type Configuration = {
    Default: string
    Targets: ConfigurationTarget list
}

and ConfigurationTarget = {
    UrlPattern: UrlPattern
    TargetDirectory: string
}

and [<RequireQualifiedAccess>] UrlPattern =
    | StartsWith of string
    | Regex of Regex

let readConfiguration () =

    let fixes = [ "{{UserProfile}}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ]

    let applyFixes (s: string) =
        fixes
        |> List.fold (fun acc (oldValue, replacement) -> acc |> String.replace oldValue replacement) s

    let path =
        (tryLocateFile "Targets.local.json" Environment.CurrentDirectory)
        |> Option.defaultValue "Targets.json"

    let defaultTarget, targets =
        using (File.OpenRead(path)) JsonSerializer.Deserialize<Map<string, string>>
        |> Map.fold
            (fun (defaultTarget, targets) key value ->
                match key, defaultTarget with
                | "Default", None ->
                    let fixedValue = value |> applyFixes
                    Some fixedValue, targets
                | "Default", Some _ -> failwith "Multiple Default targets found"
                | _ ->
                    let urlPattern =
                        if key |> String.startsWithICIC "REGEX:" then
                            let pattern = key.Substring("REGEX:".Length)
                            UrlPattern.Regex(Regex(pattern, RegexOptions.IgnoreCase ||| RegexOptions.Compiled))
                        else
                            UrlPattern.StartsWith key

                    defaultTarget,
                    {
                        UrlPattern = urlPattern
                        TargetDirectory = value |> applyFixes
                    }
                    :: targets
            )
            (None, [])

    {
        Default =
            defaultTarget
            |> Option.defaultWith (fun () -> failwith "No Default target found")
        Targets = targets
    }

let (<!>) (a: 'a option) (b: unit -> 'a option) =
    match a with
    | Some a -> Some a
    | None -> b ()

[<RequireQualifiedAccess>]
module List =
    let tryRemove (value: 'a) (list: 'a list) : 'a list * bool =
        List.foldBack (fun item (acc, found) -> if item = value then (acc, true) else (item :: acc, found)) list ([], false)

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

let deductTargetDirectory (config: Configuration) (url: Uri) : string =

    let strUrl = url.ToString()

    let rec loop targets =
        match targets with
        | [] -> config.Default
        | target :: rest ->
            match target.UrlPattern with
            | UrlPattern.StartsWith prefix when strUrl |> String.startsWithICIC prefix -> target.TargetDirectory
            | UrlPattern.Regex regex when regex.IsMatch(strUrl) -> regex.Replace(strUrl, target.TargetDirectory)
            | _ -> loop rest

    loop config.Targets

let urlFixes = [
    // https://xxxx@dev.azure.com/yyy/xxx/_git/lorem -> https://xxxx@dev.azure.com/yyy/xxx/_git/lorem
    Regex.replace @"^(?<Before>https:\/\/)(?<User>.+?@)(?<After>dev\.azure\.com\/)" @"${Before}${After}"
    // https://xxx.visualstudio.com/ -> https://dev.azure.com/xxx/
    Regex.replace @"https://(?<Organization>\w+)\.visualstudio\.com/" @"https://dev.azure.com/${Organization}/"
    // https://dev.azure.com/xxx/DefaultCollection/ -> https://dev.azure.com/xxx/DefaultCollection/
    Regex.replace @"https://dev\.azure\.com/(?<Organization>\w+)/DefaultCollection/" @"https://dev.azure.com/${Organization}/"
]

let fixGitUrl url =
    List.fold (fun url fixer -> fixer url) url urlFixes

[<EntryPoint>]
let main argv =
    try
        let args, mergeRequest = argv |> Seq.toList |> List.tryRemove "--merge-request"

        let gitUrl =
            match args with
            | [ arg ] -> arg
            | [] -> ClipboardService.GetText()
            | _ -> failwith "Only one argument (git URL) is expected"

        let gitUrl = fixGitUrl gitUrl

        let gitUrl =
            match Uri.TryCreate(gitUrl, UriKind.Absolute) with
            | false, gitUrl -> failwith $"No valid URL found in clipboard %A{gitUrl}"
            | true, gitUrl ->
                printfn $"Cloning project from %s{gitUrl.ToString()}"
                gitUrl

        let gitUrl =
            if mergeRequest then
                gitUrl.ToString() |> Regex.replace "/-/merge_requests/.*$" ".git" |> Uri
            else
                gitUrl

        let configuration = readConfiguration ()

        let targetDirectory =
            deductTargetDirectory configuration gitUrl

        printfn $"Cloning %s{string gitUrl} into %s{targetDirectory}"

        let outputDirectory = (clone gitUrl targetDirectory) |> readOutputDirectory

        let path = Path.Combine(targetDirectory, outputDirectory)

        riderFixConfig path

        if mergeRequest then
            rider path
        else

            explorer path
            fork path
            rider path
            wt path

        printfn $"Project cloned into %s{path}"

        10. |> TimeSpan.FromSeconds |> Thread.Sleep

        0

    with ex ->
        printfn $"Error: %s{ex.Message}"
        printfn "Press Enter to exit..."
        Console.ReadLine() |> ignore
        1
