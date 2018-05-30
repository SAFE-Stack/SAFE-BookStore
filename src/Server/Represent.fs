module Server.Represent

open Newtonsoft.Json
open System
open Fable.Helpers.ReactServer
open Freya.Core
open Freya.Machines.Http
open Freya.Types.Http

let jsonConverter = Fable.JsonConverter() :> JsonConverter

let json<'a> value =
    let data =
        JsonConvert.SerializeObject(value, [|jsonConverter|])
        |> Text.UTF8Encoding.UTF8.GetBytes
    let desc =
        { Encodings = None
          Charset = Some Charset.Utf8
          Languages = None
          MediaType = Some MediaType.Json }
    { Data = data
      Description = desc }

let html (value:string) =
    let data = Text.UTF8Encoding.UTF8.GetBytes value
    let desc =
        {
            Encodings = None
            Charset = Some Charset.Utf8
            Languages = None
            MediaType = Some MediaType.Html
        }

    { Data = data ; Description = desc}
    
let react htmlNode =
    renderToString htmlNode
    |> html