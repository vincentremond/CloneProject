namespace CloneProject

[<RequireQualifiedAccess>]
type UrlPattern =
    | StartsWith of string
    | Regex of pattern: string

type ConfigurationTarget = {
    UrlPattern: UrlPattern
    TargetDirectory: string
}

type Configuration = {
    Default: string
    Targets: ConfigurationTarget list
}
