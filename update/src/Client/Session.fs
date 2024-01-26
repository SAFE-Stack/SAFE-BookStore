module Session

open Shared
open Thoth.Json

let loadUser () : UserData option =
    let userDecoder = Decode.Auto.generateDecoder<UserData> ()

    match LocalStorage.load userDecoder "user" with
    | Ok user -> Some user
    | Error _ -> None

let deleteUser () = LocalStorage.delete "user"

let saveUser (user: UserData) = LocalStorage.save "user" user