/// Functions to serialize and deserialize JSON, with client side support.
module ServerCode.FableJson

open Newtonsoft.Json
open Giraffe
open Microsoft.AspNetCore.Http

// The Fable.JsonConverter serializes F# types so they can be deserialized on the
// client side by Fable into full type instances, see http://fable.io/blog/Introducing-0-7.html#JSON-Serialization
// The converter includes a cache to improve serialization performance. Because of this,
// it's better to keep a single instance during the server lifetime.
let private jsonConverter = Fable.JsonConverter() :> JsonConverter

let toJson value =
    JsonConvert.SerializeObject(value, [|jsonConverter|])

let ofJson<'a> (json:string) : 'a =
    JsonConvert.DeserializeObject<'a>(json, [|jsonConverter|])

let serialize x =
    setStatusCode 200
      >=> setHttpHeader "Content-Type" "application/json"
      >=> setBodyAsString (toJson x)


let getJsonFromCtx<'a> (ctx:HttpContext) = task {
    let! t = ctx.ReadBodyFromRequestAsync()
    return ofJson<'a> t
}
