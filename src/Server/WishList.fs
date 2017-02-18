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

let defaultWishList userName : Domain.WishList =
    {
        UserName = userName
        Books = 
            [{ Title = "Mastering F#"
               Authors = ["Alfonso Garcia-Caro Nunez"]
               Link = "https://www.amazon.com/Mastering-F-Alfonso-Garcia-Caro-Nunez-ebook/dp/B01M112LR9" }
             { Title = "Learn F#"
               Authors = ["Isaac Abraham"]
               Link = "https://www.manning.com/books/learn-fsharp" }]
    }

let getJSONFileName userName = sprintf "./temp/db/%s.json" userName

let getWishListFromDB userName =
    let fi = FileInfo(getJSONFileName userName)
    if not fi.Exists then
        defaultWishList userName
    else
        File.ReadAllText(fi.FullName)
        |> JsonConvert.DeserializeObject<Domain.WishList>

let saveWishListToDB (wishList:Domain.WishList) =
    try
        let fi = FileInfo(getJSONFileName wishList.UserName)
        if not fi.Directory.Exists then
            fi.Directory.Create()
        File.WriteAllText(fi.FullName,JsonConvert.SerializeObject wishList)
    with
    | _ -> ()


let getWishList (ctx: HttpContext) =
    Auth.useToken ctx (fun token -> async {
        try
            let wishList = getWishListFromDB token.UserName
            return! Successful.OK (JsonConvert.SerializeObject wishList) ctx
        with
        | _ -> return! SERVICE_UNAVAILABLE "Database not available" ctx
    })

let postWishList (ctx: HttpContext) =
    Auth.useToken ctx (fun token -> async {
        try
            let wishList = 
                ctx.request.rawForm
                |> System.Text.Encoding.UTF8.GetString
                |> JsonConvert.DeserializeObject<Domain.WishList>
            
            if token.UserName <> wishList.UserName then
                return! UNAUTHORIZED (sprintf "WishList is not matching user %s" token.UserName) ctx
            else
                saveWishListToDB wishList
                return! Successful.OK (JsonConvert.SerializeObject wishList) ctx
        with
        | _ -> return! SERVICE_UNAVAILABLE "Database not available" ctx
    })    