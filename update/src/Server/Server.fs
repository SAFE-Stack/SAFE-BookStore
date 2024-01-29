module Server

open System
open Azure.Data.Tables
open Azure.Storage.Blobs
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open ResetStorage
open Saturn
open Shared
open SAFE
open Storage
open Microsoft.Extensions.Azure
open Azure.Identity
open Quartz

module Option =
    let ofString input =
        match String.IsNullOrWhiteSpace input with
        | true -> None
        | false -> Some input

let systemStartTime = DateTime.UtcNow

let booksApi (context: HttpContext) =
    let tableStorage = context.GetService<TableServiceClient>()
    let blobStorage = context.GetService<BlobServiceClient>()

    {
        getBooks = fun () -> async { return Defaults.mockBooks }
        getWishlist = getWishListFromDB tableStorage
        addBook =
            fun (user, book) -> async {
                let! _ = addBook tableStorage user book
                return book
            }
        removeBook =
            fun (user, title) -> async {
                do! removeBook tableStorage user title
                return title
            }
        getLastResetTime = fun () -> getLastResetTime blobStorage systemStartTime
    }

let userApi = {
    login = fun user -> async { return Authorise.login user }
}

let auth =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue userApi
    |> Remoting.withErrorHandler ErrorHandling.errorHandler
    |> Remoting.buildHttpHandler

let books =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromContext booksApi
    |> Remoting.withErrorHandler ErrorHandling.errorHandler
    |> Remoting.buildHttpHandler

let webApp = choose [ auth; books ]

let configureServices (services: IServiceCollection) =
    let config = services.BuildServiceProvider().GetService<IConfiguration>()

    services.AddAzureClients(fun builder ->
        if Environment.GetEnvironmentVariable "ASPNETCORE_ENVIRONMENT" = Environments.Development then
            config.GetConnectionString "StorageAccount"
            |> Option.ofString
            |> Option.map (fun connectionString ->
                builder.AddBlobServiceClient connectionString |> ignore
                builder.AddTableServiceClient connectionString |> ignore)
            |> Option.defaultWith (fun () -> failwith "Storage account connection string not in app settings")
        else
            config["StorageAccountName"]
            |> Option.ofString
            |> Option.map (fun storageAccountName ->
                let storageUri service =
                    Uri $"https://{storageAccountName}.{service}.core.windows.net"

                let blobStorage = storageUri "blob"
                let tableStorage = storageUri "table"
                builder.AddBlobServiceClient(blobStorage) |> ignore
                builder.AddTableServiceClient(tableStorage) |> ignore
                builder.UseCredential(DefaultAzureCredential()) |> ignore)
            |> Option.defaultWith (fun () ->
                failwith "Storage account name has not been set in the app deployment settings"))

    services
        .AddQuartz(fun config ->
            let jobName = JobKey "reset-storage"

            config
                .AddJob<ResetStorageJob>(jobName)
                .AddTrigger(fun trigger -> trigger.ForJob(jobName).WithCronSchedule("0 * 0/2 * * ?") |> ignore)
            |> ignore)
        .AddQuartzHostedService(fun options -> options.WaitForJobsToComplete <- true)
    |> ignore

    services

let app = application {
    use_router webApp
    service_config configureServices
    memory_cache
    use_static "public"
    use_gzip
}

[<EntryPoint>]
let main _ =
    run app
    0