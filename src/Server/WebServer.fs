/// Functions for managing the Giraffe web server.
module ServerCode.WebServer

open ServerCode
open ServerCode.ServerUrls
open Giraffe
open Giraffe.TokenRouter
open RequestErrors
open Microsoft.AspNetCore.Http

/// Start the web server and connect to database
let webApp databaseType =
    let startupTime = System.DateTime.UtcNow

    let db = Database.getDatabase databaseType startupTime
    let apiPathPrefix = PathString("/api")
    let notfound: HttpHandler =
        fun next ctx ->
            if ctx.Request.Path.StartsWithSegments(apiPathPrefix) then
                NOT_FOUND "Page not found" next ctx
            else
                Pages.notfound next ctx

    router notfound [
        GET [
            route PageUrls.Home Pages.home
            route PageUrls.Login Pages.login

            route APIUrls.WishList (Auth.requiresJwtTokenForAPI (WishList.getWishList db.LoadWishList))
            route APIUrls.ResetTime (WishList.getResetTime db.GetLastResetTime)
        ]

        POST [
            route APIUrls.Login Auth.login
            route APIUrls.WishList (Auth.requiresJwtTokenForAPI (WishList.postWishList db.SaveWishList))
        ]
    ]
