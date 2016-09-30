// ==========================================================
// NEWS HACKER
//  
// News Hacker is a tool to bump up your Karma on Hacker News (https://news.ycombinator.com/)
//
// Visit https://dusted.codes/ for more information.
// ==========================================================

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net
open System.Net.Http
open System.Text.RegularExpressions
open System.Threading
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
    member this.ToHackerNewsFormData (fnid : string) =
        dict
            [ 
                "fnid",     fnid; 
                "fnop",     "submit-page"; 
                "title",    this.Title; 
                "url",      this.Url;
                "text",     "";
            ]

type Seconds = float

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

    let printInfo (data : KeyValuePair<string, string> seq ) =
        let title = data |> Seq.find (fun (x : KeyValuePair<string,string>) -> x.Key = "title")
        let url   = data |> Seq.find (fun (x : KeyValuePair<string,string>) -> x.Key = "url")
        printfn "Publising [%s](%s)" title.Value url.Value

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
        printInfo data
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
        "http://feeds.feedburner.com/TechCrunch/"
        "http://feeds.feedburner.com/TechCrunchIT"
        "http://feeds.feedburner.com/TechCrunch/Microsoft"
        "http://feeds.feedburner.com/TechCrunch/Twitter"
        "http://feeds.feedburner.com/TechCrunch/Google"
        "http://feeds.hanselman.com/ScottHanselman"
        "http://feeds.feedburner.com/TroyHunt"
        "https://blogs.msdn.microsoft.com/dotnet/feed/"
        "http://techblog.netflix.com/rss.xml"
        "http://feeds.macrumors.com/MacRumors-All"
        "http://feeds.feedburner.com/HighScalability"
        "https://news.bitcoin.com/feed"
        "https://blogs.msdn.microsoft.com/dotnet/feed/"
        "https://blogs.msdn.microsoft.com/typescript/feed/"
        "https://blogs.msdn.microsoft.com/visualstudio/feed/"
        "https://code.visualstudio.com/feed.xml"
    ]

let hackerNewsBaseUrl       = "https://news.ycombinator.com"
let hackerNewsFormUrl       = hackerNewsBaseUrl + "/submit"
let hackerNewsFormActionUrl = hackerNewsBaseUrl + "/r"
let userCookieValue         = Config.getValue "USER_COOKIE"
let maxAgeInSeconds         = Config.getValue "MAX_AGE"    |> Double.Parse
let sleepTimeInSeconds      = Config.getValue "SLEEP_TIME" |> Double.Parse

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

let convertToFormData (article : NewsArticle) =
    article.ToHackerNewsFormData (getNewFnid())

let filterArticlesByAge (maxAgeInSeconds    : Seconds)
                        (articles           : NewsArticle seq) =
    let maxAge = TimeSpan.FromSeconds(maxAgeInSeconds)
    articles
    |> Seq.filter (fun a -> 
        DateTime.UtcNow - a.PublishDate.ToUniversalTime() <= maxAge)

let postToHackerNews data =
    (hackerNewsHttpClient, hackerNewsFormActionUrl, data)
    |||> Curler.postForm

[<EntryPoint>]
let main argv = 
    printfn "Starting News Hacker."
    while true do
        printfn "Checking for new articles..."

        newsFeeds
        |> Seq.collect (Curler.getStream defaultHttpClient >> FeedParser.parseFeed)
        |> Seq.distinct
        |> filterArticlesByAge maxAgeInSeconds
        |> Seq.map convertToFormData
        |> Seq.iter postToHackerNews

        printfn "Taking a nap. zzZZ"

        let napTime = TimeSpan.FromSeconds(sleepTimeInSeconds)
        Thread.Sleep(napTime)
    0