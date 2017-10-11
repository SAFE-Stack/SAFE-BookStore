module ServerCode.Storage.AzureTable

open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Table
open Microsoft.WindowsAzure.Storage.Blob
open ServerCode.Domain

type AzureConnection = AzureConnection of string

let getBooksTable (AzureConnection connectionString) = async {
    let client = (CloudStorageAccount.Parse connectionString).CreateCloudTableClient()
    let table = client.GetTableReference "book"

    // Azure will temporarily lock table names after deleting and can take some time before the table name is made available again.
    let rec createTableSafe() = async {
        try
        do! table.CreateIfNotExistsAsync() |> Async.AwaitTask |> Async.Ignore
        with _ ->
            do! Async.Sleep 5000
            return! createTableSafe() }

    do! createTableSafe()
    return table }

/// Save to the database
let saveWishListToDB connectionString wishList = async {
    let operation = TableBatchOperation()

    wishList.Books
    |> List.map(fun book ->
        let entity = DynamicTableEntity()
        entity.PartitionKey <- wishList.UserName
        entity.RowKey <- (wishList.UserName + book.Title).GetHashCode() |> string
        entity.Properties.["Title"] <- EntityProperty.GeneratePropertyForString book.Title
        entity.Properties.["Authors"] <- EntityProperty.GeneratePropertyForString book.Authors
        entity.Properties.["Link"] <- EntityProperty.GeneratePropertyForString book.Link
        entity)
    |> List.iter (TableOperation.InsertOrReplace >> operation.Add)
    
    let! booksTable = getBooksTable connectionString
    return! booksTable.ExecuteBatchAsync operation |> Async.AwaitTask |> Async.Ignore }

let getWishListFromDB connectionString userName = async {
    let rec loadResultsFromDb() = async {
        let! table = getBooksTable connectionString
        let query = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userName)           
        let! results = table.ExecuteQuerySegmentedAsync(TableQuery(FilterString = query), null) |> Async.AwaitTask
        match results.Results |> Seq.toList with 
        | [] ->
            do! saveWishListToDB connectionString (Defaults.defaultWishList userName)
            return! loadResultsFromDb()
        | results -> return results }

    let! results = loadResultsFromDb()
    return
        { UserName = userName
          Books =
            results
            |> List.map(fun result ->
                { Title = result.Properties.["Title"].StringValue
                  Authors = string result.Properties.["Authors"].StringValue
                  Link = string result.Properties.["Link"].StringValue }) } }


module private StateManagement =
    let getStateBlob (AzureConnection connectionString) name = async {
        let client = (CloudStorageAccount.Parse connectionString).CreateCloudBlobClient()
        let state = client.GetContainerReference "state"
        do! state.CreateIfNotExistsAsync() |> Async.AwaitTask |> Async.Ignore
        return state.GetBlockBlobReference name }

    let resetTimeBlob connectionString = getStateBlob connectionString "resetTime"
    let storeResetTime connectionString = async {
        let! blob = resetTimeBlob connectionString
        do! blob.UploadTextAsync "" |> Async.AwaitTask |> Async.Ignore }

let getLastResetTime connectionString =
    fun () ->
    async {
        let! blob = StateManagement.resetTimeBlob connectionString
        do! blob.FetchAttributesAsync() |> Async.AwaitTask
        return blob.Properties.LastModified |> Option.ofNullable |> Option.map(fun d -> d.UtcDateTime) }

/// Clears all Wishlists and records the time that it occurred at.
let clearWishLists connectionString = async {
    let! table = getBooksTable connectionString
    do! table.DeleteIfExistsAsync() |> Async.AwaitTask |> Async.Ignore
    do! StateManagement.storeResetTime connectionString }