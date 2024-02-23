module Azure

open Azure.Identity
open Microsoft.Extensions.Azure
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open System

module private Option =
    let ofString input =
        match String.IsNullOrWhiteSpace input with
        | true -> None
        | false -> Some input

let addAppInsights (services: IServiceCollection) =
    if
        Environment.GetEnvironmentVariable "ASPNETCORE_ENVIRONMENT"
        <> Environments.Development
    then
        services.AddApplicationInsightsTelemetry() |> ignore

    services

let addAzureStorage (services: IServiceCollection) =
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