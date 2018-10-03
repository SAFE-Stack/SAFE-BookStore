/// Login web part and functions for API web part request authorisation with JWT.
module ServerCode.Auth

open System
open Giraffe
open RequestErrors
open Microsoft.AspNetCore.Http
open ServerCode.Domain
open FSharp.Control.Tasks.V2
open Thoth.Json.Net

let createUserData (login : Domain.Login) =
    {
        UserName = login.UserName
        Token    =
            ServerCode.JsonWebToken.encode (
                { UserName = login.UserName } : ServerTypes.UserRights
            )
    } : Domain.UserData

let inline private invalidLoginWithMsg msg =
    "Login is not valid.\n" + msg
    |> RequestErrors.BAD_REQUEST

/// Authenticates a user and returns a token in the HTTP body.
let login : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let login = Decode.fromContext ctx Domain.Login.Decoder

            match login with
            | Ok login ->
                return!
                    match login.IsValid() with
                    | true  ->
                        createUserData login
                        |> UserData.Encoder
                        |> Encode.toString 0
                        |> ctx.WriteStringAsync
                    | false -> UNAUTHORIZED "Bearer" "" (sprintf "User '%s' can't be logged in." login.UserName) next ctx
            | Error msg ->
                return! invalidLoginWithMsg msg next ctx
        }

let private missingToken = RequestErrors.BAD_REQUEST "Request doesn't contain a JSON Web Token"
let private invalidToken = RequestErrors.FORBIDDEN "Accessing this API is not allowed"

/// Checks if the HTTP request has a valid JWT token for API.
/// On success it will invoke the given `f` function by passing in the valid token.
let requiresJwtTokenForAPI f : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        (match ctx.TryGetRequestHeader "Authorization" with
        | Some authHeader ->
            let jwt = authHeader.Replace("Bearer ", "")
            match JsonWebToken.isValid jwt with
            | Some token -> f token
            | None -> invalidToken
        | None -> missingToken) next ctx
