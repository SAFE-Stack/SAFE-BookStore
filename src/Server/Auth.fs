/// Login web part and functions for API web part request authorisation with JWT.
module ServerCode.Auth
open Freya.Machines

open System
open Freya.Core
open Freya.Machines.Http
open Freya.Types.Http
//open System
//open Giraffe
//open RequestErrors
//open Microsoft.AspNetCore.Http
open ServerCode.Domain
open Server.Represent
open Server
//
let createUserData (login : Domain.Login) =
   {
       UserName = login.UserName
       Token    =
           ServerCode.JsonWebToken.encode (
               { UserName = login.UserName } : ServerTypes.UserRights
           )
   } : Domain.UserData

//let private missingToken = RequestErrors.BAD_REQUEST "Request doesn't contain a JSON Web Token"
//let private invalidToken = RequestErrors.FORBIDDEN "Accessing this API is not allowed"

let getAuthHeader =
    freya {
        let header =
            Freya.Optics.Http.Request.header_ "Authorization"
            |> Freya.Optic.get
        return! header
    } |> Freya.memo

let getUserFromAuthToken =
    freya {
        let! authHeader = getAuthHeader
        let user =
            authHeader
            |> Option.bind (fun (header:string) ->
                let jwt = header.Replace("Bearer ", "")
                JsonWebToken.isValid jwt
            )

        return user
    } |> Freya.memo

let isAuthorized =
    freya {
        let! user = getUserFromAuthToken
        return user.IsSome
    }

let noAuthHeader =
    freya {
        let! authHeader = getAuthHeader
        return authHeader.IsNone
    }

let missingAuthHeader =
    freya {
        return Represent.text "Request doesn't contain a JSON Web Token"
    }

///// Checks if the HTTP request has a valid JWT token for API.
///// On success it will invoke the given `f` function by passing in the valid token.
//let requiresJwtTokenForAPI f : HttpHandler =
//    fun (next : HttpFunc) (ctx : HttpContext) ->
//        (match ctx.TryGetRequestHeader "Authorization" with
//        | Some authHeader ->
//            let jwt = authHeader.Replace("Bearer ", "")
//            match JsonWebToken.isValid jwt with
//            | Some token -> f token
//            | None -> invalidToken
//        | None -> missingToken) next ctx

let authMachine =
    freyaMachine {
        authorized isAuthorized
        badRequest noAuthHeader
        handleBadRequest missingAuthHeader
    }

// /api/auth/login

let loginModel = freya { return! readJson<Domain.Login> } |> Freya.memo

let loginResponse = 
    freya {
        let! login =loginModel
        let data = createUserData login
        return Represent.json data
    }

let invalidLogin =
    freya {
        let! login = loginModel
        return Represent.text (sprintf "User '%s' can't be logged in." login.UserName)
    }

let isLoginValid =
    freya {
        let! model = loginModel
        return model.IsValid()
    }

//
///// Authenticates a user and returns a token in the HTTP body.
//let login : HttpHandler =
//    fun (next : HttpFunc) (ctx : HttpContext) ->
//        task {
//            let! login = ctx.BindJsonAsync<Domain.Login>()
//            return!
//                match login.IsValid() with
//                | true  ->
//                    let data = createUserData login
//                    ctx.WriteJsonAsync data
//                | false -> UNAUTHORIZED "Bearer" "" (sprintf "User '%s' can't be logged in." login.UserName) next ctx
//        }

let loginMachine =
    freyaMachine {
        methods [OPTIONS;POST]
        authorized isLoginValid
        handleUnauthorized invalidLogin
        handleOk loginResponse
    }