module ServerCode.Storage.AzureTable

open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Table
open ServerCode.Domain

let getBooksTable connectionString = async {
    let client = (CloudStorageAccount.Parse connectionString).CreateCloudTableClient()
    let table = client.GetTableReference "book"
    do! table.CreateIfNotExistsAsync() |> Async.AwaitTask |> Async.Ignore
    return table }

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