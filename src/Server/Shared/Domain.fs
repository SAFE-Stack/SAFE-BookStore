namespace ServerCode.Domain

open System

type JWT = string

type Login = { 
    UserName : string
    Password : string }

type Book = {
    Title: string
    Authors: string
    Link: string }
    with
        static member empty = { 
            Title = ""
            Authors = ""
            Link = ""}

type WishList = {
    UserName : string
    Books : Book list }
    with 
        static member empty userName = 
            { UserName = userName
              Books = [] }