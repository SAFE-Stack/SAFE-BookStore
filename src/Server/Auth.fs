module ServerCode.Auth

open Suave
open Newtonsoft.Json
open Suave.RequestErrors

let unauthorized s = Suave.Response.response HTTP_401 s

let UNAUTHORIZED s = unauthorized (UTF8.bytes s)

let login (ctx: HttpContext) = async {
    let login = 
        ctx.request.rawForm 
        |> System.Text.Encoding.UTF8.GetString
        |> JsonConvert.DeserializeObject<Domain.Login>

    try
        if login.UserName <> "test" || login.Password <> "test" then
            return! failwithf "Could not authenticate %s" login.UserName
        let user : ServerTypes.User = { UserName = login.UserName }
        let token = TokenUtils.encode user

        return! Successful.OK token ctx
    with
    | _ -> return! UNAUTHORIZED (sprintf "User '%s' can't be logged in." login.UserName) ctx
}

let useToken f =
    context (fun ctx ->
        match ctx.request.header "Authorization" with
        | Choice1Of2 accesstoken when accesstoken.StartsWith "Bearer " -> 
            let jwt = accesstoken.Replace("Bearer ","")
            match TokenUtils.isValid jwt with
            | Some token -> f token
            | _ ->
                FORBIDDEN "Accessing this API is not allowed"
        | _ -> BAD_REQUEST "Request doesn't contain a JSON Web Token")

let checkToken ctx f webpart = async {
    match ctx.request.header "Authorization" with
    | Choice1Of2 accesstoken when accesstoken.StartsWith "Bearer " -> 
        let jwt = accesstoken.Replace("Bearer ","")
        match TokenUtils.isValid jwt with
        | Some token when f token ->
            return! webpart
        | _ ->
            return! FORBIDDEN "Accessing this API is not allowed" ctx
    | _ -> return! BAD_REQUEST "Request doesn't contain a JSON Web Token" ctx
}

let checkQueryStringToken ctx f webpart = async {
    match ctx.request.queryParam "token" with
    | Choice1Of2 accesstoken -> 
        let ok = try f accesstoken with | _ -> false
        if ok then
            return! webpart
        else
            return! FORBIDDEN "Accessing this API is not allowed" ctx
    | _ -> return! BAD_REQUEST "Request doesn't contain a JSON Web Token" ctx
}