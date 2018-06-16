/// Login web part and functions for API web part request authorisation with JWT.
module ServerCode.Auth
open Freya.Machines

open Freya.Core
open Freya.Machines.Http
open Freya.Types.Http
open Freya.Optics.Http
open Freya.Core.Operators

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
    Request.Headers.authorization_
    |> Freya.Optic.get
    |> Freya.memo

let validateAuthHeader (header:string) =
    let jwt = header.Replace("Bearer ", "")
    JsonWebToken.isValid jwt

// Operators: https://github.com/xyncro/freya-core/blob/master/src/Freya.Core/Operators.fs
let getUserFromAuthToken =
    Option.bind validateAuthHeader <!> getAuthHeader
    |> Freya.memo

let isAuthorized =
    Option.isSome <!> getUserFromAuthToken 

let noAuthHeader =
    getAuthHeader
    |> Freya.map (Option.isNone)

let missingAuthHeader =
    Represent.text "Request doesn't contain a JSON Web Token"
    |> Freya.init

let authMachine =
    freyaMachine {
        authorized isAuthorized
        badRequest noAuthHeader
        handleBadRequest missingAuthHeader
    }

///// Authenticates a user and returns a token in the HTTP body.

let loginModel =
    readJson<Domain.Login> 
    |> Freya.memo

let loginResponse = 
    loginModel
    |> Freya.map (createUserData >> Represent.json)

let invalidLogin =
    let invalidText (login:Login) = sprintf "User '%s' can't be logged in." login.UserName

    loginModel
    |> Freya.map (invalidText >> Represent.text)

let isLoginValid =
    loginModel
    |> Freya.map (fun m -> m.IsValid())

let loginMachine =
    freyaMachine {
        methods [OPTIONS;POST]
        authorized isLoginValid
        handleUnauthorized invalidLogin
        handleOk loginResponse
    }