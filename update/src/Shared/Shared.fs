namespace Shared

open System

type Book = {
  Title : string
  Authors : string
  Link : string
  ImageLink : string
}

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

type IBooksApi = {
    getWishlist: unit -> Async<Book seq>
}