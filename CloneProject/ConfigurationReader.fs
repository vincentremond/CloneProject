namespace CloneProject

open System
open System.IO
open System.Text.Json
open Pinicola.FSharp

[<RequireQualifiedAccess>]
module ConfigurationReader =

    let read config =

        let fixes = [ "{{UserProfile}}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ]

        let applyFixes (s: string) =
            fixes
            |> List.fold (fun acc (oldValue, replacement) -> acc |> String.replace oldValue replacement) s

        let defaultTarget, targets =
            config
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

                                if pattern |> String.endsWithICIC "$" |> not then
                                    failwithf $"REGEX pattern must end with '$': %s{pattern}"

                                if pattern |> String.startsWithICIC "^" |> not then
                                    failwithf $"REGEX pattern must start with '^': %s{pattern}"

                                UrlPattern.Regex(pattern)
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

    let readFromFile (fileInfo: FileInfo) =
        using (fileInfo.OpenRead()) JsonSerializer.Deserialize<Map<string, string>>
        |> read

    let readFromContent (content: string) =
        JsonSerializer.Deserialize<Map<string, string>>(content) |> read
