namespace CloneProject

open System
open System.Diagnostics
open System.Text
open Pinicola.FSharp
open RunProcess

[<RequireQualifiedAccess>]
module Process =
    let start exeShortName directory (arguments: string list) =
        let exe = Where.findInPath exeShortName
        Process.Start(ProcessStartInfo(exe.FullName, arguments, WorkingDirectory = directory)).WaitForExit()

    let explorer (path: string) = start "explorer" path [ path ]

    let fork (path: string) = start "fork" path []

    let rider (path: string) = start "rider" path [ path ]

    let wt (path: string) = start "wt" path []

    let riderFixConfig (path: string) = start "RiderFixConfig" path [ path ]

    let gitClone gitUrl targetDirectory =
        use proc = new ProcessHost("git", targetDirectory)

        proc.Start($"clone {gitUrl}")

        proc.WaitForExit(TimeSpan.MaxValue) |> ignore

        let stdErr = proc.StdErr.ReadAllText(Encoding.UTF8)
        let stdOut = proc.StdOut.ReadAllText(Encoding.UTF8)
        printfn $"%s{stdErr}"
        printfn $"%s{stdOut}"
        stdErr

