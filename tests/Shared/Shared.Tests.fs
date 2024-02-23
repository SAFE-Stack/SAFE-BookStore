module Shared.Tests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open Shared

let shared =
    testList "Shared" [
        let cases = [ "test", "test", true; "test1", "test", false; "test", "test1", false ]

        for user, password, expected in cases do
            testCase $"Login for user:{user} and password:{password}"
            <| fun _ ->
                let login = { UserName = user; Password = password }
                let actual = login.IsValid()
                Expect.equal actual expected ""
    ]