module UITests.Tests

open canopy.classic
open Expecto

let serverUrl = "http://localhost:8085/"
let logoutLinkSelector = ".logout"
let loginLinkSelector = "Login"
let username = "test"
let password = "test"

let startApp () =
    url serverUrl
    waitForElement(".elmish-app")

let login () =
    let logout = someElement logoutLinkSelector 
    if logout.IsSome then
        click logoutLinkSelector 
        waitForElement loginLinkSelector

    click loginLinkSelector

    "#username" << username
    "#password" << password

    click "Log In"
    waitForElement logoutLinkSelector

let logout () =
    click logoutLinkSelector

let tests = 
    testList "client tests" [
        testCase "sound check - server is online" (fun () ->
            startApp ()
        )

        testCase "login with test user" (fun () ->
            startApp ()
            login ()
            logout ()
        )

        testCase "validate form fields" (fun () ->
            startApp ()
            login ()

            click ".btn"

            element "No title was entered" |> ignore
            element "No author was entered" |> ignore
            element "No link was entered" |> ignore

            "input[name=Title]" << "title"
            let titleWarnElem = someElement "No title was entered"
            Expect.isNone titleWarnElem "should dismiss title warning"

            "input[name=Author]" << "author"
            let authorWarnElem = someElement "No author was entered"
            Expect.isNone authorWarnElem "should dismiss author warning"

            "input[name=Link]" << "link"
            let linkWarnElem = someElement "No link was entered"
            Expect.isNone linkWarnElem "should dismiss link warning"

            logout()
        )

        // testCase "create and remove book" (fun () ->
        //     startApp ()
        //     login ()

        //     let initBookRows =
        //         match someElement "table tbody tr" with
        //         | Some (_) -> elements "table tbody tr"
        //         | None -> []

        //     let bookTitle = "Expert F# 4.0"
        //     let bookAuthor = "Don Syme & Adam Granicz & Antonio Cisternino"
        //     let bookLink = "https://www.amazon.com/Expert-F-4-0-Don-Syme/dp/1484207416"

        //     "input[name=Title]" << bookTitle
        //     "input[name=Author]" << bookAuthor
        //     "input[name=Link]" << bookLink

        //     click ".btn"

        //     let titleElement = element bookTitle
        //     let authorElement = element bookAuthor
        //     let removeBtn = titleElement |> parent |> parent |> elementWithin "Remove"

        //     let href = titleElement.GetAttribute("href")
        //     Expect.equal href bookLink "title element's href should be book link"

        //     let currBookRows = elements "table tbody tr"
        //     Expect.equal currBookRows.Length (initBookRows.Length + 1) "should add a new book"

        //     let bookRemoved () =
        //         match someElement bookTitle with
        //         | Some (_) -> false
        //         | None -> true

        //     click removeBtn
        //     waitFor bookRemoved

        //     let currBookRows = elements "table tbody tr"
        //     Expect.equal currBookRows.Length initBookRows.Length "should remove the new book"

        //     logout ()
        // )

        // DEMO12 - UI tests with canopy
        testCase "create a duplicate book" (fun () ->
            startApp ()
            login ()

            let bookTitle = "Expert F# 4.0"
            let bookAuthor = "Don Syme & Adam Granicz & Antonio Cisternino"
            let bookLink = "https://www.amazon.com/Expert-F-4-0-Don-Syme/dp/1484207416"

            "input[name=Title]" << bookTitle
            "input[name=Author]" << bookAuthor
            "input[name=Link]" << bookLink

            click ".btn"

            "input[name=Title]" << bookTitle
            "input[name=Author]" << bookAuthor
            "input[name=Link]" << bookLink

            click ".btn"

            element "Your wishlist contains this book already." |> ignore
            
            logout()

        )
    ]
