module Client.Utils

open Fable.Import

let load<'T> key =
    Browser.localStorage.getItem(key) |> unbox
    |> Option.map (JS.JSON.parse >> unbox<'T>)

let save key (data: 'T) =
    Browser.localStorage.setItem(key, JS.JSON.stringify data)

let delete key =
    Browser.localStorage.removeItem(key)