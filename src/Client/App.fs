module Client.App

open Fable.Core
open Fable.Core.JsInterop

open Fable.Core
open Fable.Import
open Elmish
open Fable.Import.Browser
open Fable.PowerPack
open Elmish.Browser.Navigation
open Client.Messages
open Elmish.UrlParser

// Types
type SubModel =
| NoSubModel
| LoginModel of Login.Model

type Model =
  { Page : Page
    Menu : Menu.Model
    cache : Map<string,string list>
    SubModel : SubModel }


/// The URL is turned into a Result.
let pageParser : Parser<Page->_,_> =
    oneOf 
        [ format Home (s "home")
          format Page.Login (s "login")
          format Blog (s "blog" </> i32)
          format Search (s "search" </> str) ]

let hashParser (location:Location) =
    UrlParser.parse id pageParser (location.hash.Substring 1)



type Place = { ``place name``: string; state: string; }
type ZipResponse = { places : Place list }

let get query =
    promise {
        let! r = Fable.PowerPack.Fetch.fetchAs<ZipResponse> ("http://api.zippopotam.us/us/" + query) []
        return r |> fun r -> r.places |> List.map (fun p -> p.``place name`` + ", " + p.state)
    }

(* If the URL is valid, we just update our model or issue a command.
If it is not a valid URL, we modify the URL to whatever makes sense.
*)
let urlUpdate (result:Result<Page,string>) model =
    match result with
    | Error e ->
        Browser.console.error("Error parsing url:", e)
        ( model, Navigation.modifyUrl (toHash model.Page) )

    | Ok (Page.Login as page) ->
        let m,cmd = Login.init model.Menu.User
        { model with Page = page; SubModel = LoginModel m }, Cmd.map LoginMsg cmd

    | Ok (Search query as page) ->
        { model with Page = page; Menu = { model.Menu with query = query } },
           if Map.containsKey query model.cache then []
           else Cmd.ofPromise get query (fun r -> FetchSuccess (query,r)) (fun ex -> FetchFailure (query,ex))

    | Ok page ->
        { model with Page = page; Menu = { model.Menu with query = "" } }, []

let init result =
    let menu,menuCmd = Menu.init()
    let m = 
        { Page = Home
          Menu = menu
          cache = Map.empty
          SubModel = NoSubModel }
    let m,cmd = urlUpdate result m
    m,Cmd.batch[cmd; menuCmd]


(* A relatively normal update function. The only notable thing here is that we
are commanding a new URL to be added to the browser history. This changes the
address bar and lets us use the browser&rsquo;s back button to go back to
previous pages.
*)
let update msg model =
    match msg with
    | AppMsg.OpenLogIn ->
        let m,cmd = Login.init None
        { model with
            Page = Page.Login
            SubModel = LoginModel m }, Cmd.batch [cmd; Navigation.modifyUrl (toHash Page.Login) ]

    | LoginMsg msg ->
        match model.SubModel with
        | LoginModel m -> 
            let m,cmd = Login.update msg m
            let cmd = Cmd.map LoginMsg cmd  
            match m.State with
            | Login.LoginState.LoggedIn token -> 
                let newUser : UserData = { UserName = m.Login.UserName; Token = token }
                let cmd =              
                    if model.Menu.User = Some newUser then cmd else
                    Utils.save "user" newUser
                    Cmd.batch [cmd; Cmd.ofMsg LoggedIn ]

                { model with 
                    SubModel = LoginModel m
                    Menu = { model.Menu with User = Some newUser }}, cmd
            | _ -> 
                { model with 
                    SubModel = LoginModel m
                    Menu = { model.Menu with User = None } }, cmd
        | _ -> model, Cmd.none

    | AppMsg.LoggedIn ->
        let m,cmd = urlUpdate (Ok Page.Home) model
        match model.Menu.User with
        | Some user ->
            m, Cmd.batch [cmd; Navigation.modifyUrl (toHash Page.Home) ]
        | None ->
            m, Cmd.ofMsg Logout

    | AppMsg.Query query ->
        { model with Menu = { model.Menu with query = query } }, []

    | AppMsg.Enter ->
        let newPage = Search model.Menu.query
        { model with Page = newPage }, Navigation.newUrl (toHash newPage)

    | AppMsg.FetchFailure (query,_) ->
        { model with cache = Map.add query [] model.cache }, []

    | AppMsg.FetchSuccess (query,locations) ->
        { model with cache = Map.add query locations model.cache }, []

    | AppMsg.Logout ->
        Utils.delete "user"
        { model with
            Page = Page.Home
            SubModel = NoSubModel
            Menu = { model.Menu with User = None } }, Navigation.modifyUrl (toHash Page.Home)

// VIEW

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Client.Style

let viewPage model dispatch =
    match model.Page with
    | Page.Home ->
        [ words 60 "Welcome!"
          text "Play with the links and search bar above. (Press ENTER to trigger the zip code search.)" ]

    | Page.Login -> 
        match model.SubModel with
        | LoginModel m -> 
            [ div [ ] [ Login.view m dispatch ]]
        | _ -> [ ]

    | Page.Blog id ->
        [ words 20 "This is blog post number"
          words 100 (string id) ]

    | Page.Search query ->
        match Map.tryFind query model.cache with
        | Some [] ->
            [ text ("No results found for " + query + ". Need a valid zip code like 90210.") ]
        | Some (location :: _) ->
            [ words 20 ("Zip code " + query + " is in " + location + "!") ]
        | _ ->
            [ text "..." ]

open Fable.Core.JsInterop

let view model dispatch =
  div []
    [ 
      Menu.view model.Menu dispatch
      hr [] []
      div [ centerStyle "column" ] (viewPage model dispatch)
    ]

open Elmish.React

// App
Program.mkProgram init update view
|> Program.toNavigable hashParser urlUpdate
|> Program.withConsoleTrace
|> Program.withReact "elmish-app"
|> Program.run