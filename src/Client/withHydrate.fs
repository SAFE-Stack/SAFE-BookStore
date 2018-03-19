module Elmish.React.Extension

[<RequireQualifiedAccess>]
module Program =
    open Fable.Import.Browser
    /// Setup rendering of root React component inside html element identified by placeholderId using React.hydrate
    let withReactHydrate placeholderId (program:Elmish.Program<_,_,_,_>) =
        let setState dispatch =
            let viewWithDispatch = program.view dispatch
            fun model ->
                Fable.Import.ReactDom.hydrate(
                    lazyViewWith (fun x y -> obj.ReferenceEquals(x,y)) viewWithDispatch model,
                    document.getElementById(placeholderId)
                )

        { program with setState = setState }
