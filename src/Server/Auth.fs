/// Login web part and functions for API web part request authorisation with JWT.
module ServerCode.Auth
open Freya.Machines

open Freya.Core
open Freya.Machines.Http
open Freya.Types.Http
open ServerCode.Domain
open Server.Represent
open Server
let createUserData (login : Domain.Login) =
   {
       UserName = login.UserName
       Token    =
           ServerCode.JsonWebToken.encode (
               { UserName = login.UserName } : ServerTypes.UserRights
           )
   } : Domain.UserData

///// Checks if the HTTP request has a valid JWT token for API.
///// On success it will invoke the given `f` function by passing in the valid token.

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

let authMachine =
    freyaMachine {
        authorized isAuthorized
        badRequest noAuthHeader
        handleBadRequest missingAuthHeader
    }

///// Authenticates a user and returns a token in the HTTP body.

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

let loginMachine =
    freyaMachine {
        methods [OPTIONS;POST]
        authorized isLoginValid
        handleUnauthorized invalidLogin
        handleOk loginResponse
    }