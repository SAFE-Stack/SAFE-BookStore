/// Wish list API web parts and data access functions.
module ServerCode.WishList

open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Giraffe
open ServerCode.Domain
open ServerTypes
open FSharp.Control.Tasks.V2
open Thoth.Json.Net

/// Handle the GET on /api/wishlist
let getWishList (getWishListFromDB : string -> Task<WishList>) (token : UserRights) : HttpHandler =
     fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! wishList = getWishListFromDB token.UserName
            let json = WishList.Encoder wishList
                        |> Encode.toString 0
            return! ctx.WriteStringAsync json
        }

let private invalidWishList =
    RequestErrors.BAD_REQUEST "WishList is not valid"

let inline private forbiddenWishList username =
    sprintf "WishList is not matching user %s" username
    |> RequestErrors.FORBIDDEN

let inline private invalidWishListWithMsg msg =
    "WishList is not valid.\n" + msg
    |> RequestErrors.BAD_REQUEST

/// Handle the POST on /api/wishlist
let postWishList (saveWishListToDB: WishList -> Task<unit>) (token : UserRights) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            match Decode.fromContext ctx WishList.Decoder with
            | Ok wishList ->
                match token.UserName.Equals wishList.UserName with
                | true ->
                    match Validation.verifyWishList wishList with
                    | true ->
                        do! saveWishListToDB wishList
                        return!
                            WishList.Encoder wishList
                            |> Encode.toString 0
                            |> ctx.WriteStringAsync
                    | false -> return! forbiddenWishList token.UserName next ctx
                | false     -> return! invalidWishList next ctx
            | Error msg ->
                return! invalidWishListWithMsg msg next ctx
        }

/// Retrieve the last time the wish list was reset.
let getResetTime (getLastResetTime: unit -> Task<System.DateTime>) : HttpHandler =
    fun next ctx ->
        task {
            let! lastResetTime = getLastResetTime()
            let json = WishListResetDetails.Encoder { Time = lastResetTime }
                        |> Encode.toString 0
            return! ctx.WriteStringAsync json
        }
