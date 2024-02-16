module Server.Tests

open Expecto

// add server tests here
let all = testList "All" [ Shared.Tests.shared; ]

[<EntryPoint>]
let main _ = runTestsWithCLIArgs [] [||] all