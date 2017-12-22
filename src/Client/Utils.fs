module Client.Utils

open Fable.Import
open Fable.Core.JsInterop

let inline load<'T> key =
    Browser.localStorage.getItem(key) |> unbox
    |> Option.map ofJson<'T>

let inline delete key =
    Browser.localStorage.removeItem(key)

let inline save<'T> key (data: 'T) =
    delete key
    Browser.localStorage.setItem(key, toJson data)
