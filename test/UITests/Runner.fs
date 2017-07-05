module UITests.Runner

open System.IO
open canopy
open Expecto
open System.Diagnostics
open System

let rec findPackages (di:DirectoryInfo) =
    if isNull di then failwith "Could not find packages folder"
    let packages = DirectoryInfo(Path.Combine(di.FullName,"packages"))
    if packages.Exists then di else
    findPackages di.Parent

let rootDir = findPackages (DirectoryInfo (Directory.GetCurrentDirectory()))

let executingDir () = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)

let isWindows =
    match Environment.OSVersion.Platform with
    | PlatformID.Win32NT
    | PlatformID.Win32S
    | PlatformID.Win32Windows
    | PlatformID.WinCE -> true
    | _ -> false

let startBrowser() = 
    canopy.configuration.chromeDir <- executingDir()
    if isWindows then
        canopy.configuration.phantomJSDir <- Path.Combine(rootDir.FullName,"packages/test/PhantomJS/tools/phantomjs")
    else
        canopy.configuration.phantomJSDir <- Path.Combine(rootDir.FullName,"node_modules/phantomjs/bin")

    
    start phantomJS 
    resize (1280, 960)

[<EntryPoint>]
let main args =
    try
        try
            startBrowser()
            runTestsWithArgs { defaultConfig with ``parallel`` = false } args Tests.tests
        with e ->
            printfn "Error: %s" e.Message
            -1
    finally
        quit()