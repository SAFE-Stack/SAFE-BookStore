/// Provides a nicer interop for configuring and starting the Kestrel server.
module KestrelInterop

open Freya.Core
open Microsoft.AspNetCore.Builder

module ApplicationBuilder =
    type IApplicationBuilder with
        member this.UseFreya (f:Freya.Core.Pipeline) =
            let owin : OwinMidFunc = OwinMidFunc.ofFreya f
            this.UseOwin(fun p -> p.Invoke owin) |> ignore