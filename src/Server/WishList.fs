/// Wish list API web parts and data access functions.
module ServerCode.WishList

open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open ServerCode.Domain
open ServerTypes

/// Handle the GET on /api/wishlist
//let getWishList (getWishListFromDB : string -> Task<WishList>) (token : UserRights) : HttpHandler =
//     fun (next : HttpFunc) (ctx : HttpContext) ->
//        task {
//            let! wishList = getWishListFromDB token.UserName
//            return! ctx.WriteJsonAsync wishList
//        }
//
//let private invalidWishList =
//    RequestErrors.BAD_REQUEST "WishList is not valid"
//
//let inline private forbiddenWishList username =
//    sprintf "WishList is not matching user %s" username
//    |> RequestErrors.FORBIDDEN
//
///// Handle the POST on /api/wishlist
//let postWishList (saveWishListToDB: WishList -> Task<unit>) (token : UserRights) : HttpHandler =
//    fun (next : HttpFunc) (ctx : HttpContext) ->
//        task {
//            let! wishList = ctx.BindJsonAsync<Domain.WishList>()
//
//            match token.UserName.Equals wishList.UserName with
//            | true ->
//                match Validation.verifyWishList wishList with
//                | true ->
//                    do! saveWishListToDB wishList
//                    return! ctx.WriteJsonAsync wishList
//                | false -> return! forbiddenWishList token.UserName next ctx
//            | false     -> return! invalidWishList next ctx
//        }
//
///// Retrieve the last time the wish list was reset.
//let getResetTime (getLastResetTime: unit -> Task<System.DateTime>) : HttpHandler =
//    fun next ctx ->
//        task {
//            let! lastResetTime = getLastResetTime()
//            return! ctx.WriteJsonAsync({ Time = lastResetTime })
//        }