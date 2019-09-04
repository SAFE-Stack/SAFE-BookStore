module Client.Pages

open Elmish.UrlParser

/// The different pages of the application. If you add a new page, then add an entry here.
[<RequireQualifiedAccess>]
type Page =
    | Home
    | NotFound
    | Login
    | WishList

let toPath =
    function
    | Page.Home -> "/"
    | Page.Login -> "/login"
    | Page.NotFound -> "/notfound"
    | Page.WishList -> "/wishlist"

/// The URL is turned into a Result.
let pageParser : Parser<Page -> Page,_> =
    oneOf
        [ map Page.Home (s "")
          map Page.Login (s "login")
          map Page.NotFound (s "notfound")
          map Page.WishList (s "wishlist") ]

let urlParser location = parsePath pageParser location
