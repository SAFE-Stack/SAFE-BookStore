module UITests.Runner

open System.IO
open canopy
open canopy.classic
open canopy.runner.classic
open canopy.types
open Expecto
open System.Diagnostics
open System

let startBrowser() =
    //start Chrome // Use this if you want to see your tests in the browser
    start ChromeHeadless
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