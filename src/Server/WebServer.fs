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


    let secured = router {
        pipe_through (Saturn.Auth.requireAuthentication Saturn.ChallengeType.JWT)
        get ServerUrls.APIUrls.WishList (WishList.getWishList db.LoadWishList)
        get ServerUrls.APIUrls.ResetTime (WishList.getResetTime db.GetLastResetTime)
        post ServerUrls.APIUrls.WishList (WishList.postWishList db.SaveWishList)
    }

    router {
        not_found_handler (RequestErrors.NOT_FOUND "Page not found")
        get ServerUrls.PageUrls.Home (htmlFile "index.html")
        post ServerUrls.APIUrls.Login Auth.login
        forward "" secured
    }