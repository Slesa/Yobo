module Yobo.Shared.Text

type TextValue =
    | Id
    | FirstName
    | LastName
    | Password
    | SecondPassword
    | Email
    | Registration
    | Register
    | Login

type TextMessageValue =
    | ActivatingYourAccount
    | AccountSuccessfullyActivated
    | RegistrationSuccessful