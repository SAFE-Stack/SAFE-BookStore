module ServerCode.Pages

open Giraffe
open Client.Shared
open FSharp.Control.Tasks.ContextInsensitive
open System.Threading.Tasks

let home: HttpHandler = fun _ ctx ->
    task {
        let model: Model = {
            User = None
            RenderedOnServer = true
            PageModel = HomePageModel
        }
        return! ctx.WriteHtmlViewAsync (Templates.index (Some model))
    }

let login: HttpHandler = fun _ ctx ->
    task {
        let model: Model = {
            User = None
            RenderedOnServer = true
            PageModel =
                let m,_ = Client.Login.init None
                LoginModel m
        }
        return! ctx.WriteHtmlViewAsync (Templates.index (Some model))
    }

let wishList (getWishListFromDB : string -> Task<Domain.WishList>) (getLastResetTime: unit -> Task<System.DateTime>) (userName:string) : HttpHandler = fun _ ctx ->
    task {
        let! wishList = getWishListFromDB userName
        let! resetTime = getLastResetTime()
        let model: Model = {
            User = None
            RenderedOnServer = true
            PageModel =
                let m,_ = Client.WishList.initWithWishList wishList resetTime
                WishListModel m
        }
        return! ctx.WriteHtmlViewAsync (Templates.index (Some model))
    }

let notfound: HttpHandler = fun _ ctx ->
    ctx.SetStatusCode 404
    ctx.WriteHtmlViewAsync (Templates.index None)
