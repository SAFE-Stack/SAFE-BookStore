module Storage

open System
open Azure
open Azure.Data.Tables
open Azure.Identity
open Shared
open System.Threading.Tasks

module BookTitle =
    let isAllowed = string >> @"/\#?".Contains >> not

type AzureConnection =
    | Dev of string
    | Deployed of Uri * DefaultAzureCredential

    member this.TableServiceClient =
        match this with
        | Dev connectionString -> TableServiceClient(connectionString)
        | Deployed(storageAccount, credentials) -> TableServiceClient(storageAccount, credentials)

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

let getBooksTable (connectionString: AzureConnection) = task {
    let client = connectionString.TableServiceClient
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
let getWishListFromDB connectionString (userName: UserName) = task {
    let! table = getBooksTable connectionString
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
let saveWishListToDB connectionString wishList = task {
    let! booksTable = getBooksTable connectionString

    let buildAction book =
        let book = BookEntity.buildEntity wishList.UserName.Value book
        TableTransactionAction(TableTransactionActionType.UpsertReplace, book)

    let batch = wishList.Books |> Seq.map buildAction

    let! thing = booksTable.SubmitTransactionAsync batch
    ()
}

let addBook connectionString (userName: UserName) book = task {
    let! client = getBooksTable connectionString
    let entity = BookEntity.buildEntity userName.Value book

    let! _ = client.AddEntityAsync entity

    return {
        Title = book.Title
        Authors = book.Authors
        ImageLink = book.ImageLink
        Link = book.Link
    }
}

let removeBook connectionString (userName: UserName) (title: string) = task {
    let! client = getBooksTable connectionString
    let partitionKey = userName.Value
    let rowKey = title.ToCharArray() |> Array.filter BookTitle.isAllowed |> String
    let! response = client.DeleteEntityAsync(partitionKey, rowKey)
    ()
}

// module StateManagement =
//     let getStateBlob (AzureConnection connectionString) name = task {
//         let client = (CloudStorageAccount.Parse connectionString).CreateCloudBlobClient()
//         let state = client.GetContainerReference "state"
//         let! _ = state.CreateIfNotExistsAsync()
//         return state.GetBlockBlobReference name }
//
//     let resetTimeBlob connectionString = getStateBlob connectionString "resetTime"
//
//     let storeResetTime connectionString = task {
//         let! blob = resetTimeBlob connectionString
//         return! blob.UploadTextAsync "" }
//
// let getLastResetTime connection = task {
//     let! blob = StateManagement.resetTimeBlob connection
//     do! blob.FetchAttributesAsync()
//     return blob.Properties.LastModified |> Option.ofNullable |> Option.map (fun d -> d.UtcDateTime)
// }