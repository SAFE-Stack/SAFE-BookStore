/// Server program entry point module.
module ServerCode.Program

open System
open System.IO
open Microsoft.Extensions.Logging
open Newtonsoft.Json
open Giraffe
open Giraffe.HttpStatusCodeHandlers.ServerErrors

open Saturn

let GetEnvVar var =
    match Environment.GetEnvironmentVariable(var) with
    | null -> None
    | value -> Some value

let getPortsOrDefault defaultVal =
    match Environment.GetEnvironmentVariable("SUAVE_FABLE_PORT") with
    | null -> defaultVal
    | value -> value |> uint16

let errorHandler (ex : Exception) (logger : ILogger) =
    match ex with
    | :? Microsoft.WindowsAzure.Storage.StorageException as dbEx ->
        let msg = sprintf "An unhandled Windows Azure Storage exception has occured: %s" dbEx.Message
        logger.LogError (EventId(), dbEx, "An error has occured when hitting the database.")
        SERVICE_UNAVAILABLE msg
    | _ ->
        logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
        clearResponse >=> INTERNAL_ERROR ex.Message


let configureLogging (loggerBuilder : ILoggingBuilder) =
    loggerBuilder.AddFilter(fun lvl -> lvl.Equals LogLevel.Error)
                 .AddConsole()
                 .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    try
        let args = Array.toList args
        let clientPath =
            match args with
            | clientPath:: _  when Directory.Exists clientPath -> clientPath
            | _ ->
                // did we start from server folder?
                let devPath = Path.Combine("..","Client")
                if Directory.Exists devPath then devPath
                else
                    // maybe we are in root of project?
                    let devPath = Path.Combine("src","Client")
                    if Directory.Exists devPath then devPath
                    else @"./client"
            |> Path.GetFullPath

        let database =
            args
            |> List.tryFind(fun arg -> arg.StartsWith "AzureConnection=")
            |> Option.map(fun arg ->
                arg.Substring "AzureConnection=".Length
                |> ServerCode.Storage.AzureTable.AzureConnection
                |> Database.DatabaseType.AzureStorage)
            |> Option.defaultValue Database.DatabaseType.FileSystem

        let port = getPortsOrDefault 8085us

        let fableJsonSettings = JsonSerializerSettings()
        fableJsonSettings.Converters.Add(Fable.JsonConverter())

        let app = application {
            url ("http://0.0.0.0:" + port.ToString() + "/")
            use_router (WebServer.webApp database)

            use_static clientPath
            error_handler errorHandler
            use_json_settings fableJsonSettings
            logging configureLogging

        }

        run app
        0
    with
    | exn ->
        let color = Console.ForegroundColor
        Console.ForegroundColor <- System.ConsoleColor.Red
        Console.WriteLine(exn.Message)
        Console.ForegroundColor <- color
        1
