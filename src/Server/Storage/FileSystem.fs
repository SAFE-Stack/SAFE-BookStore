module ServerCode.Storage.FileSystem

open System.IO
open ServerCode
open ServerCode.Domain

/// Get the file name used to store the data for a specific user
let getJSONFileName userName = sprintf "./temp/db/%s.json" userName

let getWishListFromDB userName =
    let fi = FileInfo(getJSONFileName userName)
    if not fi.Exists then Defaults.defaultWishList userName
    else
        File.ReadAllText(fi.FullName)
        |> FableJson.ofJson<WishList>

let saveWishListToDB wishList =
    let fi = FileInfo(getJSONFileName wishList.UserName)
    if not fi.Directory.Exists then
        fi.Directory.Create()
    File.WriteAllText(fi.FullName, FableJson.toJson wishList)