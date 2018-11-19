/// Wish list API web parts and data access functions.
module ServerCode.WishList

open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Giraffe
open ServerCode.Domain
open ServerTypes
open FSharp.Control.Tasks.V2

/// Handle the GET on /api/wishlist
let getWishList (getWishListFromDB : string -> Task<WishList>) (token : UserRights) : HttpHandler =
     fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! wishList = getWishListFromDB token.UserName
            return! ctx.WriteJsonAsync wishList
        }



/// Handle the POST on /api/wishlist
let postWishList (saveWishListToDB: WishList -> Task<unit>) (token : UserRights) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! wishList = ctx.BindJsonAsync<Domain.WishList>()

            match token.UserName.Equals wishList.UserName with
            | true ->
                match Validation.verifyWishList wishList with
                | true ->
                    do! saveWishListToDB wishList
                    return! ctx.WriteJsonAsync wishList
                | false -> return! RequestErrors.FORBIDDEN (sprintf "WishList is not matching user %s" token.UserName) next ctx
            | false     -> return! RequestErrors.BAD_REQUEST "WishList is not valid" next ctx
        }

/// Retrieve the last time the wish list was reset.
let getResetTime (getLastResetTime: unit -> Task<System.DateTime>) : HttpHandler =
    fun next ctx ->
        task {
            let! lastResetTime = getLastResetTime()
            return! ctx.WriteJsonAsync({ Time = lastResetTime })
        }