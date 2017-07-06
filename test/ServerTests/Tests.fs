module ServerTests.Tests

open Expecto
open ServerCode

let wishListTests =
  testList "Wishlist" [
    testCase "default contains F# mastering book" <| fun _ ->
      let defaults =  WishList.defaultWishList "test"
      Expect.isNonEmpty defaults.Books "Default Books list should have at least one item"
      Expect.isTrue
        (defaults.Books |> Seq.exists (fun b -> b.Title = "Mastering F#")) 
        "A good book should have been advertised"
  ]