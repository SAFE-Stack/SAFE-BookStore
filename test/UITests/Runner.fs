module UITests.Runner

open System.IO
open canopy
open Expecto
open System.Diagnostics
open System

let rec findPackages (di:DirectoryInfo) =
    if di = null then failwith "Could not find packages folder"
    let packages = DirectoryInfo(Path.Combine(di.FullName,"packages"))
    if packages.Exists then di else 
    findPackages di.Parent

let rootDir = findPackages (DirectoryInfo (Directory.GetCurrentDirectory()))

let startChrome() = 
    canopy.configuration.chromeDir <- Path.Combine(rootDir.FullName,"packages","test/Selenium.WebDriver.ChromeDriver/driver")
    start chrome

[<EntryPoint>]
let main args =
    try
        try
            startChrome()
            runTestsWithArgs { defaultConfig with ``parallel`` = false } args Tests.tests
        with e ->
            printfn "Error: %s" e.Message
            -1
    finally
        quit()