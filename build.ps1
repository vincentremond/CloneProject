$ErrorActionPreference = "Stop"

dotnet tool restore
dotnet build

AddToPath .\CloneProject\bin\Debug\
