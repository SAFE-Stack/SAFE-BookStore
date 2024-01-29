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
open Saturn
open Shared
open SAFE
open Storage
open Microsoft.Extensions.Azure
open Azure.Identity

let mockBooks = seq {
    {
        Title = "Get Programming with F#"
        Authors = "Isaac Abraham"
        ImageLink = "/images/Isaac.png"
        Link = "https://www.manning.com/books/get-programming-with-f-sharp"
    }

    {
        Title = "Mastering F#"
        Authors = "Alfonso Garcia-Caro Nunez"
        ImageLink = "/images/Alfonso.jpg"
        Link = "https://www.amazon.com/Mastering-F-Alfonso-Garcia-Caro-Nunez-ebook/dp/B01M112LR9"
    }

    {
        Title = "Stylish F#"
        Authors = "Kit Eason"
        ImageLink = "/images/Kit.jpg"
        Link = "https://www.apress.com/la/book/9781484239995"
    }
}

module Option =
    let ofString input =
        match String.IsNullOrWhiteSpace input with
        | true -> None
        | false -> Some input

let startTime = DateTime.UtcNow

let booksApi (context: HttpContext) =
    let tableStorage = context.GetService<TableServiceClient>()
    let blobStorage = context.GetService<BlobServiceClient>()

    {
        getBooks = fun () -> async { return seq { mockBooks } |> Seq.concat }
        getWishlist = fun user -> async { return! getWishListFromDB tableStorage user |> Async.AwaitTask }
        addBook =
            fun (user, book) -> async {
                let! _ = addBook tableStorage user book |> Async.AwaitTask
                return book
            }
        removeBook =
            fun (user, title) -> async {
                do! removeBook tableStorage user title |> Async.AwaitTask
                return title
            }
        getLastResetTime = fun () -> async { return! getLastResetTime blobStorage startTime |> Async.AwaitTask }
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