module Client.Login

open Elmish
open Fable.Helpers.React
open Fable.Helpers.React.Props
open ServerCode.Domain
open System
open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.PowerPack.Fetch.Fetch_types
open ServerCode
open Client.Styles
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type Model = {
    Login : Login
    Running : bool
    ErrorMsg : string option }

/// The messages processed during login
type Msg =
    | LoginSuccess of UserData
    | SetUserName of string
    | SetPassword of string
    | AuthError of exn
    | ClickLogIn


let authUser (login:Login) =
    promise {
        if String.IsNullOrEmpty login.UserName then return! failwithf "You need to fill in a username." else
        if String.IsNullOrEmpty login.Password then return! failwithf "You need to fill in a password." else

        let body = Encode.Auto.toString(0, login)

        let props =
            [ RequestProperties.Method HttpMethod.POST
              Fetch.requestHeaders [
                  HttpRequestHeaders.ContentType "application/json" ]
              RequestProperties.Body !^body ]

        try
            let! res = Fetch.fetch ServerUrls.APIUrls.Login props
            let! txt = res.text()
            return Decode.Auto.unsafeFromString<UserData> txt
        with _ ->
            return! failwithf "Could not authenticate user."
    }


let init (user:UserData option) =
    let userName = user |> Option.map (fun u -> u.UserName) |> Option.defaultValue ""
            
    { Login = { UserName = userName; Password = ""; PasswordId = Guid.NewGuid() }
      Running = false
      ErrorMsg = None }, Cmd.none
    
let update (msg:Msg) model : Model*Cmd<Msg> =
    match msg with
    | LoginSuccess _ ->
        model, Cmd.none // DEMO06 - some messages are handled one level above

    | SetUserName name ->
        { model with Login = { model.Login with UserName = name; Password = ""; PasswordId = Guid.NewGuid() } }, Cmd.none

    | SetPassword pw ->
        { model with Login = { model.Login with Password = pw }}, Cmd.none

    | ClickLogIn ->
        { model with Running = true }, Cmd.ofPromise authUser model.Login LoginSuccess AuthError

    | AuthError exn ->
        { model with Running = false; ErrorMsg = Some exn.Message }, Cmd.none

let view model (dispatch: Msg -> unit) =
    let buttonActive = 
        if String.IsNullOrEmpty model.Login.UserName || 
           String.IsNullOrEmpty model.Login.Password ||
           model.Running
        then 
            "btn-disabled"
        else
            "btn-primary"
    
    div [ Key "SignIn"; ClassName "signInBox" ] [
        h3 [ ClassName "text-center" ] [ str "Log in with 'test' / 'test'."]

        Styles.errorBox model.ErrorMsg

        div [ ClassName "input-group input-group-lg" ] [
            span [ClassName "input-group-addon" ] [
                span [ClassName "glyphicon glyphicon-user"] []
            ]
            input [
                Id "username"
                HTMLAttr.Type "text"
                ClassName "form-control input-lg"
                Placeholder "Username"
                DefaultValue model.Login.UserName
                OnChange (fun ev -> dispatch (SetUserName ev.Value))
                AutoFocus true
            ]
        ]

        div [ ClassName "input-group input-group-lg" ] [
            span [ClassName "input-group-addon" ] [
                span [ClassName "glyphicon glyphicon-asterisk"] []
            ]
            input [
                Id "password"
                Key ("password_" + model.Login.PasswordId.ToString())
                HTMLAttr.Type "password"
                ClassName "form-control input-lg"
                Placeholder "Password"
                DefaultValue model.Login.Password
                OnChange (fun ev -> dispatch (SetPassword ev.Value))
                onEnter ClickLogIn dispatch
            ]
        ]

        div [ ClassName "text-center" ] [
            button [ ClassName ("btn " + buttonActive);
                     OnClick (fun _ -> dispatch ClickLogIn) ]
                   [ str "Log In" ]
        ]
    ]
