// Learn more about F# at http://fsharp.org

open System
open fsi_analyze
[<EntryPoint>]
let main argv =
    // let dataDump = Loader.loadDump "/Users/jimmybyrd/core_4910-2019-11-27"
    let dataDump = Loader.loadDump "/mnt/hgfs/Documents/core_4910-2019-11-27"
    // let version = dataDump.ClrVersions |> Seq.head
    let stats = Loader.computeHeapStatistics dataDump
    printfn "%A" stats
    // printfn "CLR Version %s" (string version.Version)
    TryChart.chartByTopsize stats
    TryChart.chartByTopCount stats

    printfn "Hello World from F#!"
    System.Console.ReadLine () |> ignore
    0 // return an integer exit code
