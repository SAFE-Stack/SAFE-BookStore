module Client.Login

open Fable.Core
open Fable.Import
open Elmish
open Fable.Helpers.React
open Fable.Helpers.React.Props
open ServerCode.Domain
open Style
open Messages
open System
open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.PowerPack.Fetch.Fetch_types

    
type LoginState =
| LoggedOut
| LoggedIn of JWT

type Model = { 
    State : LoginState
    Login : Login
    ErrorMsg : string }

let authUser (login:Login,apiUrl) =
    promise {
        if String.IsNullOrEmpty login.UserName then return! failwithf "You need to fill in a username." else
        if String.IsNullOrEmpty login.Password then return! failwithf "You need to fill in a password." else

        let body = toJson login

        let props = 
            [ RequestProperties.Method HttpMethod.POST
              Fetch.requestHeaders [
                HttpRequestHeaders.ContentType "application/json" ]
              RequestProperties.Body !^body ]
        
        try

            let! response = Fetch.fetch apiUrl props

            if not response.Ok then
                return! failwithf "Error: %d" response.Status
            else    
                let! data = response.text() 
                return data
        with
        | _ -> return! failwithf "Could not authenticate user."
    }

let authUserCmd login apiUrl = 
    Cmd.ofPromise authUser (login,apiUrl) GetTokenSuccess AuthError

let init (user:UserData option) = 
    match user with
    | None ->
        { Login = { UserName = ""; Password = ""}
          State = LoggedOut
          ErrorMsg = "" }, Cmd.none
    | Some user ->
        { Login = { UserName = user.UserName; Password = ""}
          State = LoggedIn user.Token
          ErrorMsg = "" }, Cmd.none

let update (msg:LoginMsg) model : Model*Cmd<LoginMsg> = 
    match msg with
    | LoginMsg.GetTokenSuccess token ->
        { model with State = LoggedIn token;  Login = { model.Login with Password = "" } }, []
    | LoginMsg.SetUserName name ->
        { model with Login = { model.Login with UserName = name; Password = "" }}, []
    | LoginMsg.SetPassword pw ->
        { model with Login = { model.Login with Password = pw }}, []
    | LoginMsg.ClickLogIn ->
        model, authUserCmd model.Login "/api/users/login"
    | LoginMsg.AuthError exn ->
        { model with ErrorMsg = string (exn.Message) }, []

let [<Literal>] ENTER_KEY = 13.

let view model (dispatch: AppMsg -> unit) = 
    let showErrorClass = if String.IsNullOrEmpty model.ErrorMsg then "hidden" else ""
    let buttonActive = if String.IsNullOrEmpty model.Login.UserName || String.IsNullOrEmpty model.Login.Password then "btn-disabled" else "btn-primary"

    let onEnter msg dispatch =
        function 
        | (ev:React.KeyboardEvent) when ev.keyCode = ENTER_KEY ->
            ev.preventDefault() 
            dispatch msg
        | _ -> ()
        |> OnKeyDown
        
    match model.State with
    | LoggedIn _ ->
        div [Id "greeting"] [
          h3 [ ClassName "text-center" ] [ str (sprintf "Hi %s!" model.Login.UserName) ]
        ]

    | LoggedOut ->
        div [ClassName "signInBox" ] [
          h3 [ ClassName "text-center" ] [ str "Log in with 'test' / 'test'."]
           
          div [ ClassName showErrorClass ] [
                  div [ ClassName "alert alert-danger" ] [ str model.ErrorMsg ]
           ]

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
                    OnChange (fun ev -> dispatch (LoginMsg (SetUserName !!ev.target?value)))
                    AutoFocus true ]
          ]

          div [ ClassName "input-group input-group-lg" ] [
                span [ClassName "input-group-addon" ] [
                  span [ClassName "glyphicon glyphicon-asterisk"] []
                ]
                input [ 
                        Id "password"
                        HTMLAttr.Type "password"
                        ClassName "form-control input-lg"
                        Placeholder "Password"
                        DefaultValue model.Login.Password
                        OnChange (fun ev -> dispatch (LoginMsg (SetPassword !!ev.target?value)))
                        onEnter (LoginMsg ClickLogIn) dispatch  ]
            ]    
           
          div [ ClassName "text-center" ] [
              button [ ClassName ("btn " + buttonActive); OnClick (fun _ -> dispatch (LoginMsg ClickLogIn)) ] [ str "Log In" ]
          ]                   
        ]    
 