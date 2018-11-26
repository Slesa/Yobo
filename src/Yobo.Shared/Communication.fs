module Yobo.Shared.Communication

open System
open Yobo.Shared.Validation
open Yobo.Shared.Domain

type ServerError =
    | ValidationError of ValidationError list
    | DomainError of DomainError
    | Exception of string