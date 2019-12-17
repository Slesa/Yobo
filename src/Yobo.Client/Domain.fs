﻿module Yobo.Client.Domain

type Page =
    | Calendar
    // auth
    | Login of Pages.Login.Domain.Model
    | Registration of Pages.Registration.Domain.Model

type Model = {
    CurrentPage : Page
}

module Model =
    let init = {
        CurrentPage = Calendar
    }

type Msg =
    // navigation
    | UrlChanged of Page
    // auth
    | LoginMsg of Pages.Login.Domain.Msg
    | RegistrationMsg of Pages.Registration.Domain.Msg