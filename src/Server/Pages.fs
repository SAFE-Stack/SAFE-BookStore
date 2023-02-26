module ServerCode.Pages

open Giraffe
open Client.Shared
open System.Threading.Tasks

let home (getWishListFromDB : string -> Task<Domain.WishList>) : HttpHandler = fun _ ctx ->
    task {
        let! wishList = getWishListFromDB "test"
        let model: Model = {
            MenuModel = { User = None; RenderedOnServer = true }
            PageModel = HomePageModel { WishList = Some wishList }
        }
        let page = Templates.index model
        return! ctx.WriteHtmlViewAsync page
    }

let login: HttpHandler = fun _ ctx ->
    task {
        let model: Model = {
            MenuModel ={ User = None; RenderedOnServer = true }
            PageModel =
                let m,_ = Client.Login.init None
                LoginModel m
        }
        return! ctx.WriteHtmlViewAsync (Templates.index model)
    }

let notfoundModel: Model = {
    MenuModel = { User = None; RenderedOnServer = true }
    PageModel = NotFoundModel
}

let notfound: HttpHandler = fun _ ctx ->

    ctx.SetStatusCode 404
    ctx.WriteHtmlViewAsync (Templates.index notfoundModel)
