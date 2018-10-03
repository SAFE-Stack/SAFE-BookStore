module ServerCode.Storage.FileSystem

open System.IO
open ServerCode
open ServerCode.Domain
open Thoth.Json.Net

/// Get the file name used to store the data for a specific user
let getJSONFileName userName = sprintf "./temp/db/%s.json" userName

let getWishListFromDB userName =
    let fi = FileInfo(getJSONFileName userName)
    if not fi.Exists then Defaults.defaultWishList userName
    else
        let txt = File.ReadAllText(fi.FullName)
        match Decode.fromString WishList.Decoder txt with
        | Ok wishList -> wishList
        | Error msg -> failwith msg

let saveWishListToDB wishList =
    let fi = FileInfo(getJSONFileName wishList.UserName)
    if not fi.Directory.Exists then
        fi.Directory.Create()
    let json = WishList.Encoder wishList
                |> Encode.toString 4
    File.WriteAllText(fi.FullName,  json)
