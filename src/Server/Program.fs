/// Server program entry point module.
module ServerCode.Program

open System
open System.IO
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.Serialization.Json
open Giraffe.HttpStatusCodeHandlers.ServerErrors
open Thoth.Json.Giraffe

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

let configureApp db (app : IApplicationBuilder) =
    app.UseGiraffeErrorHandler(errorHandler)
       .UseStaticFiles()
       .UseGiraffe (WebServer.webApp db)

let configureServices (services : IServiceCollection) =
    // Add default Giraffe dependencies
    services.AddGiraffe() |> ignore

    services.AddSingleton<IJsonSerializer>(ThothSerializer())
    |> ignore

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
                    let devPath = Path.Combine("src", "Client")
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

        WebHost
            .CreateDefaultBuilder()
            .UseWebRoot(clientPath)
            .UseContentRoot(clientPath)
            .ConfigureLogging(configureLogging)
            .ConfigureServices(configureServices)
            .Configure(Action<IApplicationBuilder> (configureApp database))
            .UseUrls("http://0.0.0.0:" + port.ToString() + "/")
            .Build()
            .Run()
        0
    with
    | exn ->
        let color = Console.ForegroundColor
        Console.ForegroundColor <- System.ConsoleColor.Red
        Console.WriteLine(exn.Message)
        Console.ForegroundColor <- color
        1
