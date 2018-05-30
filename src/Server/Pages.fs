module ServerCode.Pages

open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open ServerCode.Domain
open ServerTypes
open Client.Shared
open Freya.Core

//
let home = 
    freya {
        let model: Model = {
            User = None
            PageModel = PageModel.HomePageModel
        }
        
        let view = Templates.index (Some model)
    
        return Server.Represent.react view 
    }
    
//let notfound: HttpHandler = fun _ ctx ->
//    ctx.WriteHtmlViewAsync (Templates.index None)
