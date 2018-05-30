/// Server program entry point module.
module ServerCode.Program

open System
open System.IO
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Newtonsoft.Json
open KestrelInterop
open Microsoft.AspNetCore.Builder.Internal

let GetEnvVar var =
    match Environment.GetEnvironmentVariable(var) with
    | null -> None
    | value -> Some value

let getPortsOrDefault defaultVal =
    match Environment.GetEnvironmentVariable("SUAVE_FABLE_PORT") with
    | null -> defaultVal
    | value -> value |> uint16

let configureApp db root (app : IApplicationBuilder) =
    app.UseStaticFiles() |> ignore
    ApplicationBuilder.useFreya (ServerCode.WebServer.root db) app |> ignore

let configureServices (services : IServiceCollection) =
    ()

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

        WebHost
            .CreateDefaultBuilder()
            .UseWebRoot(clientPath)
            .UseContentRoot(clientPath)
            .ConfigureLogging(configureLogging)
            .ConfigureServices(configureServices)
            .Configure(Action<IApplicationBuilder> (configureApp database clientPath))
            .UseUrls("http://localhost:" + port.ToString() + "/")
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
