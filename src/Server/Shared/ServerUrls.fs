/// API urls shared between client and server.
module ServerCode.ServerUrls

[<RequireQualifiedAccess>]
module PageUrls =
  [<Literal>]
  let Home = "/"
  [<Literal>]
  let WishList = "/wishlist"
  [<Literal>]
  let Login = "/login"

[<RequireQualifiedAccess>]
module APIUrls =

  [<Literal>]
  let WishList = "/api/wishlist/"
  [<Literal>]
  let ResetTime = "/api/wishlist/resetTime/"
  [<Literal>]
  let Login = "/api/users/login/"
