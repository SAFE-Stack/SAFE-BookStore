/// Wish list API web parts and data access functions.
module ServerCode.WishList

open Suave
open Suave.Logging
open Suave.RequestErrors
open Suave.ServerErrors
open ServerCode.Domain
open Suave.Logging.Message

let logger = Log.create "FableSample"

/// Handle the GET on /api/wishlist
let getWishList getWishListFromDB (ctx: HttpContext) =
    Auth.useToken ctx (fun token -> async {
        try
            let! wishList = getWishListFromDB token.UserName
            return! Successful.OK (FableJson.toJson wishList) ctx
        with exn ->
            logger.error (eventX "SERVICE_UNAVAILABLE" >> addExn exn)
            return! SERVICE_UNAVAILABLE "Database not available" ctx
    })

/// Handle the POST on /api/wishlist
let postWishList saveWishListToDB (ctx: HttpContext) =
    Auth.useToken ctx (fun token -> async {
        try
            let wishList = 
                ctx.request.rawForm
                |> System.Text.Encoding.UTF8.GetString
                |> FableJson.ofJson<Domain.WishList>
            
            if token.UserName <> wishList.UserName then
                return! UNAUTHORIZED (sprintf "WishList is not matching user %s" token.UserName) ctx
            else
                if Validation.verifyWishList wishList then
                    do! saveWishListToDB wishList
                    return! Successful.OK (FableJson.toJson wishList) ctx
                else
                    return! BAD_REQUEST "WishList is not valid" ctx
        with exn ->
            logger.error (eventX "Database not available" >> addExn exn)
            return! SERVICE_UNAVAILABLE "Database not available" ctx
    })

/// Retrieve the last time the wish list was reset.
let getResetTime getLastResetTime ctx = async {
    let! lastResetTime = getLastResetTime()    
    return! Successful.OK (FableJson.toJson { Time = lastResetTime }) ctx }
