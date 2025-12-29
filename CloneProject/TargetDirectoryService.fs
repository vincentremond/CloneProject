namespace CloneProject

open System
open Pinicola.FSharp
open Pinicola.FSharp.RegularExpressions

[<RequireQualifiedAccess>]
module TargetDirectoryService =

    let find (config: Configuration) (url: Uri) : string =

        let strUrl = url.ToString()

        let rec loop targets =
            match targets with
            | [] -> config.Default
            | target :: rest ->
                match target.UrlPattern with
                | UrlPattern.StartsWith prefix when strUrl |> String.startsWithICIC prefix -> target.TargetDirectory
                | UrlPattern.Regex pattern when strUrl |> Regex.isMatchPattern pattern -> strUrl |> Regex.replacePattern pattern target.TargetDirectory
                | _ -> loop rest

        loop config.Targets
