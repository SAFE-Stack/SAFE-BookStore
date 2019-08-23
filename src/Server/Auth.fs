/// Login web part and functions for API web part request authorisation with JWT.
module ServerCode.Auth

open Giraffe
open Microsoft.AspNetCore.Http
open ServerCode.Domain
open FSharp.Control.Tasks.ContextInsensitive
open Saturn.ControllerHelpers

let createUserData (login : Domain.Login) : Domain.UserData =
    {
        UserName = login.UserName
        Token    = JsonWebToken.generateToken login.UserName
    }

/// Authenticates a user and returns a token in the HTTP body.
let login (next : HttpFunc) (ctx : HttpContext) = task {
    let! login = ctx.BindJsonAsync<Domain.Login>()
    return!
        if login.IsValid() then
            let data = createUserData login
            ctx.WriteJsonAsync data
        else
            Response.unauthorized ctx "Bearer" "" (sprintf "User '%s' can't be logged in." login.UserName)
}
