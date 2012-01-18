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

let initLogger() = 
    new ConsoleAppender(Layout = new PatternLayout("%timestamp [%thread] %-5level %logger{1} - %message%newline"))
    |> BasicConfigurator.Configure

let main (dbFile : string, dataFile : string, protoFile : string) = 
    initLogger()
    let logger = LogManager.GetLogger("QueryProfileDb")
    logger.DebugFormat("Db file path: {0}", dbFile)
    let chunkDbPath = Path.Combine(Path.GetTempPath(), "newChunkDB.db");
    let chunkDb = new ChunkDbService(chunkDbPath, false)
    let profileDb = new VirtualDiskProfileService(dbFile, dataFile, chunkDb)
    let dto = profileDb.ToChunkMapDto()
    ChunkMapSerializer.Serialize(protoFile, dto)
    
    logger.Debug("Db file processing finished.")


let _ =
    do main (fsi.CommandLineArgs.[1], fsi.CommandLineArgs.[2], 
        fsi.CommandLineArgs.[3]) |> ignore
