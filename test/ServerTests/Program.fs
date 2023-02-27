module ServerTests.TestsRunner

open Expecto

[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [CLIArguments.Sequenced] args ServerTests.Tests.wishListTests
