module ServerCode.Storage.FileSystem

open System.IO
open ServerCode
open ServerCode.Domain

/// The default initial data 
let defaultWishList userName =
    { UserName = userName
      Books = 
        [ { Title = "Mastering F#"
            Authors = "Alfonso Garcia-Caro Nunez"
            Link = "https://www.amazon.com/Mastering-F-Alfonso-Garcia-Caro-Nunez-ebook/dp/B01M112LR9" }
          { Title = "Get Programming with F#"
            Authors = "Isaac Abraham"
            Link = "https://www.manning.com/books/get-programming-with-f-sharp" } ] }

/// Get the file name used to store the data for a specific user
let getJSONFileName userName = sprintf "./temp/db/%s.json" userName

let getWishListFromDB userName =
    let fi = FileInfo(getJSONFileName userName)
    if not fi.Exists then defaultWishList userName
    else
        File.ReadAllText(fi.FullName)
        |> FableJson.ofJson<WishList>

let saveWishListToDB wishList =
    let fi = FileInfo(getJSONFileName wishList.UserName)
    if not fi.Directory.Exists then
        fi.Directory.Create()
    File.WriteAllText(fi.FullName, FableJson.toJson wishList)
