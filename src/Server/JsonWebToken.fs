/// JSON Web Token (JWT) functions.
module ServerCode.JsonWebToken

open System.IO
open Saturn.ControllerHelpers
open System
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt

let private createPassPhrase() =
    let crypto = System.Security.Cryptography.RandomNumberGenerator.Create()
    let randomNumber = Array.init 32 byte
    crypto.GetBytes(randomNumber)
    randomNumber

let secret =
    let fi = FileInfo("./temp/token.txt")
    if not fi.Exists then
        let passPhrase = createPassPhrase()
        if not fi.Directory.Exists then
            fi.Directory.Create()
        File.WriteAllBytes(fi.FullName,passPhrase)
    File.ReadAllBytes(fi.FullName)
    |> System.Text.Encoding.UTF8.GetString

let issuer = "safebookstore.io"

let private algorithm = Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256

let generateToken username =
    [ Claim(JwtRegisteredClaimNames.Sub, username);
      Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) ]
    |> Authentication.generateToken (secret, algorithm) issuer  (DateTime.UtcNow.AddHours(1.0))