module Session

open Shared
open Thoth.Json

[<Literal>]
let USER_SESSION_KEY = "user"

let loadUser () : UserData option =
    let userDecoder = Decode.Auto.generateDecoder<UserData> ()

    match LocalStorage.load userDecoder USER_SESSION_KEY with
    | Ok user -> Some user
    | Error _ -> None

let deleteUser () = LocalStorage.delete USER_SESSION_KEY

let saveUser (user: UserData) = LocalStorage.save USER_SESSION_KEY user