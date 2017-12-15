/// Functions for managing the Suave web server.
module ServerCode.WebServer

open ServerCode
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
            route "/" (htmlFile (System.IO.Path.Combine(root,"index.html")))
            route ServerUrls.WishList (WishList.getWishList db.LoadWishList)
            route ServerUrls.ResetTime (WishList.getResetTime db.GetLastResetTime)
        ]

        POST [
            route ServerUrls.Login Auth.login
            route ServerUrls.WishList (WishList.postWishList db.SaveWishList)
        ]
    ]
