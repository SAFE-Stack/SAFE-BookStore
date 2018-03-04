module ServerCode.Templates



open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Giraffe
open ServerCode.Domain
open ServerCode.FableJson
open ServerTypes
open Client.Shared
open Giraffe.GiraffeViewEngine
open Fable.Helpers.ReactServer

#if DEBUG
let assetsBaseUrl = "http://localhost:8080"
#else
let assetsBaseUrl = ""
#endif

let index (model: Model) =
  let jsonState = toJson (toJson model)
  let htmlStr = Client.Shared.view model ignore |> renderToString
  html []
    [ head [] [ meta [ _httpEquiv "Content-Type"; _content "text/html"; _charset "utf-8" ] ]
      title [] [ rawText "SAFE-Stack sample" ]
      link
        [ _rel "stylesheet"
          _href "https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css";
          attr "integrity" "sha384-BVYiiSIFeK1dGmJRAkycuHAHRg32OmUcww7on3RYdg4Va+PmSTsz/K68vbdEjh4u"
          _crossorigin "anonymous"
        ]
      link
        [ _rel "stylesheet"
          _href "https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap-theme.min.css"
          attr "integrity" "sha384-rHyoN1iRsVXV4nD0JutlnGaslCJuC7uwjduW9SVrLvRYooPp2bWYgmgJQIXwl/Sp"
          _crossorigin "anonymous" ]
      link [ _rel "stylesheet"; _href "css/site.css" ]
      link [ _rel "shortcut icon"; _type "image/png"; _href "/images/safe_favicon.png" ]
      body [ _class "app-container" ] [
        div [ _id "elmish-app"; _class "elmish-app" ] [
          rawText htmlStr
        ]
        script [ ] [ rawText (sprintf "var __INIT_MODEL__ = %s" jsonState) ]
        script [ _src (assetsBaseUrl + "/public/bundle.js") ] []
      ]
    ]
