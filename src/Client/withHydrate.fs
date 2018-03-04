namespace Client

open Elmish.React

[<RequireQualifiedAccess>]
module Program =
    open Fable.Import.Browser
    let withReactHydrate placeholderId (program:Elmish.Program<_,_,_,_>) =
        let setState dispatch =
            let viewWithDispatch = program.view dispatch
            fun model ->
                Fable.Import.ReactDom.hydrate(
                    lazyViewWith (fun x y -> obj.ReferenceEquals(x,y)) viewWithDispatch model,
                    document.getElementById(placeholderId)
                )

        { program with setState = setState }
