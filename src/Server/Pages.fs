module ServerCode.Pages

open Client.Shared
open Freya.Core

let home = 
    let model: Model = {
            User = None
            PageModel = PageModel.HomePageModel
        }
        
    let view = Templates.index (Some model)
    Server.Represent.react view
    |> Freya.init

let notfound =
    Server.Represent.react (Templates.index None)
    |> Freya.init