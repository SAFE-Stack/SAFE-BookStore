/// Wish list API web parts and data access functions.
module ServerCode.WishList

open Giraffe
open RequestErrors
open ServerErrors
open ServerCode.Domain
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open System.Threading.Tasks

/// Handle the GET on /api/wishlist
let getWishList (getWishListFromDB: string -> Task<WishList>) next (ctx: HttpContext) =
    Auth.useToken next ctx (fun token -> task {
        try
            let! wishList = getWishListFromDB token.UserName
            return! ctx.WriteJsonAsync wishList
        with exn ->
            let msg = "Database not available"
            let logger = ctx.GetLogger "wishlist"
            logger.LogError (EventId(), exn, msg)
            return! SERVICE_UNAVAILABLE "Database not available" next ctx
    })

/// Handle the POST on /api/wishlist
let postWishList (saveWishListToDB: WishList -> Task<unit>) next (ctx: HttpContext) =
    Auth.useToken next ctx (fun token -> task {
        try
            let! wishList = ctx.BindJsonAsync<Domain.WishList>()

            if token.UserName <> wishList.UserName then
                return! FORBIDDEN (sprintf "WishList is not matching user %s" token.UserName) next ctx
            else
                if Validation.verifyWishList wishList then
                    do! saveWishListToDB wishList
                    return! ctx.WriteJsonAsync wishList
                else
                    return! BAD_REQUEST "WishList is not valid" next ctx
        with exn ->
            let msg = "Database not available"
            let logger = ctx.GetLogger "wishlist"
            logger.LogError (EventId(), exn, msg)
            return! SERVICE_UNAVAILABLE "Database not available" next ctx
    })

/// Retrieve the last time the wish list was reset.
let getResetTime (getLastResetTime: unit -> Task<System.DateTime>) : HttpHandler =
    fun next ctx ->
        task {
            let! lastResetTime = getLastResetTime()
            return! ctx.WriteJsonAsync({ Time = lastResetTime })
        }
