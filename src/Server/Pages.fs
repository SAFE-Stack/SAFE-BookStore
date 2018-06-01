module ServerCode.Pages

open Client.Shared
open Freya.Core

let home = 
    freya {
        let model: Model = {
            User = None
            PageModel = PageModel.HomePageModel
        }
        
        let view = Templates.index (Some model)
    
        return Server.Represent.react view 
    }
    
let notfound =
    freya {
        return Server.Represent.react (Templates.index None) 
    }