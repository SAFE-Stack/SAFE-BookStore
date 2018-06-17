/// API urls shared between client and server.
module ServerCode.ServerUrls

[<RequireQualifiedAccess>]
module PageUrls =
  [<Literal>]
  let Home = "/"

[<RequireQualifiedAccess>]
module APIUrls =

  [<Literal>]
  let Socket = "/socket"