module ServerCode.Templates

open Client.Shared
open Giraffe.ViewEngine
open Fable.ReactServer
open Thoth.Json.Net

/// Server side react
let index (model: Model) =
    html [_lang "en-US" ] [
        head [ ] [
            meta [ _httpEquiv "Content-Type"; _content "text/html; charset=UTF-8" ]
            title [] [ rawText "SAFE-Stack Bookstore" ]
            link [
                _rel "stylesheet"
                _href "https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css"
                attr "integrity" "sha384-BVYiiSIFeK1dGmJRAkycuHAHRg32OmUcww7on3RYdg4Va+PmSTsz/K68vbdEjh4u"
                _crossorigin "anonymous"
            ]
            link [
                _rel "stylesheet"
                _href "https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap-theme.min.css"
                attr "integrity" "sha384-rHyoN1iRsVXV4nD0JutlnGaslCJuC7uwjduW9SVrLvRYooPp2bWYgmgJQIXwl/Sp"
                _crossorigin "anonymous"
            ]
            link [ _rel "stylesheet"; _href "css/site.css" ]
            link [ _rel "shortcut icon"; _type "image/png"; _href "/Images/safe_favicon.png" ]
        ]
        body [ _class "app-container" ] [
            div [ _id "elmish-app"; _class "elmish-app" ] [
                Client.Shared.view model ignore
                |> renderToString |> rawText
            ]
            script [ ] [
                // Note we call json serialization twice here,
                // because Elmish's model can be some complicated type instead of POJO.
                // The first one will seriallize the state to a json string,
                // and the second one will seriallize the json string to a js string,
                // so we can deseriallize it by Thoth auto decoder and get the correct types.
                "var __INIT_MODEL__ = " +
                  Encode.Auto.toString(0, (Encode.Auto.toString(0, model)))
                |> rawText
            ]
            script [ _src "/public/vendors.js" ] []
            script [ _src "/public/main.js" ] []
        ]
    ]
