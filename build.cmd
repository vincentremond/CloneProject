@ECHO OFF

dotnet tool restore
dotnet build -- %*

add-to-path CloneProject\bin\Debug\
