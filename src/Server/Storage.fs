module Storage

open System
open Azure
open Azure.Data.Tables
open Azure.Storage.Blobs
open Shared
open System.Threading.Tasks

module Option =
    let ofResponse (response: Response<'t>) =
        match response.HasValue with
        | true -> Some response.Value
        | false -> None

module BookTitle =
    let isAllowed = string >> @"/\#?".Contains >> not

type BookEntity() =
    member val Title = "" with get, set
    member val Authors = "" with get, set
    member val ImageLink = "" with get, set
    member val Link = "" with get, set

    interface ITableEntity with
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = System.Nullable() with get, set
        member val ETag = ETag.All with get, set

module BookEntity =
    let buildEntity userName book =
        let entity: ITableEntity =
            BookEntity(Title = book.Title, Authors = book.Authors, Link = book.Link, ImageLink = book.ImageLink)

        entity.PartitionKey <- userName
        entity.RowKey <- book.Title.ToCharArray() |> Array.filter BookTitle.isAllowed |> String
        entity

module Defaults =
    let mockBooks = [
        {
            Title = "Get Programming with F#"
            Authors = "Isaac Abraham"
            ImageLink = "/images/Isaac.png"
            Link = "https://www.manning.com/books/get-programming-with-f-sharp"
        }

        {
            Title = "Mastering F#"
            Authors = "Alfonso Garcia-Caro Nunez"
            ImageLink = "/images/Alfonso.jpg"
            Link = "https://www.amazon.com/Mastering-F-Alfonso-Garcia-Caro-Nunez-ebook/dp/B01M112LR9"
        }

        {
            Title = "Stylish F#"
            Authors = "Kit Eason"
            ImageLink = "/images/Kit.jpg"
            Link = "https://www.apress.com/la/book/9781484239995"
        }
    ]

    let wishList = {
        UserName = UserName "test"
        Books = mockBooks
    }

let getBooksTable (client: TableServiceClient) = async {
    let table = client.GetTableClient "book"

    // Azure will temporarily lock table names after deleting and can take some time before the table name is made available again.
    let rec createTableSafe () = async {
        try
            let! _ = table.CreateIfNotExistsAsync() |> Async.AwaitTask
            ()
        with _ ->
            do! Async.Sleep 5000
            return! createTableSafe ()
    }

    do! createTableSafe ()
    return table
}

/// Load from the database
let getWishListFromDB (client: TableServiceClient) (userName: UserName) = async {
    let! table = getBooksTable client
    let results = table.Query<BookEntity>($"PartitionKey eq '{userName.Value}'")

    return {
        UserName = userName
        Books = [
            for result in results ->
                {
                    Title = result.Title
                    Authors = result.Authors
                    ImageLink = result.ImageLink
                    Link = result.Link
                }
        ]
    }
}

let saveWishListToDB client wishList = async {
    let! table = getBooksTable client

    let existingBooks =
        table.Query<BookEntity>($"PartitionKey eq '{wishList.UserName.Value}'")

    let deleteAction book =
        TableTransactionAction(TableTransactionActionType.Delete, book)

    let upsertAction book =
        let book = BookEntity.buildEntity wishList.UserName.Value book
        TableTransactionAction(TableTransactionActionType.UpsertReplace, book)

    let! _ =
        existingBooks
        |> Seq.map deleteAction
        |> table.SubmitTransactionAsync
        |> Async.AwaitTask

    let! _ =
        wishList.Books
        |> Seq.map upsertAction
        |> table.SubmitTransactionAsync
        |> Async.AwaitTask

    ()
}

let addBook client (userName: UserName) book = async {
    let! table = getBooksTable client
    let entity = BookEntity.buildEntity userName.Value book

    let! _ = table.AddEntityAsync entity |> Async.AwaitTask

    return {
        Title = book.Title
        Authors = book.Authors
        ImageLink = book.ImageLink
        Link = book.Link
    }
}

let removeBook client (userName: UserName) (title: string) = async {
    let! table = getBooksTable client
    let partitionKey = userName.Value
    let rowKey = title.ToCharArray() |> Array.filter BookTitle.isAllowed |> String
    let! _ = table.DeleteEntityAsync(partitionKey, rowKey) |> Async.AwaitTask
    ()
}

module StateManagement =
    let getStateBlob (client: BlobServiceClient) name = async {
        let state = client.GetBlobContainerClient "state"
        let! _ = state.CreateIfNotExistsAsync() |> Async.AwaitTask
        return state.GetBlobClient name
    }

    let resetTimeBlob client = getStateBlob client "resetTime"

    let storeResetTime client = async {
        let! blob = resetTimeBlob client
        let data = BinaryData ""
        let _ = blob.DeleteIfExistsAsync() |> Async.AwaitTask
        let! _ = blob.UploadAsync data |> Async.AwaitTask
        ()
    }

let getLastResetTime client systemStartTime = async {
    let! blob = StateManagement.resetTimeBlob client

    try
        let! response = blob.GetPropertiesAsync() |> Async.AwaitTask

        return
            response
            |> Option.ofResponse
            |> Option.map (_.LastModified.UtcDateTime)
            |> Option.defaultValue systemStartTime

    with :? RequestFailedException ->
        return systemStartTime
}