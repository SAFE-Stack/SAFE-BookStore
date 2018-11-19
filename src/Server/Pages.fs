module ServerCode.Pages

open Giraffe
open Client.Shared
open FSharp.Control.Tasks.V2

let home: HttpHandler = fun _ ctx ->
    task {
        let model: Model = {
            User = None
            PageModel = PageModel.HomePageModel
        }
        return! ctx.WriteHtmlViewAsync (Templates.index (Some model))
    }

let login: HttpHandler = fun _ ctx ->
    task {
        let model: Model = {
            User = None
            PageModel =
                let m,_ = Client.Login.init None
                PageModel.LoginModel m
        }
        return! ctx.WriteHtmlViewAsync (Templates.index (Some model))
    }

let notfound: HttpHandler = fun _ ctx ->
    ctx.WriteHtmlViewAsync (Templates.index None)
