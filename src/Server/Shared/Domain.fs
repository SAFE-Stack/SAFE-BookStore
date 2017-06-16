/// Domain model shared between client and server.
namespace ServerCode.Domain
   
open System

// Json web token type.
type JWT = string

// Login credentials.
type Login = 
    { UserName : string
      Password : string }

/// The data for each book in /api/wishlist
type Book = 
    { Title: string
      Authors: string
      Link: string }

    static member empty = 
        { Title = ""
          Authors = ""
          Link = "" }

/// The logical representation of the data for /api/wishlist
type WishList = 
    { UserName : string
      Books : Book list }

    // Create a new WishList.  This is supported in client code too,
    // thanks to the magic of https://www.nuget.org/packages/Fable.JsonConverter
    static member New userName = 
        { UserName = userName
          Books = [] }


// Model validation functions.  Write your validation functions once, for server and client!
module Validation =

    let verifyBookTitle title =
        if String.IsNullOrWhiteSpace title then Some "No title was entered" else
        None

    let verifyBookAuthors authors =
        if String.IsNullOrWhiteSpace authors then Some "No author was entered" else
        None

    let verifyBookLink link =
        if String.IsNullOrWhiteSpace link then Some "No link was entered" else
        None

    let verifyBook book =
        verifyBookTitle book.Title = None &&
        verifyBookAuthors book.Authors = None &&
        verifyBookLink book.Link = None

    let verifyWishList wishList =
        wishList.Books |> List.forall verifyBook