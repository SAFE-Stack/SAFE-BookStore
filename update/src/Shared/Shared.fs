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

type UserData =
  { UserName : string
    Token : JWT }

type Book = {
  Title : string
  Authors : string
  Link : string
  ImageLink : string
}

module Route =
    let builder typeName methodName =
        $"/api/%s{typeName}/%s{methodName}"

type IBooksApi = {
    getWishlist: unit -> Async<Book seq>
}

type IUserApi = {
    login: Login -> Async<UserData>
}