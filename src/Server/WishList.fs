module ServerCode.WishList

open System.IO
open Suave
open Suave.Logging
open System.Net
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open System
open Suave.ServerErrors
open ServerCode.Domain
open Suave.Logging
open Suave.Logging.Message

let logger = Log.create "FableSample"


/// The default initial data 
let defaultWishList userName : WishList =
    {
        UserName = userName
        Books = 
            [{ Title = "Mastering F#"
               Authors = "Alfonso Garcia-Caro Nunez"
               Link = "https://www.amazon.com/Mastering-F-Alfonso-Garcia-Caro-Nunez-ebook/dp/B01M112LR9" }
             { Title = "Learn F#"
               Authors = "Isaac Abraham"
               Link = "https://www.manning.com/books/learn-fsharp" }]
    }

/// Get the file name used to store the data for a specific user
let getJSONFileName userName = sprintf "./temp/db/%s.json" userName

/// Query the database
let getWishListFromDB userName =
    let fi = FileInfo(getJSONFileName userName)
    if not fi.Exists then
        defaultWishList userName
    else
        File.ReadAllText(fi.FullName)
        |> JsonUtils.ofJson<WishList>

/// Save to the database
let saveWishListToDB (wishList:WishList) =
    try
        let fi = FileInfo(getJSONFileName wishList.UserName)
        if not fi.Directory.Exists then
            fi.Directory.Create()
        File.WriteAllText(fi.FullName, JsonUtils.toJson wishList)
    with exn ->
        logger.error (eventX "Save failed with exception" >> addExn exn)

/// Handle the GET on /api/wishlist
let getWishList (ctx: HttpContext) =
    Auth.useToken ctx (fun token -> async {
        try
            let wishList = getWishListFromDB token.UserName
            return! Successful.OK (JsonUtils.toJson wishList) ctx
        with exn ->
            logger.error (eventX "SERVICE_UNAVAILABLE" >> addExn exn)
            return! SERVICE_UNAVAILABLE "Database not available" ctx
    })

/// Handle the POST on /api/wishlist
let postWishList (ctx: HttpContext) =
    Auth.useToken ctx (fun token -> async {
        try
            let wishList = 
                ctx.request.rawForm
                |> System.Text.Encoding.UTF8.GetString
                |> JsonUtils.ofJson<Domain.WishList>
            
            if token.UserName <> wishList.UserName then
                return! UNAUTHORIZED (sprintf "WishList is not matching user %s" token.UserName) ctx
            else
                if Validation.verifyWishList wishList then
                    saveWishListToDB wishList
                    return! Successful.OK (JsonUtils.toJson wishList) ctx
                else
                    return! BAD_REQUEST "WishList is not valid" ctx
        with exn ->
            logger.error (eventX "Database not available" >> addExn exn)
            return! SERVICE_UNAVAILABLE "Database not available" ctx
    })    