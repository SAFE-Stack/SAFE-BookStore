module ServerCode.Pages



open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Giraffe
open ServerCode.Domain
open ServerTypes
open Client.Shared


// Retrieve the last time the wish list was reset.
let home (user: UserData option): HttpHandler = fun _ ctx ->
    task {
        let model: Model = {
            User = user
            PageModel = PageModel.HomePageModel
        }
        return! ctx.WriteHtmlViewAsync (Templates.index model)
    }

let login (user: UserData option): HttpHandler = fun _ ctx ->
    task {
        let loginModel, _ = Client.Login.init user
        let model: Model = {
            User = user
            PageModel = PageModel.LoginModel loginModel
        }
        return! ctx.WriteHtmlViewAsync (Templates.index model)
    }

let wishList (user: UserData): HttpHandler = fun _ ctx ->
    task {
        let wishList, _ = Client.WishList.init user
        let model: Model = {
            User = Some user
            PageModel = PageModel.WishListModel wishList
        }
        return! ctx.WriteHtmlViewAsync (Templates.index model)
    }
