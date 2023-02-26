module ServerCode.Storage.AzureTable

open System.Linq.Expressions
open Azure
open Azure.Data.Tables
open Azure.Storage.Blobs
open FSharp.Control
open ServerCode.Domain
open System
open System.IO
open System.Text
open System.Text.Encodings
open System.Threading.Tasks
open System.Linq

type AzureConnection =
    | AzureConnection of string

let getBooksTable (AzureConnection connectionString) = task {
    let table = new TableClient(connectionString, "book")

    // Azure will temporarily lock table names after deleting and can take some time before the table name is made available again.
    let rec createTableSafe() = task {
        try
        let! _ = table.CreateIfNotExistsAsync()
        ()
        with _ ->
            do! Task.Delay 5000
            return! createTableSafe() }

    do! createTableSafe()
    return table }

type Book = { PartitionKey: string }

/// Load from the database
let getWishListFromDB connectionString userName = task {
    let! results = task {
        let! table = getBooksTable connectionString
        return! table.QueryAsync<TableEntity>(fun b -> b.PartitionKey = userName).ToListAsync() }
    return
        { UserName = userName
          Books =
            [ for result in results ->
                let x = result["foo"]
                { Title = result.GetString "Title"
                  Authors = result.GetString "Authors"
                  ImageLink = result.GetString "ImageLink"
                  Link = result.GetString "Link" } ] } }

/// Save to the database
let saveWishListToDB connectionString wishList = task {
    let buildEntity userName book =
        let isAllowed = string >> @"/\#?".Contains >> not
        let entity = TableEntity(userName, book.Title.ToCharArray() |> Array.filter isAllowed |> String)
        entity

    let! existingWishList = getWishListFromDB connectionString wishList.UserName
    let! booksTable = getBooksTable connectionString
    
    let batch =
        existingWishList.Books
        |> Seq.map (fun book ->
            let entity = buildEntity wishList.UserName book
            entity.ETag <- ETag("*")
            TableTransactionAction(TableTransactionActionType.Delete, entity)
        )
    
    let! _ = booksTable.SubmitTransactionAsync batch
    ()

    let batch =
        wishList.Books
        |> Seq.map (fun book ->
            let entity = buildEntity wishList.UserName book
            entity["Title"] <- book.Title
            entity["Authors"] <- book.Authors
            entity["ImageLink"] <- book.ImageLink
            entity["Link"] <- book.Link
            TableTransactionAction(TableTransactionActionType.UpsertReplace, entity))


    let! _ = booksTable.SubmitTransactionAsync batch
    ()
}

module StateManagement =
    let getStateBlob (AzureConnection connectionString) name = task {
        let state = BlobContainerClient(connectionString, "state")
        let! _ = state.CreateIfNotExistsAsync()
        return state.GetBlobClient name }

    let resetTimeBlob connectionString = getStateBlob connectionString "resetTime"

    let storeResetTime connectionString = task {
        let! blob = resetTimeBlob connectionString
        return! blob.UploadAsync(MemoryStream.Null) }

let getLastResetTime connection = task {
    let! blob = StateManagement.resetTimeBlob connection
    let! props = blob.GetPropertiesAsync()
    if props.HasValue then
        return Some props.Value.LastModified.UtcDateTime
    else return None
}
