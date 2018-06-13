/// API urls shared between client and server.
module ServerCode.ServerUrls

[<RequireQualifiedAccess>]
module PageUrls =
  [<Literal>]
  let Home = "/"

[<RequireQualifiedAccess>]
module APIUrls =

  [<Literal>]
  let Socket = "/api/socket"
  (*
  [<Literal>]
  let WishList = "/api/wishlist/"
  [<Literal>]
  let ResetTime = "/api/wishlist/resetTime/"
  [<Literal>]
  let Login = "/api/users/login/"
  *)