namespace SAFE

open Elmish.SweetAlert
open Fable.Core
open Fable.Remoting.Client
open Fable.SimpleJson

[<AutoOpen>]
module Extensions =
    type System.Exception with

        member this.GetPropagatedError() =
            match this with
            | :? ProxyRequestException as exn ->
                let response =
                    exn.ResponseText
                    |> Json.parseAs<{|
                        error: {| ClassName: string; Message: string |}
                        ignored: bool
                        handled: bool
                    |} >

                response.error
            | ex -> {|
                ClassName = "Unknown"
                Message = ex.Message
              |}

        member this.AsAlert() =
            SimpleAlert(this.GetPropagatedError().Message)
                .Type(AlertType.Error)
                .Title("Critical Error")
            |> SweetAlert.Run