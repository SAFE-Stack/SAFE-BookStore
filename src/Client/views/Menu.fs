module Client.Menu

open Fable.React
open Fable.React.Props
open Client.Styles
open Client.Pages
open Client.Utils
open ServerCode.Domain

type Model = {
    User : UserData option
    RenderedOnServer : bool
}

type Props = {
    Model : Model
    OnLogout: unit -> unit
}

let view = elmishView "Menu" (fun ({ OnLogout = onLogout; Model = model }) ->
    div [ centerStyle "row" ] [
        yield viewLink Page.Home "Home"
        match model.User with
        | Some _ ->
            yield viewLink Page.WishList "Wishlist"
            yield menuLink onLogout "Logout"
        | _ ->
            yield viewLink Page.Login "Login"

        if model.RenderedOnServer then
            yield img [ Key "RenderSource"; Src "/Images/baseline_check_box_outline_blank_black_24dp.png"; Title "Rendered on server";  ]
        else
            yield img [ Key "RenderSource"; Src "/Images/baseline_check_box_black_24dp.png"; Title "Rendered in browser" ]
    ]
)