module ServerCode.Templates

open ServerCode.FableJson
open Client.Shared
open Fable.Helpers.ReactServer
open Fable.Helpers.React
open Fable.Helpers.React.Props

let index (model: Model option) =
  let jsonState, htmlStr =
    match model with
    | Some model ->
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
