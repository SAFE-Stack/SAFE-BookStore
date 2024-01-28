module Storage

open System
open Azure
open Azure.Data.Tables
open Azure.Storage.Blobs
open Shared
open System.Threading.Tasks

module BookTitle =
    let isAllowed = string >> @"/\#?".Contains >> not

type StorageAccountName = StorageAccountName of string

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

let getBooksTable (client: TableServiceClient) = task {
    let table = client.GetTableClient "book"

    // Azure will temporarily lock table names after deleting and can take some time before the table name is made available again.
    let rec createTableSafe () = task {
        try
            let! _ = table.CreateIfNotExistsAsync()
            ()
        with _ ->
            do! Task.Delay 5000
            return! createTableSafe ()
    }

    do! createTableSafe ()
    return table
}

/// Load from the database
let getWishListFromDB (client: TableServiceClient) (userName: UserName) = task {
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

/// Save to the database
let saveWishListToDB connection wishList = task {
    let! booksTable = getBooksTable connection

    let buildAction book =
        let book = BookEntity.buildEntity wishList.UserName.Value book
        TableTransactionAction(TableTransactionActionType.UpsertReplace, book)

    let batch = wishList.Books |> Seq.map buildAction

    let! _ = booksTable.SubmitTransactionAsync batch
    ()
}

let addBook connection (userName: UserName) book = task {
    let! client = getBooksTable connection
    let entity = BookEntity.buildEntity userName.Value book

    let! _ = client.AddEntityAsync entity

    return {
        Title = book.Title
        Authors = book.Authors
        ImageLink = book.ImageLink
        Link = book.Link
    }
}

let removeBook connection (userName: UserName) (title: string) = task {
    let! client = getBooksTable connection
    let partitionKey = userName.Value
    let rowKey = title.ToCharArray() |> Array.filter BookTitle.isAllowed |> String
    let! _ = client.DeleteEntityAsync(partitionKey, rowKey)
    ()
}

module StateManagement =
    let getStateBlob (client: BlobServiceClient) name = task {
        let state = client.GetBlobContainerClient "state"
        let! _ = state.CreateIfNotExistsAsync()
        return state.GetBlobClient name
    }

    let resetTimeBlob connection = getStateBlob connection "resetTime"

    let storeResetTime connection = task {
        let! blob = resetTimeBlob connection
        let data = BinaryData ""
        let! _ = blob.UploadAsync data
        ()
    }

let getLastResetTime connection = task {
    let! blob = StateManagement.resetTimeBlob connection
    let! response = blob.GetPropertiesAsync()

    return
        if response.HasValue then
            Some response.Value.LastModified.Date
        else
            None
}