/// Wish list API web parts and data access functions.
module ServerCode.WishList

open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Giraffe
open Microsoft.Extensions.Logging
open ServerCode.Domain
open System.Security.Claims
open Saturn.ControllerHelpers

/// Handle the GET on /api/wishlist
let getWishList (getWishListFromDB : string -> Task<WishList>) (userName:string) _next (ctx: HttpContext) =
    task {
        try
            let! wishList = getWishListFromDB userName
            return! ctx.WriteJsonAsync wishList
        with exn ->
            let msg = "Database not available"
            let logger = ctx.GetLogger "wishlist"
            logger.LogError (EventId(), exn, msg)
            return! Response.serviceUnavailable ctx "Database not available"
    }

/// Handle the POST on /api/wishlist
let postWishList (saveWishListToDB: WishList -> Task<unit>) _next (ctx: HttpContext) =
    task {
        let! wishList = ctx.BindJsonAsync<Domain.WishList>()

        let username = ctx.User.FindFirst ClaimTypes.NameIdentifier

        if username.Value <> wishList.UserName then
            return! Response.forbidden ctx (sprintf "WishList is not matching user %s" username.Value)
        else
            if wishList.Verify() then
                do! saveWishListToDB wishList
                return! ctx.WriteJsonAsync wishList
            else
                return! Response.badRequest ctx "WishList is not valid"
    }

/// Retrieve the last time the wish list was reset.
let getResetTime (getLastResetTime: unit -> Task<System.DateTime>) : HttpHandler =
    fun next ctx ->
        task {
            let! lastResetTime = getLastResetTime()
            return! ctx.WriteJsonAsync({ Time = lastResetTime })
        }