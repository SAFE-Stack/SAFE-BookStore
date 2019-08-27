module UITests.Runner

open canopy.classic
open canopy.types
open Expecto

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