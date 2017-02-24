module UITests.Tests

open canopy
open System.IO
open Expecto
open System
open System.Threading

let serverUrl = "http://localhost:8085/"

let tests = 
    testList "client tests" [
        testCase "sound check - server is online" <| fun () ->
            url serverUrl
            waitForElement ".elmish-app"

            
        testCase "login with test user" <| fun () ->
            url serverUrl
            waitForElement ".elmish-app"

            let logout = someElement  ".logout"
            if logout.IsSome then click "Logout"

            click "Login"

            "#username" << "test"
            "#password" << "test"

            click "Log In"
            
            waitForElement "Isaac Abraham"
    ]
