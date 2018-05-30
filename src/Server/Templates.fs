module ServerCode.Templates



open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open ServerCode.Domain
open ServerCode.FableJson
open ServerTypes
open Client.Shared
open Fable.Helpers.ReactServer
open Fable.Helpers.React
open Fable.Helpers.React.Props

let index (model: Model option) =
  let jsonState, htmlStr =
    match model with
    | Some model ->
        // Note we call ofJson twice here,
        // because Elmish's model can be some complicated type instead of pojo.
        // The first one will seriallize the state to a json string,
        // and the second one will seriallize the json string to a js string,
        // so we can deseriallize it by Fable's ofJson and get the correct types.
        toJson model,
        Client.Shared.view model ignore |> renderToString
    | None ->
        "null", ""
  html []
    [ head [] [ 
        meta [ HttpEquiv "Content-Type"; Props.Content "text/html"; CharSet "utf-8" ]
        title [] [ str "SAFE-Stack sample" ]
        link
          [ Rel "stylesheet"
            Href "https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css";
            Integrity "sha384-BVYiiSIFeK1dGmJRAkycuHAHRg32OmUcww7on3RYdg4Va+PmSTsz/K68vbdEjh4u"
            CrossOrigin "anonymous"
          ]
        link
          [ Rel "stylesheet"
            Href "https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap-theme.min.css"
            Integrity "sha384-rHyoN1iRsVXV4nD0JutlnGaslCJuC7uwjduW9SVrLvRYooPp2bWYgmgJQIXwl/Sp"
            CrossOrigin "anonymous" ]
        link [ Rel "stylesheet"; Href "css/site.css" ]
        link [ Rel "shortcut icon"; Type "image/png"; Href "/Images/safe_favicon.png" ]
      ]
      body [ ClassName "app-container" ] [
        div [ Id "elmish-app"; ClassName "elmish-app"; DangerouslySetInnerHTML { __html = htmlStr} ] []
        script [ DangerouslySetInnerHTML { __html = (sprintf "var __INIT_MODEL__ = %s" jsonState) } ] [ ]
        script [ Src "/public/bundle.js" ] []
      ]
    ]
