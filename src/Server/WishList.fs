/// Wish list API web parts and data access functions.
module ServerCode.WishList

open Giraffe
open ServerCode.Domain
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open System.Threading.Tasks
open System.Security.Claims
open Saturn.ControllerHelpers

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
            return! Response.serviceUnavailable ctx "Database not available"
    }

/// Handle the POST on /api/wishlist
let postWishList (saveWishListToDB: WishList -> Task<unit>) next (ctx: HttpContext) =
    task {
        try
            let! wishList = FableJson.getJsonFromCtx<Domain.WishList> ctx
            let username = ctx.User.FindFirst ClaimTypes.NameIdentifier

            if username.Value <> wishList.UserName then
                return! Response.forbidden ctx (sprintf "WishList is not matching user %s" username.Value)
            else
                if Validation.verifyWishList wishList then
                    do! saveWishListToDB wishList
                    return! FableJson.serialize wishList next ctx
                else
                    return! Response.badRequest ctx "WishList is not valid"
        with exn ->
            let msg = "Database not available"
            let logger = ctx.GetLogger "wishlist"
            logger.LogError (EventId(), exn, msg)
            return! Response.serviceUnavailable ctx "Database not available"
    }

/// Retrieve the last time the wish list was reset.
let getResetTime (getLastResetTime: unit -> Task<System.DateTime>) next ctx = task {
    let! lastResetTime = getLastResetTime()
    return! FableJson.serialize { Time = lastResetTime } next ctx }
