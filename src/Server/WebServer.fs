/// Functions for managing the Giraffe web server.
module ServerCode.WebServer

open ServerCode
open ServerCode.ServerUrls
open Giraffe
open Saturn.Router
open Saturn.Pipeline
open Microsoft.AspNetCore.Http

/// Start the web server and connect to database
let webApp databaseType =
    let startupTime = System.DateTime.UtcNow

    let db = Database.getDatabase databaseType startupTime
    let apiPathPrefix = PathString("/api")


    let secured = router {
        pipe_through jwtAuthentication
        get ServerUrls.WishList (WishList.getWishList db.LoadWishList)
        get ServerUrls.ResetTime (WishList.getResetTime db.GetLastResetTime)
        post ServerUrls.WishList (WishList.postWishList db.SaveWishList)
    }

    router {
        error_handler (NOT_FOUND "Page not found")
        get "/" (htmlFile "index.html")
        post ServerUrls.Login Auth.login
        forward "" secured
    }