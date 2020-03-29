module Yobo.Client.Pages.Registration.Domain

open System
open Yobo.Client.Forms
open Yobo.Shared.Auth.Communication
open Yobo.Shared.Errors

type Model = {
    ShowTerms : bool
    Form : ValidatedForm<Request.Register>
    ShowThankYou : bool
}

module Model =
    let init = {
        ShowTerms = false
        Form = Request.Register.init |> ValidatedForm.init
        ShowThankYou = false
    }

type Msg =
    | FormChanged of Request.Register
    | Register
    | Registered of ServerResult<unit>
    | ToggleTerms