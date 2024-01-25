namespace Shared

open System

type JWT = string

// Login credentials.
type Login =
    { UserName   : string
      Password   : string }

    member this.IsValid() =
        not ((this.UserName <> "test"  || this.Password <> "test") &&
             (this.UserName <> "test2" || this.Password <> "test2"))
type UserName =
    | UserName of string

    member this.Value =
        match this with
        | UserName v -> v

type UserData =
  { UserName : UserName
    Token : JWT }

type Book = {
  Title : string
  Authors : string
  Link : string
  ImageLink : string
}

type WishList =
    { UserName : UserName
      Books : Book list }

module Route =
    let builder typeName methodName =
        $"/api/%s{typeName}/%s{methodName}"

type IBooksApi = {
    getBooks: unit -> Async<Book seq>
    getWishlist: UserName -> Async<WishList>
    removeBook: UserName * string -> Async<string>
}

type IUserApi = {
    login: Login -> Async<UserData>
}