﻿module Yobo.Client.View

open Domain
open Elmish
open Feliz
open Feliz.Bulma
open Feliz.Router

let private parseUrl = function
    | [ Paths.Login ] -> Login (Pages.Login.Domain.Model.init)
    | [ Paths.Registration ] -> Registration Pages.Registration.Domain.Model.init
    | [ Paths.Calendar ] -> Calendar
    | _ -> Model.init.CurrentPage
    
let view (model:Model) (dispatch:Msg -> unit) =
    let render =
        match model.CurrentPage with
        | Login m -> Pages.Login.View.view m (LoginMsg >> dispatch)
        | Registration m -> Pages.Registration.View.view m (RegistrationMsg >> dispatch)
        | Calendar ->
            Html.a [
                prop.text "Login"
                prop.href (Router.format Paths.Login)
                prop.onClick Router.goToUrl
            ]
            
    Router.router [
        Router.onUrlChanged (parseUrl >> UrlChanged >> dispatch)
        Router.pathMode
        Router.application render
    ]