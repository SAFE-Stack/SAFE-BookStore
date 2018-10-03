module Decode

open System.IO
open System.Text
open Thoth.Json.Net
open Microsoft.AspNetCore.Http

let private readAllBytes (s : Stream) =
    let ms = new MemoryStream()
    s.CopyTo(ms)
    ms.ToArray()

let fromContext (ctx : HttpContext) (decoder : Decode.Decoder<'T>) =
    ctx.Request.Body
    |> readAllBytes
    |> Encoding.UTF8.GetString
    |> Decode.fromString decoder
