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
        let user : ServerTypes.UserRights = { UserName = login.UserName }
        let userData :Domain.UserData = { UserName = login.UserName; Token = JsonWebToken.encode user }
        return! FableJson.serialize userData next ctx
    with
    | _ -> 
        return! UNAUTHORIZED "Bearer" "" (sprintf "User '%s' can't be logged in." login.UserName) next ctx
}

/// Invokes a function that produces the output for a web part if the HttpContext
/// contains a valid auth token. Use to authorise the expressions in your web part
/// code (e.g. WishList.getWishList).
let useToken next (ctx: HttpContext) f = task {
    match ctx.Request.Headers.TryGetValue "Authorization" with
    | true, accesstoken -> 
        match Seq.tryHead accesstoken with 
        | Some t when t.StartsWith "Bearer " ->
            let jwt = t.Replace("Bearer ","")
            match JsonWebToken.isValid jwt with
            | None -> return! FORBIDDEN "Accessing this API is not allowed" next ctx
            | Some token -> return! f token
        | _ ->
           return! BAD_REQUEST "Request doesn't contain a JSON Web Token" next ctx
    | _ -> 
        return! BAD_REQUEST "Request doesn't contain a JSON Web Token" next ctx
}
