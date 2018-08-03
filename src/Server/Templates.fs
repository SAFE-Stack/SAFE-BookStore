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
open Thoth.Json.Net

let index (model: Model option) =
  let jsonState, htmlStr =
    match model with
    | Some model ->
        // Note we call ofJson twice here,
        // because Elmish's model can be some complicated type instead of pojo.
        // The first one will seriallize the state to a json string,
        // and the second one will seriallize the json string to a js string,
        // so we can deseriallize it by Fable's ofJson and get the correct types.
        Encode.Auto.toString(0, (Encode.Auto.toString(0, model))),
        Client.Shared.view model ignore |> renderToString
    | None ->
        "null", ""
  html []
    [ head [] [
        meta [ _httpEquiv "Content-Type"; _content "text/html"; _charset "utf-8" ]
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
        link [ _rel "shortcut icon"; _type "image/png"; _href "/Images/safe_favicon.png" ]
      ]
      body [ _class "app-container" ] [
        div [ _id "elmish-app"; _class "elmish-app" ] [
          rawText htmlStr
        ]
        script [ ] [ rawText (sprintf "var __INIT_MODEL__ = %s" jsonState) ]
        script [ _src "/public/vendors.js" ] []
        script [ _src "/public/main.js" ] []
      ]
    ]
