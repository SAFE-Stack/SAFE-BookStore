module ServerCode.Storage.Defaults

open ServerCode.Domain

/// The default initial data
let defaultWishList userName =
    { UserName = userName
      Books =
        [ { Title = "Mastering F#"
            Authors = "Alfonso Garcia-Caro Nunez"
            ImageLink = "/Images/covers/Alfonso.jpg"
            Link = "https://www.amazon.com/Mastering-F-Alfonso-Garcia-Caro-Nunez-ebook/dp/B01M112LR9" }
          { Title = "Get Programming with F#"
            Authors = "Isaac Abraham"
            ImageLink = "/Images/covers/Isaac.png"
            Link = "https://www.manning.com/books/get-programming-with-f-sharp" }
          { Title = "Stylish F#"
            Authors = "Kit Eason"
            ImageLink = "/Images/covers/Kit.jpg"
            Link = "https://www.apress.com/la/book/9781484239995" } ]
    }