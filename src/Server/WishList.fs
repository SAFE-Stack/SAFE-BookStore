/// Wish list API web parts and data access functions.
module ServerCode.WishList

open Giraffe
open RequestErrors
open ServerErrors
open ServerCode.Domain
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open System.Threading.Tasks
open System.Security.Claims

/// Handle the GET on /api/wishlist
let getWishList (getWishListFromDB: string -> Task<WishList>) next (ctx: HttpContext) =
    task {
        try
            let username = ctx.User.FindFirst ClaimTypes.NameIdentifier
            let! wishList = getWishListFromDB username.Value
            return! FableJson.serialize wishList next ctx
        with exn ->
            let msg = "Database not available"
            let logger = ctx.GetLogger "wishlist"
            logger.LogError (EventId(), exn, msg)
            return! SERVICE_UNAVAILABLE "Database not available" next ctx
    }

/// Handle the POST on /api/wishlist
let postWishList (saveWishListToDB: WishList -> Task<unit>) next (ctx: HttpContext) =
    task {
        try
            let! wishList = FableJson.getJsonFromCtx<Domain.WishList> ctx
            let username = ctx.User.FindFirst ClaimTypes.NameIdentifier

            if username.Value <> wishList.UserName then
                return! FORBIDDEN (sprintf "WishList is not matching user %s" username.Value) next ctx
            else
                if Validation.verifyWishList wishList then
                    do! saveWishListToDB wishList
                    return! FableJson.serialize wishList next ctx
                else
                    return! BAD_REQUEST "WishList is not valid" next ctx
        with exn ->
            let msg = "Database not available"
            let logger = ctx.GetLogger "wishlist"
            logger.LogError (EventId(), exn, msg)
            return! SERVICE_UNAVAILABLE "Database not available" next ctx
    }

/// Retrieve the last time the wish list was reset.
let getResetTime (getLastResetTime: unit -> Task<System.DateTime>) next ctx = task {
    let! lastResetTime = getLastResetTime()
    return! FableJson.serialize { Time = lastResetTime } next ctx }
