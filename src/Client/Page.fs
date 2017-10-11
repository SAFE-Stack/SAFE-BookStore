module Client.Pages


open Elmish.Browser.UrlParser

/// The different pages of the application. If you add a new page, then add an entry here.
[<RequireQualifiedAccess>]
type Page = 
| Home 
| Login
| WishList

let toHash =
    function
    | Page.Home -> "#home"
    | Page.Login -> "#login"
    | Page.WishList -> "#wishlist"


/// The URL is turned into a Result.
let pageParser : Parser<Page -> Page,_> =
    oneOf
        [ map Page.Home (s "home")
          map Page.Login (s "login")
          map Page.WishList (s "wishlist") ]

let urlParser location = parseHash pageParser location