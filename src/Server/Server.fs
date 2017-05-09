module ServerCode.Server

open System.IO
open Suave
open Suave.Logging
open System.Net
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors

let startServer clientPath =
    if not (Directory.Exists clientPath) then
        failwithf "Client-HomePath '%s' doesn't exist." clientPath

    // let outPath = Path.Combine(clientPath,"public")
    // if not (Directory.Exists outPath) then
    //     failwithf "Out-HomePath '%s' doesn't exist." outPath

    // if Directory.EnumerateFiles outPath |> Seq.isEmpty then
    //     failwithf "Out-HomePath '%s' is empty." outPath

    let logger = Logging.Targets.create Logging.Info [| "Suave" |]

    let serverConfig =
        { defaultConfig with
            logger = Targets.create LogLevel.Debug [|"ServerCode"; "Server" |]
            homeFolder = Some clientPath
            bindings = [ HttpBinding.create HTTP (IPAddress.Parse "0.0.0.0") 8085us] }

    let app =
        choose [
            GET >=> choose [
                path "/" >=> Files.browseFileHome "index.html"
                pathRegex @"/(public|js|css|Images)/(.*)\.(css|png|gif|jpg|js|map)" >=> Files.browseHome

                path "/api/wishlist/" >=> WishList.getWishList ]

            POST >=> choose [
                path "/api/users/login" >=> Auth.login

                path "/api/wishlist/" >=> WishList.postWishList
            ]

            NOT_FOUND "Page not found."

        ] >=> logWithLevelStructured Logging.Info logger logFormatStructured

    startWebServer serverConfig app
