/// Functions for managing the database.
module ServerCode.Database

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

[<RequireQualifiedAccess>]
type DatabaseType = 
    | FileSystem 
    | AzureStorage of connectionString : AzureConnection

/// Start up background Azure web jobs with the given Azure connection.
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

type IDatabaseFunctions =
    abstract member LoadWishList : string -> Async<Domain.WishList>
    abstract member SaveWishList : Domain.WishList -> Async<unit>
    abstract member GetLastResetTime : unit -> Async<System.DateTime>

/// Start the web server and connect to database
let getDatabase (logger:Logger) databaseType startupTime =
    logger.logSimple (Message.event LogLevel.Info (sprintf "Using database %O" databaseType))
    match databaseType with
    | DatabaseType.AzureStorage connection ->
        startWebJobs connection
        { new IDatabaseFunctions with
            member __.LoadWishList key = Storage.AzureTable.getWishListFromDB connection key
            member __.SaveWishList wishList = Storage.AzureTable.saveWishListToDB connection wishList
            member __.GetLastResetTime () = async {
                let! resetTime = Storage.AzureTable.getLastResetTime connection
                return resetTime |> Option.defaultValue startupTime } }

    | DatabaseType.FileSystem ->
        { new IDatabaseFunctions with
            member __.LoadWishList key = async { return Storage.FileSystem.getWishListFromDB key }
            member __.SaveWishList wishList = async { return Storage.FileSystem.saveWishListToDB wishList }
            member __.GetLastResetTime () = async { return startupTime } } 

