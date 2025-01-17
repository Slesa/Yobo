﻿module Yobo.Server.Local.Program

open System.Diagnostics
open System.IO

[<EntryPoint>]
let main _ =
    let d = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".Local",""))
    let p = ProcessStartInfo()
    p.WorkingDirectory <- d
//    p.FileName <- "cmd.exe"
    p.FileName <- "func"
//    p.Arguments <- "/K func host start"
    p.Arguments <- "host start"
    Process.Start(p).WaitForExit()
    0