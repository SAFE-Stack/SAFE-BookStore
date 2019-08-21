module ServerTests.Tests

open Expecto
open ServerCode
open ServerCode.Storage

let defaults = ServerCode.Storage.Defaults.defaultWishList "test"

let wishListTests =
    testList "Wishlist" [
        testCase "default contains F# mastering book" (fun _ ->
            Expect.isNonEmpty defaults.Books "Default Books list should have at least one item"
            Expect.isTrue
                (defaults.Books |> Seq.exists (fun b -> b.Title = "Mastering F#"))
                 "A good book should have been advertised"
        )

        testCase "adding a duplicate book is forbidden" (fun _ ->
            let duplicate = defaults.Books |> Seq.head
            let result = defaults.VerifyNewBookIsNotADuplicate duplicate
            Expect.isSome result "Can't add a duplicate"
        )
    ]