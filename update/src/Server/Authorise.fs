module Authorise

open System
open System.IO
open System.Security.Claims
open Microsoft.IdentityModel.JsonWebTokens
open Shared

let private algorithm = Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256

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

let generateToken username =
    [ Claim(JwtRegisteredClaimNames.Sub, username);
      Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) ]
    |> Saturn.Auth.generateJWT (secret, algorithm) issuer (DateTime.UtcNow.AddHours(1.0))

let createUserData (login : Login) : UserData =
    {
        UserName = UserName login.UserName
        Token    = generateToken login.UserName
    }

let login (login: Login) =
    if login.IsValid() then
        login
        |> createUserData
    else
        failwith $"User '{login.UserName}' can't be logged in"
