module Client.ClientTypes

/// The user data sent with every message.
type UserData = 
  { UserName : string 
    Token : ServerCode.Domain.JWT }
