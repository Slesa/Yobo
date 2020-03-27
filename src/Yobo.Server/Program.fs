﻿module Yobo.Server.Program

open System.Threading.Tasks
open Microsoft.Azure.WebJobs
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Giraffe
open Attributes
open Microsoft.Azure.WebJobs.Extensions.Http
open FSharp.Control.Tasks

let webApp (root:CompositionRoot) =
    choose [
        // anonymous handlers
        Auth.HttpHandlers.authServiceHandler root
        
        // authenticated handlers
        Auth.HttpHandlers.onlyForLoggedUser root.Auth >=> choose [
            UserAccount.HttpHandlers.userAccountServiceHandler root
        ]
    ]

[<FunctionName("Index")>]
let run ([<HttpTrigger (AuthorizationLevel.Anonymous, Route = "{*any}")>] req : HttpRequest, context : ExecutionContext, log : ILogger, [<CompositionRoot>]root: CompositionRoot) =
    task {
        let hostingEnvironment = req.HttpContext.GetHostingEnvironment()
        hostingEnvironment.ContentRootPath <- context.FunctionAppDirectory
        let! _ = webApp root (Some >> Task.FromResult) req.HttpContext
        return Microsoft.AspNetCore.Mvc.EmptyResult()
    }
