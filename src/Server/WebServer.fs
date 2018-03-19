/// Functions for managing the Suave web server.
module ServerCode.WebServer

open ServerCode
open ServerCode.ServerUrls
open Giraffe
open Giraffe.TokenRouter
open RequestErrors

/// Start the web server and connect to database
let webApp databaseType root =
    let startupTime = System.DateTime.UtcNow

    let db = Database.getDatabase databaseType startupTime

    let notfound = NOT_FOUND "Page not found"

    router notfound [
        GET [
            route PageUrls.Home (Auth.addUserDataForPage Pages.home)
            route PageUrls.Login (Auth.addUserDataForPage Pages.login)
            route PageUrls.WishList (Auth.requiresLoginForPage Pages.wishList)

            route APIUrls.WishList (Auth.requiresJwtTokenForAPI (WishList.getWishList db.LoadWishList))
            route APIUrls.ResetTime (WishList.getResetTime db.GetLastResetTime)
        ]

        POST [
            route APIUrls.Login Auth.login
            route APIUrls.WishList (Auth.requiresJwtTokenForAPI (WishList.postWishList db.SaveWishList))
        ]
    ]
