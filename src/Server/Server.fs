module Server

open Giraffe
open Microsoft.AspNetCore.Authentication.JwtBearer
open Saturn

let webApp =
    let authenticated =
        warbler (fun _ -> requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme))

    choose [ Api.create Api.guestApi; authenticated >=> Api.create Api.wishlistApi ]

let configureServices =
    Azure.addAppInsights >> Azure.addAzureStorage >> Jobs.addResetStorageJob

let app = application {
    use_router webApp
    service_config configureServices
    use_jwt_authentication Authorise.secret Authorise.issuer
    memory_cache
    use_static "public"
    use_gzip
}

[<EntryPoint>]
let main _ =
    run app
    0