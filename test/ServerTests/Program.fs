module ServerTests.TestsRunner

open Expecto

[<EntryPoint>]
let main args =
        runTestsWithArgs { defaultConfig with ``parallel`` = false } args ServerTests.Tests.wishListTests
