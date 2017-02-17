module ServerCode.WishList

open System.IO
open Suave
open Suave.Logging
open Newtonsoft.Json
open System.Net
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open System
open Suave.ServerErrors

let getWishList (ctx: HttpContext) =
    Auth.useToken ctx (fun token -> async {
        try
            let wishList :Domain.WishList =
                {
                    UserName = token.UserName
                    Books = 
                        [{ Title = "Mastering F#"
                           Authors = ["Alfonso Garcia-Caro Nunez"]
                           Link = "https://www.amazon.com/Mastering-F-Alfonso-Garcia-Caro-Nunez-ebook/dp/B01M112LR9" }
                         { Title = "Learn F#"
                           Authors = ["Isaac Abraham"]
                           Link = "https://www.manning.com/books/learn-fsharp" }]
                }
            return! Successful.OK (JsonConvert.SerializeObject wishList) ctx
        with
        | _ -> return! SERVICE_UNAVAILABLE "Database not available" ctx
    })