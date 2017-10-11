/// Functions for managing the Suave web server.
module ServerCode.WebServer

open System.IO
open Suave
open Suave.Logging
open System.Net
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open Microsoft.Azure.WebJobs
open ServerCode.Storage.AzureTable
open ServerCode

type DatabaseType = 
    | FileSystem 
    | Azure of connectionString : AzureConnection

// Start up background Azure web jobs.
let startWebJobs azureConnection =    
    let host =
        let config =
            let (AzureConnection connectionString) = azureConnection
            JobHostConfiguration(
                DashboardConnectionString = connectionString,
                StorageConnectionString = connectionString)
        config.UseTimers()
        config.JobActivator <- ServerCode.Storage.WishListWebJobsActivator azureConnection
        new JobHost(config)
    host.Start()

// Fire up our web server!
let start databaseType clientPath port =
    let startupTime = System.DateTime.UtcNow
    if not (Directory.Exists clientPath) then
        failwithf "Client-HomePath '%s' doesn't exist." clientPath

    let logger = Logging.Targets.create Logging.Info [| "Suave" |]
    let serverConfig =
        { defaultConfig with
            logger = Targets.create LogLevel.Debug [|"ServerCode"; "Server" |]
            homeFolder = Some clientPath
            bindings = [ HttpBinding.create HTTP (IPAddress.Parse "0.0.0.0") port] }

    let loadFromDb, saveToDb, getLastResetTime =
        logger.logSimple (Message.event LogLevel.Info (sprintf "Using database %O" databaseType))
        match databaseType with
        | Azure connection ->
            startWebJobs connection
            Storage.AzureTable.getWishListFromDB connection, Storage.AzureTable.saveWishListToDB connection, Storage.AzureTable.getLastResetTime startupTime connection
        | FileSystem ->
            Storage.FileSystem.getWishListFromDB >> async.Return, Storage.FileSystem.saveWishListToDB >> async.Return, fun _ -> async.Return startupTime

    let app =
        choose [
            GET >=> choose [
                path "/" >=> Files.browseFileHome "index.html"
                pathRegex @"/(public|js|css|Images)/(.*)\.(css|png|gif|jpg|js|map)" >=> Files.browseHome
                path ServerUrls.WishList >=> WishList.getWishList loadFromDb
                path ServerUrls.ResetTime >=> WishList.getResetTime getLastResetTime ]

            POST >=> choose [
                path ServerUrls.Login >=> Auth.login
                path ServerUrls.WishList >=> WishList.postWishList saveToDb
            ]

            NOT_FOUND "Page not found."

        ] >=> logWithLevelStructured Logging.Info logger logFormatStructured

    startWebServer serverConfig app
