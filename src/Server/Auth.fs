module ServerCode.Auth

open Suave
open Suave.RequestErrors

let unauthorized s = Suave.Response.response HTTP_401 s

let UNAUTHORIZED s = unauthorized (UTF8.bytes s)

let login (ctx: HttpContext) = async {
    let login = 
        ctx.request.rawForm 
        |> System.Text.Encoding.UTF8.GetString
        |> JsonUtils.ofJson<Domain.Login>

    try
        if (login.UserName <> "test" || login.Password <> "test") && 
           (login.UserName <> "test2" || login.Password <> "test2") then
            return! failwithf "Could not authenticate %s" login.UserName
        let user : ServerTypes.UserRights = { UserName = login.UserName }
        let token = TokenUtils.encode user

        return! Successful.OK token ctx
    with
    | _ -> return! UNAUTHORIZED (sprintf "User '%s' can't be logged in." login.UserName) ctx
}

let useToken ctx f = async {
    match ctx.request.header "Authorization" with
    | Choice1Of2 accesstoken when accesstoken.StartsWith "Bearer " -> 
        let jwt = accesstoken.Replace("Bearer ","")
        match TokenUtils.isValid jwt with
        | None -> return! FORBIDDEN "Accessing this API is not allowed" ctx
        | Some token -> return! f token
    | _ -> return! BAD_REQUEST "Request doesn't contain a JSON Web Token" ctx
}
