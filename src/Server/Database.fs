/// Functions for managing the database.
module ServerCode.Database

open Microsoft.Azure.WebJobs
open ServerCode.Storage.AzureTable
open ServerCode
open System.Threading.Tasks
open Giraffe.Tasks

[<RequireQualifiedAccess>]
type DatabaseType = 
    | FileSystem 
    | AzureStorage of connectionString : AzureConnection

type IDatabaseFunctions =
    abstract member LoadWishList : string -> Task<Domain.WishList>
    abstract member SaveWishList : Domain.WishList -> Task<unit>
    abstract member GetLastResetTime : unit -> Task<System.DateTime>

/// Start the web server and connect to database
let getDatabase databaseType startupTime =
    match databaseType with
    | DatabaseType.AzureStorage connection ->
        Storage.WebJobs.startWebJobs connection
        { new IDatabaseFunctions with
            member __.LoadWishList key = Storage.AzureTable.getWishListFromDB connection key
            member __.SaveWishList wishList = Storage.AzureTable.saveWishListToDB connection wishList
            member __.GetLastResetTime () = task {
                let! resetTime = Storage.AzureTable.getLastResetTime connection
                return resetTime |> Option.defaultValue startupTime } }

    | DatabaseType.FileSystem ->
        { new IDatabaseFunctions with
            member __.LoadWishList key = task { return Storage.FileSystem.getWishListFromDB key }
            member __.SaveWishList wishList = task { return Storage.FileSystem.saveWishListToDB wishList }
            member __.GetLastResetTime () = task { return startupTime } } 

