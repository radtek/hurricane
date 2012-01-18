// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php
#I @"bin\Debug\"
#r "gSeries.ProvisionSupport.dll"
#r "FSharp.PowerPack"
#r "log4net.dll"

open System
open System.IO
open Microsoft.FSharp.Text
open GSeries.ProvisionSupport
open log4net
open log4net.Config
open log4net.Appender
open log4net.Layout

let dataFile = ref None
let verbose = ref false
// Empty otherArgs function.
let otherArgs (args : string) = ()
let specs =
    ["-d", ArgType.String (fun s -> dataFile := Some(s)), "Data file to read from"
     "-v", ArgType.Set verbose, "Display additional information"
     "--", ArgType.Rest otherArgs, "Stop parsing command line"
    ] |> List.map (fun (sh, ty, desc) -> ArgInfo(sh, ty, desc))
 
let () =
    ArgParser.Parse(specs, otherArgs)


let initLogger() = 
    new ConsoleAppender(Layout = new PatternLayout("%timestamp [%thread] %-5level %logger{1} - %message%newline"))
    |> BasicConfigurator.Configure 

let main (dataFile : string) = 
    initLogger()
    let logger = LogManager.GetLogger("CreateChunkDb")
    let dbFilePath = Path.Combine(Path.GetTempPath(), "newChunkDB.db");
    logger.DebugFormat("Data file path: {0}", dbFilePath)
    let dbs = new ChunkDbService(dbFilePath, true)
    dbs.AddFile dataFile
    logger.Debug("Data file processing finished.")

let _ =
    match !dataFile with
    | None -> 
        printf "No data file specified."
        // Error code 
        2
    | Some(f) -> 
        main f
        // Program exit code
        0