module Helpers

open System
open System.Reflection

let inline reraisePreserveStackTrace (e : Exception) =
    let remoteStackTraceString = typeof<exn>.GetField("_remoteStackTraceString", BindingFlags.Instance ||| BindingFlags.NonPublic);
    remoteStackTraceString.SetValue(e, e.StackTrace + Environment.NewLine);
    raise e
