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
        |> JsonConvert.DeserializeObject<WishList>

/// Save to the database
let saveWishListToDB (wishList:WishList) =
    try
        let fi = FileInfo(getJSONFileName wishList.UserName)
        if not fi.Directory.Exists then
            fi.Directory.Create()
        File.WriteAllText(fi.FullName,  JsonConvert.SerializeObject wishList)
    with exn ->
        logger.error (eventX "Save failed with exception" >> addExn exn)


let getWishList authToken = 
    match JsonWebToken.isValid authToken with
    | None -> Response.Error UserNotLoggedIn
    | Some userRights ->
        let userName = userRights.UserName
        getWishListFromDB userName
        |> Success
    |> Async.result


let createWishList (input: AuthorizedRequest<WishList>) =
    match JsonWebToken.isValid input.AuthToken with
    | None -> Response.Error UserNotLoggedIn
    | Some user ->
        let wishList = input.Request
        if user.UserName <> wishList.UserName then
            Response.Error UserUnauthorized
        else
            match Validation.verifyWishList wishList with
            | false -> Response.Error RequestInvalid
            | true -> 
                saveWishListToDB wishList
                Success wishList
    |> Async.result