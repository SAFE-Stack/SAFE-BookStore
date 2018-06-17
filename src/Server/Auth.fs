/// Login web part and functions for API web part request authorisation with JWT.
module ServerCode.Auth

open ServerCode.Domain

let createUserData (login : Domain.Login) =
    {
        UserName = login.UserName
        Token    =
            ServerCode.JsonWebToken.encode (
                { UserName = login.UserName } : ServerTypes.UserRights
            )
    } : Domain.UserData