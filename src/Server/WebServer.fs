/// Functions for managing the Suave web server.
module ServerCode.WebServer

open ServerCode
open Giraffe
open RequestErrors
open Saturn.Router
open Saturn.Pipeline

/// Start the web server and connect to database
let webApp databaseType =
    let startupTime = System.DateTime.UtcNow
    let db = Database.getDatabase databaseType startupTime

    let secured = scope {
        pipe_through jwtAuthentication
        get ServerUrls.WishList (WishList.getWishList db.LoadWishList)
        get ServerUrls.ResetTime (WishList.getResetTime db.GetLastResetTime)
        post ServerUrls.WishList (WishList.postWishList db.SaveWishList)
    }

    scope {
        error_handler (NOT_FOUND "Page not found")
        post ServerUrls.Login Auth.login
        forward "" secured
    }

