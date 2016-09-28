// ==========================================================
// NEWS HACKER
//  
// News Hacker is a tool to bump up your Karma on Hacker News (https://news.ycombinator.com/)
//
// Visit https://dusted.codes/ for more information.
// ==========================================================

open System
open System.IO
open System.Linq
open System.Net
open System.Net.Http
open System.Text.RegularExpressions
open System.Xml.Linq

// ----------------------------------------------------------
// Types
// ----------------------------------------------------------

type NewsArticle =
    {
        Url         : string
        Title       : string
        PublishDate : DateTime
    }
    member this.ToHackerNewsSubmitFormData (fnid : string) =
        dict
            [ 
                "fnid",     fnid; 
                "fnop",     "submit-page"; 
                "title",    this.Title; 
                "url",      this.Url;
                "text",     "";
            ]

type Minutes = float

// ----------------------------------------------------------
// Config
// ----------------------------------------------------------

module Config =

    let getEnvironmentVariable (key : string) =
        Environment.GetEnvironmentVariable(key)
    
    let getValue (key : string) =
        match getEnvironmentVariable key with
        | null -> failwith (sprintf "Missing environment variable %s." key)
        | v    -> v

// ----------------------------------------------------------
// Curler
// ----------------------------------------------------------

module Curler =

    let runSynchronously task =
        task
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let getStream (httpClient : HttpClient) (url : string) = 
        url
        |> httpClient.GetStreamAsync
        |> runSynchronously

    let getString (httpClient : HttpClient) (url : string) = 
        url
        |> httpClient.GetStringAsync
        |> runSynchronously

    let postForm (httpClient : HttpClient) (url : string) (data) =      
        httpClient.PostAsync(url, new FormUrlEncodedContent(data))
        |> runSynchronously
        |> ignore

// ----------------------------------------------------------
// FeedParser
// ----------------------------------------------------------

module FeedParser =

    let byName name = fun (e : XElement) -> e.Name.LocalName = name

    let getChildElements (e : XElement) = e.Elements()

    let findChild (name : string) (elems : XElement seq) =
        elems
        |> Seq.find (byName name)

    let filterChildElements (name : string) (elems : XElement seq) =
        elems
        |> Seq.filter (byName name)

    let getValue (e : XElement) = e.Value

    let parseNewsArticle (element : XElement) =
        element 
        |> getChildElements
        |> (fun elems ->
            {
                Url         = elems |> findChild "link"    |> getValue
                Title       = elems |> findChild "title"   |> getValue
                PublishDate = elems |> findChild "pubDate" |> getValue |> DateTime.Parse
            })

    let parseFeed (stream : Stream) =
        XDocument.Load(stream).Root.Descendants()
        |> findChild "channel"
        |> getChildElements
        |> filterChildElements "item"
        |> Seq.map parseNewsArticle

// ----------------------------------------------------------
// Main
// ----------------------------------------------------------

// Set config values
let newsFeeds =
    [
        "http://feeds.feedburner.com/TechCrunchIT"
        "http://feeds.feedburner.com/TechCrunch/Microsoft"
        "http://feeds.feedburner.com/TechCrunch/Twitter"
        "http://feeds.feedburner.com/TechCrunch/Google"
    ]

let hackerNewsBaseUrl       = "https://news.ycombinator.com"
let hackerNewsFormUrl       = hackerNewsBaseUrl + "/submit"
let hackerNewsFormActionUrl = hackerNewsBaseUrl + "/r"
let userCookieValue         = Config.getValue "USER_COOKIE"
let maxAgeInMinutes         = Config.getValue "MAX_AGE" |> Double.Parse

// Configure HttpClient for querying news newsFeeds
let defaultHttpClient = new HttpClient()

// Configure HttpClient for Hacker News requests 
let cookies = new CookieContainer()
cookies.Add(
    new Uri(hackerNewsBaseUrl),
    new Cookie("user", userCookieValue))

let httpHandler           = new HttpClientHandler(CookieContainer = cookies)
let hackerNewsHttpClient  = new HttpClient(httpHandler)

hackerNewsHttpClient.DefaultRequestHeaders.Referrer <- new Uri(hackerNewsBaseUrl)

// Core Domain Logic
let extractFnid (html : string) =
    Regex(@"<input type=""hidden"" name=""fnid"" value=""(?<fnid>\w+)"">").Match(html).Groups.["fnid"].Value

let getNewFnid() =
    (hackerNewsHttpClient, hackerNewsFormUrl)
    ||> Curler.getString
    |> extractFnid

let ConvertToFormData (article : NewsArticle) =
    article.ToHackerNewsSubmitFormData (getNewFnid())

let filterArticlesByAge (maxAgeInMinutes    : Minutes)
                        (articles           : NewsArticle seq) =
    let maxAge = TimeSpan.FromMinutes(maxAgeInMinutes)
    articles
    |> Seq.filter (fun a -> 
        DateTime.UtcNow - a.PublishDate.ToUniversalTime() <= maxAge)

let PostToHackerNews data =
    (hackerNewsHttpClient, hackerNewsFormActionUrl, data)
    |||> Curler.postForm

[<EntryPoint>]
let main argv = 
    newsFeeds
    |> Seq.collect (Curler.getStream defaultHttpClient >> FeedParser.parseFeed)
    |> Seq.distinct
    |> filterArticlesByAge maxAgeInMinutes
    |> Seq.map ConvertToFormData
    |> Seq.iter PostToHackerNews    
    0