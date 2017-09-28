module Hmr

open Elmish
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser
open Fable.Helpers.React
open Fable.Helpers.React.Props

module Program =

    type IModule =
        abstract hot: obj with get, set

    let [<Global>] [<Emit("module")>] ``module`` : IModule = jsNative

    type HMRMsg<'msg> =
        | UserMsg of 'msg
        | Reload

    type HMRModel<'model> =
        { HMRCount : int
          UserModel : 'model }

    let mutable hmrState : obj = null

    let inline withHMR (program:Elmish.Program<'arg, 'model, 'msg, 'view>) =

        if not (isNull ``module``.hot) then
            ``module``.hot?accept() |> ignore

        let map (model, cmd) =
            model, cmd |> Cmd.map UserMsg

        let update msg model =
            let newModel,cmd =
                match msg with
                | UserMsg msg ->
                    let newModel, cmd = program.update msg model.UserModel
                    { model with UserModel = newModel }, cmd
                | Reload ->
                    { model with HMRCount = model.HMRCount + 1 }, Cmd.none
                |> map

            hmrState <- newModel
            // Store the state
            newModel, cmd

        let createModel (model, cmd) =
            { HMRCount = 0
              UserModel = model }, cmd

        let init =
            if isNull (hmrState) then
                program.init >> map >> createModel
            else
                (fun _ -> unbox<HMRModel<_>> hmrState, Cmd.ofMsg Reload )

        let subs model =
            Cmd.batch [ program.subscribe model.UserModel |> Cmd.map UserMsg ]

        { init = init
          update = update
          subscribe = subs
          onError = program.onError
          setState = fun model dispatch -> program.setState model.UserModel (UserMsg >> dispatch)
          view = fun model dispatch -> program.view model.UserModel (UserMsg >> dispatch) }