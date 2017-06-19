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

            let logoutLinkSelector = ".logout"
            let loginLinkSelector = "Login"

            let logout = someElement logoutLinkSelector 
            if logout.IsSome then
                click logoutLinkSelector 
                waitForElement loginLinkSelector

            click loginLinkSelector

            "#username" << "test"
            "#password" << "test"

            click "Log In"
            
            waitForElement logoutLinkSelector
    ]
