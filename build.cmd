@ECHO OFF

dotnet tool restore
dotnet build -- %*

AddToPath .\CloneProject\bin\Debug\
