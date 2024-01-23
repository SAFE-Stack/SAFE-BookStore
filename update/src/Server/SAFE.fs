namespace SAFE

open System
open Fable.Remoting.Server
open Microsoft.AspNetCore.Http

module ErrorHandling =
    let rec getRealException (ex: Exception) =
        match ex with
        | :? AggregateException as ex -> getRealException ex.InnerException
        | _ -> ex

    let errorHandler<'a> (ex: exn) (routeInfo: RouteInfo<HttpContext>) =
        Propagate(getRealException ex)