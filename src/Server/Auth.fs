/// Login web part and functions for API web part request authorisation with JWT.
module ServerCode.Auth

open Giraffe
open RequestErrors
open Microsoft.AspNetCore.Http

/// Login web part that authenticates a user and returns a token in the HTTP body.
let login next (ctx: HttpContext) = task {
    let! login = FableJson.getJsonFromCtx<Domain.Login> ctx

    try
        if (login.UserName <> "test" || login.Password <> "test") &&
           (login.UserName <> "test2" || login.Password <> "test2") then
            return! failwithf "Could not authenticate %s" login.UserName
        let token = JsonWebToken.generateToken login.UserName
        let userData :Domain.UserData = { UserName = login.UserName; Token = token }
        return! FableJson.serialize userData next ctx
    with
    | _ ->
        return! UNAUTHORIZED "Bearer" "" (sprintf "User '%s' can't be logged in." login.UserName) next ctx
}

