/// Functions for managing the Suave web server.
module ServerCode.WebServer

open ServerCode
open ServerCode.ServerUrls
open Freya.Core
open Freya.Core.Operators
open Freya.Machines.Http
open Freya.Types.Http
open Freya.Routers.Uri.Template
open Client
open Server

///// Start the web server and connect to database

let indexMachine =
    freyaMachine {
        methods [GET; HEAD; OPTIONS]
        handleOk Pages.home }

let apiPathPrefix = "/api"

let apiNotFound =
    Represent.text "Page not found"
    |> Freya.init

let generalNotFound =
    Freya.Optic.get Freya.Optics.Http.Request.path_
    |> Freya.bind (fun path ->
        match path.StartsWith(apiPathPrefix) with
        | true -> apiNotFound
        | false -> Pages.notfound
    )

let notFound =
    freyaMachine {
        exists (Freya.init false)
        handleNotFound generalNotFound
    }

let router (dbType:Database.DatabaseType) =
    let startupTime = System.DateTime.UtcNow
    let db = Database.getDatabase dbType startupTime
    freyaRouter {
        resource PageUrls.Home indexMachine
        resource APIUrls.WishList (WishList.wishListMachine db)
        resource APIUrls.Login Auth.loginMachine
        resource APIUrls.ResetTime (WishList.resetTimeMachine db)
    }

let root (dbType:Database.DatabaseType) =
    router dbType >?= notFound