module Server.Represent

open Newtonsoft.Json
open System
open System.IO

open Fable.Helpers.ReactServer
open Freya.Core
open Freya.Machines.Http
open Freya.Types.Http
open Freya.Types.Language
open Freya.Core
open Freya.Types.Http
open Freya.Types.Http.Cors
open Freya.Types.Language
open Freya.Types.Uri.Template
open Freya.Optics.Http
open ServerCode.FableJson
open Freya.Machines


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

let readBody =
    freya {
        let! body = Freya.Optic.get Request.body_

        let data =
            using(new StreamReader (body))(fun reader -> 
                reader.ReadToEnd ()
            )
        return data
    }

let readJson<'t> =
    freya {
        let! json = readBody
        return ofJson<'t> json
    }