module ServerCode.Storage.Defaults

open ServerCode.Domain

/// The default initial data
let defaultWishList userName =
    { UserName = userName
      Books =
        [ { Title = "Mastering F#"
            Authors = "Alfonso Garcia-Caro Nunez"
            Link = "https://www.amazon.com/Mastering-F-Alfonso-Garcia-Caro-Nunez-ebook/dp/B01M112LR9" }
          { Title = "Get Programming with F#"
            Authors = "Isaac Abraham"
            Link = "https://www.manning.com/books/get-programming-with-f-sharp" }
          { Title = "Stylish F#"
            Authors = "Kit Eason"
            Link = "https://www.apress.com/la/book/9781484239995" } ]
    }