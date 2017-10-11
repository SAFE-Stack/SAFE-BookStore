module ServerCode.Storage.AzureTable

open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Table
open Microsoft.WindowsAzure.Storage.Blob
open ServerCode.Domain

type AzureConnection = 
    | AzureConnection of string

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

/// Load from the database
let getWishListFromDB connectionString userName = async {
    let! results = async {
        let! table = getBooksTable connectionString
        let query = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userName)           
        return! table.ExecuteQuerySegmentedAsync(TableQuery(FilterString = query), null) |> Async.AwaitTask }
    return
        { UserName = userName
          Books =
            [ for result in results -> 
                { Title = result.Properties.["Title"].StringValue
                  Authors = string result.Properties.["Authors"].StringValue
                  Link = string result.Properties.["Link"].StringValue } ] } }

/// Save to the database
let saveWishListToDB connectionString wishList = async {
    let buildEntity userName book =
        let disallowed = [ "/"; "\\"; "#"; "?" ]
        let entity = DynamicTableEntity()
        entity.PartitionKey <- userName
        entity.RowKey <- (book.Title, disallowed) ||> List.fold(fun title reserved -> title.Replace(reserved, ""))
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
    return! booksTable.ExecuteBatchAsync batch |> Async.AwaitTask |> Async.Ignore }

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

let getLastResetTime appStart connectionString =
    fun () ->
    async {
        let! blob = StateManagement.resetTimeBlob connectionString
        do! blob.FetchAttributesAsync() |> Async.AwaitTask
        return blob.Properties.LastModified |> Option.ofNullable |> Option.map (fun d -> d.UtcDateTime) |> Option.defaultValue appStart }

/// Clears all Wishlists and records the time that it occurred at.
let clearWishLists connectionString = async {
    let! table = getBooksTable connectionString
    do! table.DeleteIfExistsAsync() |> Async.AwaitTask |> Async.Ignore
    do! Defaults.defaultWishList "test" |> saveWishListToDB connectionString
    do! StateManagement.storeResetTime connectionString }