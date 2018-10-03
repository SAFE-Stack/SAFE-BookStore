/// Domain model shared between client and server.
namespace ServerCode.Domain

open System
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

// Json web token type.
type JWT = string

// Login credentials.
type Login =
    { UserName   : string
      Password   : string
      PasswordId : Guid }

    member this.IsValid() =
        not ((this.UserName <> "test"  || this.Password <> "test") &&
             (this.UserName <> "test2" || this.Password <> "test2"))

    static member Encoder (login : Login) =
        Encode.object [
            "username", Encode.string login.UserName
            "password", Encode.string login.Password
            "passwordId", Encode.guid login.PasswordId
        ]

    static member Decoder =
        Decode.object (fun get ->
            { UserName = get.Required.Field "username" Decode.string
              Password = get.Required.Field "password" Decode.string
              PasswordId = get.Required.Field "passwordId" Decode.guid }
        )

type UserData =
    { UserName : string
      Token    : JWT }

    static member Encoder (userData : UserData) =
        Encode.object [
            "username", Encode.string userData.UserName
            "token", Encode.string userData.Token
        ]

    static member Decoder =
        Decode.object (fun get ->
            { UserName = get.Required.Field "username" Decode.string
              Token = get.Required.Field "token" Decode.string }
        )

/// The data for each book in /api/wishlist
type Book =
    { Title: string
      Authors: string
      Link: string }

    static member empty =
        { Title = ""
          Authors = ""
          Link = "" }

    static member Encoder (book : Book) =
        Encode.object [
            "title", Encode.string book.Title
            "authors", Encode.string book.Authors
            "link", Encode.string book.Link
        ]

    static member Decoder =
        Decode.object (fun get ->
            { Title = get.Required.Field "title" Decode.string
              Authors = get.Required.Field "authors" Decode.string
              Link = get.Required.Field "link" Decode.string }
        )

/// The logical representation of the data for /api/wishlist
type WishList =
    { UserName : string
      Books : Book list }

    // Create a new WishList.  This is supported in client code too,
    // thanks to the magic of https://www.nuget.org/packages/Fable.JsonConverter
    static member New userName =
        { UserName = userName
          Books = [] }

    static member Encoder (wishList : WishList) =
        Encode.object [
            "username", Encode.string wishList.UserName
            "books", wishList.Books |> List.map Book.Encoder |> Encode.list
        ]

    static member Decoder =
        Decode.object (fun get ->
            { UserName = get.Required.Field "username" Decode.string
              Books = get.Required.Field "books" (Decode.list Book.Decoder) }
        )

type WishListResetDetails =
    { Time : DateTime }

    static member Encoder (details :  WishListResetDetails) =
        Encode.object [
            "time", Encode.datetime details.Time
        ]

    static member Decoder =
        Decode.object (fun get ->
            { Time = get.Required.Field "time" Decode.datetime }
        )

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

    let verifyBookisNotADuplicate (wishList:WishList) book =
        // only compare author and title; ignore url because it is not directly user-visible
        if wishList.Books |> Seq.exists (fun b -> (b.Authors,b.Title) = (book.Authors,book.Title)) then
            Some "Your wishlist contains this book already."
        else
            None

    let verifyBook book =
        verifyBookTitle book.Title = None &&
        verifyBookAuthors book.Authors = None &&
        verifyBookLink book.Link = None

    let verifyWishList wishList =
        wishList.Books |> List.forall verifyBook
