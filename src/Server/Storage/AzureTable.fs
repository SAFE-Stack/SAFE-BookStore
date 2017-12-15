module ServerCode.Storage.AzureTable

open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Table
open ServerCode.Domain
open System
open Giraffe.Tasks
open System.Threading.Tasks

type AzureConnection = 
    | AzureConnection of string

let getBooksTable (AzureConnection connectionString) = task {
    let client = (CloudStorageAccount.Parse connectionString).CreateCloudTableClient()
    let table = client.GetTableReference "book"

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

/// Load from the database
let getWishListFromDB connectionString userName = task {
    let! results = task {
        let! table = getBooksTable connectionString
        let query = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userName)
        return! table.ExecuteQuerySegmentedAsync(TableQuery(FilterString = query), null)  }
    return
        { UserName = userName
          Books =
            [ for result in results -> 
                { Title = result.Properties.["Title"].StringValue
                  Authors = string result.Properties.["Authors"].StringValue
                  Link = string result.Properties.["Link"].StringValue } ] } }

/// Save to the database
let saveWishListToDB connectionString wishList = task {
    let buildEntity userName book =
        let isAllowed = string >> @"/\#?".Contains >> not
        let entity = DynamicTableEntity()
        entity.PartitionKey <- userName
        entity.RowKey <- book.Title.ToCharArray() |> Array.filter isAllowed |> String
        entity

    let! existingWishList = getWishListFromDB connectionString wishList.UserName
    let batch =
        let operation = TableBatchOperation()
        let existingBooks = existingWishList.Books |> Set
        let newBooks = wishList.Books |> Set

        // Delete obsolete books
        (existingBooks - newBooks)
        |> Set.iter(fun book ->
            let entity = buildEntity wishList.UserName book
            entity.ETag <- "*"
            entity |> TableOperation.Delete |> operation.Add)

        // Insert new / update existing books
        (newBooks - existingBooks)
        |> Set.iter(fun book ->
            let entity = buildEntity wishList.UserName book
            entity.Properties.["Title"] <- EntityProperty.GeneratePropertyForString book.Title
            entity.Properties.["Authors"] <- EntityProperty.GeneratePropertyForString book.Authors
            entity.Properties.["Link"] <- EntityProperty.GeneratePropertyForString book.Link
            entity |> TableOperation.InsertOrReplace |> operation.Add)

        operation
    
    let! booksTable = getBooksTable connectionString
    let! _ = booksTable.ExecuteBatchAsync batch 
    return () }

module private StateManagement =
    let getStateBlob (AzureConnection connectionString) name = task {
        let client = (CloudStorageAccount.Parse connectionString).CreateCloudBlobClient()
        let state = client.GetContainerReference "state"
        let! _ = state.CreateIfNotExistsAsync() 
        return state.GetBlockBlobReference name }

    let resetTimeBlob connectionString = getStateBlob connectionString "resetTime"

    let storeResetTime connectionString = task {
        let! blob = resetTimeBlob connectionString
        return! blob.UploadTextAsync "" }

let getLastResetTime connectionString = task {
    let! blob = StateManagement.resetTimeBlob connectionString
    do! blob.FetchAttributesAsync() 
    return blob.Properties.LastModified |> Option.ofNullable |> Option.map (fun d -> d.UtcDateTime)
}

/// Clears all Wishlists and records the time that it occurred at.
let clearWishLists connectionString = task {
    let! table = getBooksTable connectionString
    let! _ = table.DeleteIfExistsAsync() 
    
    let! _ = Defaults.defaultWishList "test" |> saveWishListToDB connectionString
    do! StateManagement.storeResetTime connectionString }