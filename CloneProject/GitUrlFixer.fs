namespace CloneProject

open Pinicola.FSharp.RegularExpressions

[<RequireQualifiedAccess>]
module GitUrlFixer =

    let private urlFixes = [
        // https://xxxx@dev.azure.com/yyy/xxx/_git/lorem -> https://xxxx@dev.azure.com/yyy/xxx/_git/lorem
        Regex.replacePattern @"^(?<Before>https:\/\/)(?<User>.+?@)(?<After>dev\.azure\.com\/)" @"${Before}${After}"
        // https://xxx.visualstudio.com/ -> https://dev.azure.com/xxx/
        Regex.replacePattern @"https://(?<Organization>\w+)\.visualstudio\.com/" @"https://dev.azure.com/${Organization}/"
        // https://dev.azure.com/xxx/DefaultCollection/ -> https://dev.azure.com/xxx/DefaultCollection/
        Regex.replacePattern @"https://dev\.azure\.com/(?<Organization>\w+)/DefaultCollection/" @"https://dev.azure.com/${Organization}/"
    ]

    let fix url =
        List.fold (fun url fixer -> fixer url) url urlFixes
