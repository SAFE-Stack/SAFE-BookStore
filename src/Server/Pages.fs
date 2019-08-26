module ServerCode.Pages

open Giraffe
open Client.Shared
open FSharp.Control.Tasks.ContextInsensitive
open System.Threading.Tasks

let cachedWishList = ref None

let home (getWishListFromDB : string -> Task<Domain.WishList>) : HttpHandler = fun _ ctx ->
    task {
        let! wishList = task {
            match !cachedWishList with
            | Some wishList -> return wishList
            | _ ->
                let! wishList = getWishListFromDB "test"
                cachedWishList := Some wishList
                return wishList
        }
        let model: Model = {
            MenuModel = { User = None; RenderedOnServer = true }
            PageModel = HomePageModel { Version = ReleaseNotes.Version; WishList = Some wishList }
        }
        return! ctx.WriteHtmlViewAsync (Templates.index (Some model))
    }

let login: HttpHandler = fun _ ctx ->
    task {
        let model: Model = {
            MenuModel ={ User = None; RenderedOnServer = true }
            PageModel =
                let m,_ = Client.Login.init None
                LoginModel m
        }
        return! ctx.WriteHtmlViewAsync (Templates.index (Some model))
    }

let notfound: HttpHandler = fun _ ctx ->
    ctx.SetStatusCode 404
    ctx.WriteHtmlViewAsync (Templates.index None)
