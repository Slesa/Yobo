module Yobo.Client.AccountActivation.State

open Yobo.Client.AccountActivation.Domain
open Elmish

let update (msg : Msg) (state : State) : State * Cmd<Msg> =
    state, Cmd.none
    //match msg with
    //| Login -> { state with IsLogging = true }, Cmd.none
    //| ChangeEmail v -> { state with Credentials = { state.Credentials with Email = v }}, Cmd.none
    //| ChangePassword v -> { state with Credentials = { state.Credentials with Password = v }}, Cmd.none