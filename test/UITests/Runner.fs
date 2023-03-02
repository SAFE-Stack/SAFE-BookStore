module UITests.Runner

open OpenQA.Selenium
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
        startBrowser()
        runTestsWithCLIArgs [CLIArguments.Sequenced] args Tests.tests
    finally
        quit()