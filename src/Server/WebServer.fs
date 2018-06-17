/// Functions for managing the Suave web server.
module ServerCode.WebServer

open ServerCode
open ServerCode.ServerUrls
open Giraffe
open Giraffe.TokenRouter
open RequestErrors
open Microsoft.AspNetCore.Http

/// Start the web server and connect to database
let webApp databaseType root =
    let startupTime = System.DateTime.UtcNow

    let db = Database.getDatabase databaseType startupTime
    let apiPathPrefix = PathString("/api")
    let notfound: HttpHandler =
        fun next ctx ->
            if ctx.Request.Path.StartsWithSegments(apiPathPrefix) then
                NOT_FOUND "Page not found" next ctx
            else
                Pages.notfound next ctx
    choose [
        Remote.remote db
        router notfound [
            GET [
                route PageUrls.Home Pages.home
            ]
        ]
    ]
