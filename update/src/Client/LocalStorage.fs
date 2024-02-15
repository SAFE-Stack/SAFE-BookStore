module LocalStorage

open Thoth.Json
open Browser

let load (decoder: Decoder<'T>) key : Result<'T, string> =
    let o = WebStorage.localStorage.getItem key

    if isNull o then
        "No item found in local storage with key " + key |> Error
    else
        Decode.fromString decoder o

let delete key =
    WebStorage.localStorage.removeItem key
    let event = Browser.Event.Event.Create("storage")
    window.dispatchEvent event |> ignore

let inline save key (data: 'T) =
    WebStorage.localStorage.setItem (key, Encode.Auto.toString (0, data))