module CloneProject

open System.Runtime.InteropServices

module ConsoleConfiguration =
    let StdOutputHandle: uint = 0xFFFFFFF5u

    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern bool AllocConsole()

    [<DllImport("kernel32.dll")>]
    extern nativeint GetStdHandle(uint nStdHandle)

    [<DllImport("kernel32.dll")>]
    extern void SetStdHandle(uint nStdHandle, nativeint handle)
