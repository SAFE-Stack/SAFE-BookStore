module ServerCode.TokenUtils

open System.IO
open System.Text
open Newtonsoft.Json
open System.Security.Cryptography

let private createPassPhrase() = 
    let crypto = System.Security.Cryptography.RandomNumberGenerator.Create()
    let randomNumber = Array.init 32 byte
    crypto.GetBytes(randomNumber)
    randomNumber

let passPhrase =
    let encoding = Encoding.UTF8
    let fi = FileInfo("./temp/token.txt")
    if not fi.Exists then
        let passPhrase = createPassPhrase()
        if not fi.Directory.Exists then
            fi.Directory.Create()
        File.WriteAllBytes(fi.FullName,passPhrase)

    File.ReadAllBytes(fi.FullName)
let encodeString (payload:string) =
    Jose.JWT.Encode(payload, passPhrase, Jose.JweAlgorithm.A256KW, Jose.JweEncryption.A256CBC_HS512)

let decodeString (jwt:string) =
    Jose.JWT.Decode(jwt, passPhrase, Jose.JweAlgorithm.A256KW, Jose.JweEncryption.A256CBC_HS512)

let encode token =
    JsonConvert.SerializeObject token
    |> encodeString

let decode<'a> (jwt:string) : 'a =
    decodeString jwt
    |> JsonConvert.DeserializeObject<'a>

let isValid (jwt:string) : ServerTypes.UserRights option =
    try
        let token = decode jwt
        Some token
    with
    | _ -> None