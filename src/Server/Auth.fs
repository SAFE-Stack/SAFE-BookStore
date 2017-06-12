module ServerCode.Auth

open ServerCode.Domain

let users = 
    [ "admin", "pass"
      "user", "qwerty" 
      "employee", "12345" ] 

let authorize (info: LoginInfo) = 
    users
    |> List.tryFind (fun (userName, pass) ->
         userName = info.UserName && pass = info.Password)
    |> Option.map (fun _ -> Token.encode info)
    |> Async.result