module ServerCode.Logic

open ServerCode.Domain

open Fable.Remoting.Client

let routeBuilder typeName methodName = 
    sprintf "/api/%s/%s" typeName methodName

let server = Proxy.createWithBuilder<IServer> routeBuilder