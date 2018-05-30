/// Functions for managing the Suave web server.
module ServerCode.WebServer

open ServerCode
open ServerCode.ServerUrls
//open RequestErrors
open Microsoft.AspNetCore.Http
open Freya.Core
open Freya.Machines.Http
open Freya.Types.Http
open Freya.Routers.Uri.Template
open ServerCode

///// Start the web server and connect to database
//let webApp databaseType root =
//    let startupTime = System.DateTime.UtcNow
//
//    let db = Database.getDatabase databaseType startupTime
//    let apiPathPrefix = PathString("/api")
//    let notfound: HttpHandler =
//        fun next ctx ->
//            if ctx.Request.Path.StartsWithSegments(apiPathPrefix) then
//                NOT_FOUND "Page not found" next ctx
//            else
//                Pages.notfound next ctx
//
//    router notfound [
//        GET [
//            route PageUrls.Home Pages.home
//
//            route APIUrls.WishList (Auth.requiresJwtTokenForAPI (WishList.getWishList db.LoadWishList))
//            route APIUrls.ResetTime (WishList.getResetTime db.GetLastResetTime)
//        ]
//
//        POST [
//            route APIUrls.Login Auth.login
//            route APIUrls.WishList (Auth.requiresJwtTokenForAPI (WishList.postWishList db.SaveWishList))
//        ]
//    ]



let indexMachine =
    freyaMachine {
        methods [GET; HEAD; OPTIONS]
        handleOk Pages.home }

let root (db:Database.DatabaseType) =
    freyaRouter {
        resource PageUrls.Home indexMachine }