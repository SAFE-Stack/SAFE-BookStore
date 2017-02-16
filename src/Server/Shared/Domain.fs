namespace ServerCode.Domain

open System

type JWT = string

type Login = { 
    UserName : string
    Password : string }