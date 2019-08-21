module Client.Home

open Fable.React
open Fable.React.Props
open Client.Styles
open Client.Pages
open Client.Utils

type HomeModel = {
    Version : string
}

let view = elmishView "Home" (fun (model:HomeModel) ->
    div [Key "Home"; centerStyle "column"] [
        viewLink Page.Login "Please login into the SAFE-Stack sample app"
        br []
        br []
        br []
        br []
        words 20 "Made with"
        br []
        a [ Href "https://safe-stack.github.io/" ] [ img [ Src "/Images/safe_logo.png" ] ]
        br []
        br []
        words 15 "An end-to-end, functional-first stack for cloud-ready web development that emphasises type-safe programming."
        br []
        br []
        words 20 ("version " + model.Version)
    ]
)