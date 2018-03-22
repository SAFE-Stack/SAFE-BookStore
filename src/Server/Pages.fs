module ServerCode.Pages

open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Giraffe
open ServerCode.Domain
open ServerTypes
open Client.Shared


let home: HttpHandler = fun _ ctx ->
    task {
        let model: Model = {
            User = None
            PageModel = PageModel.HomePageModel
        }
        return! ctx.WriteHtmlViewAsync (Templates.index (Some model))
    }

let notfound: HttpHandler = fun _ ctx ->
    task {
        return! ctx.WriteHtmlViewAsync (Templates.index None)
    }
