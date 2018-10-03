module Client.Shared

open ServerCode.Domain
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

/// The composed model for the different possible page states of the application
type PageModel =
    | HomePageModel
    | LoginModel of Login.Model
    | WishListModel of WishList.Model

    static member Encoder (pageModel : PageModel) =
        match pageModel with
        | HomePageModel ->
            Encode.object [
                "homePageModel", Encode.nil
            ]
        | LoginModel subModel ->
            Encode.object [
                "loginModel", Login.Model.Encoder subModel
            ]
        | WishListModel subModel ->
            Encode.object [
                "wishListModel", WishList.Model.Encoder subModel
            ]

    static member Decoder =
        Decode.oneOf [
            Decode.field "homePageModel" (Decode.succeed HomePageModel)

            Decode.field "loginModel" Login.Model.Decoder
                |> Decode.map LoginModel

            Decode.field "wishListModel" WishList.Model.Decoder
                |> Decode.map WishListModel
        ]

/// The composed model for the application, which is a single page state plus login information
type Model =
    { User : UserData option
      PageModel : PageModel }

    static member Encoder (model : Model) =
        Encode.object [
            "user", Encode.option UserData.Encoder model.User
            "pageModel", PageModel.Encoder model.PageModel
        ]

    static member Decoder =
        Decode.object (fun get ->
            { User = get.Optional.Field "user" UserData.Decoder
              PageModel = get.Required.Field "pageModel" PageModel.Decoder }
        )

/// The composed set of messages that update the state of the application
type Msg =
    | LoggedIn of UserData
    | LoggedOut
    | StorageFailure of exn
    | LoginMsg of Login.Msg
    | WishListMsg of WishList.Msg
    | Logout of unit


// VIEW

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Client.Style

/// Constructs the view for a page given the model and dispatcher.
let viewPage model dispatch =
    match model.PageModel with
    | HomePageModel ->
        Home.view ()

    | LoginModel m ->
        [ Login.view m (LoginMsg >> dispatch) ]

    | WishListModel m ->
        [ WishList.view m (WishListMsg >> dispatch) ]

/// Constructs the view for the application given the model.
let view model dispatch =
    div [] [
        Menu.view (Logout >> dispatch) model.User
        hr []
        div [ centerStyle "column" ] (viewPage model dispatch)
    ]
