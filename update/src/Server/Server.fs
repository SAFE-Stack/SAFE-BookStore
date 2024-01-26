module Server

open System
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Saturn
open Shared
open SAFE
open Storage
open Microsoft.Extensions.Hosting
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

let getStorageConnection (context: HttpContext) =
    if Environment.GetEnvironmentVariable "ASPNETCORE_ENVIRONMENT" = Environments.Development then
        Dev "UseDevelopmentStorage=true"
    else
        let storageAccountName = context.GetService<IConfiguration>().["StorageAccountName"]
        Deployed(StorageAccountName storageAccountName, DefaultAzureCredential())

let booksApi (context: HttpContext) =
    let connection = getStorageConnection context

    {
        getBooks = fun () -> async { return seq { mockBooks } |> Seq.concat }
        getWishlist = fun user -> async { return! getWishListFromDB connection user |> Async.AwaitTask }
        addBook =
            fun (user, book) -> async {
                let! _ = addBook connection user book |> Async.AwaitTask
                return book
            }
        removeBook =
            fun (user, title) -> async {
                do! removeBook connection user title |> Async.AwaitTask
                return title
            }
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

let app = application {
    use_router webApp
    memory_cache
    use_static "public"
    use_gzip
}

[<EntryPoint>]
let main _ =
    run app
    0