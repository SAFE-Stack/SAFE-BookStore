/// API urls shared between client and server.
module ServerCode.ServerUrls

[<RequireQualifiedAccess>]
module PageUrls =
  [<Literal>]
  let Home = "/"
  [<Literal>]
  let Login = "/login"

[<RequireQualifiedAccess>]
module APIUrls =

  let WishList userName = sprintf "/api/wishlist/%s" userName

  [<Literal>]
  let ResetTime = "/api/wishlist/resetTime/"
  [<Literal>]
  let Login = "/api/users/login/"
