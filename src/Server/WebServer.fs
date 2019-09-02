/// Functions for managing the Giraffe web server.
module ServerCode.WebServer

open ServerCode
open Giraffe
open Saturn.Router

/// Start the web server and connect to database
let webApp databaseType =
    let startupTime = System.DateTime.UtcNow

    let db = Database.getDatabase databaseType startupTime


    let secured = router {
        pipe_through (Saturn.Auth.requireAuthentication Saturn.ChallengeType.JWT)
        post "/api/wishlist/%s" (WishList.postWishList db.SaveWishList)
    }

    router {
        not_found_handler Pages.notfound

        getf "/api/wishlist/%s" (WishList.getWishList db.LoadWishList)
        get "/api/resetTime/" (WishList.getResetTime db.GetLastResetTime)
        post "/api/users/login/" Auth.login

        // SSR
        get "" (Pages.home db.LoadWishList)
        get "/" (Pages.home db.LoadWishList)
        get "/login" Pages.login

        forward "" secured
    }