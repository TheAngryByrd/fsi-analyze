namespace fsi_analyze
open System.Linq

module Say =
    let nothing name = name |> ignore

    let hello name = sprintf "Hello %s" name

module Loader =
    open System
    open System.Collections.Generic
    open Microsoft.Diagnostics.Runtime
    type MemoryDump =
        {
            target : DataTarget
            runtime : ClrRuntime
            runtimeHeapObjects : Lazy<ClrObject array>

        } interface IDisposable with
            member x.Dispose () =
                x.target.Dispose()
          static member Create target runtime =
            {
                target = target
                runtime = runtime
                runtimeHeapObjects = lazy(runtime.Heap.EnumerateObjects() |> Seq.toArray)
            }

    type TypeHeapStats = {
        clrType : ClrType
        count : int64
        totalSize : uint64
    }
        with
            member x.AddObject size = {
                x with
                    totalSize = x.totalSize + size
                    count = x.count + 1L
            }
    let computeHeapStatistics (dump : MemoryDump) =
        printfn "starting computeHeapStatistics"
        let source = dump.runtimeHeapObjects.Value
        let length = source.Length
        let init = Dictionary<string, TypeHeapStats>()
        let mutable count = 0
        let init =
            (init,dump.runtime.Heap.EnumerateObjects())
            ||> Seq.fold(fun state next ->
                if count % 1000 = 0 then
                    let percentage = ((float count) / (float length)) * 100.
                    printfn "Progess at %.2f%%" percentage
                let name =
                    if (next.Type |> isNull) || (next.Type.Name |> isNull) then
                        "null"
                    else
                        next.Type.Name

                let value =
                    match state.TryGetValue(name) with
                    | (true, v) ->
                        v.AddObject next.Size
                    | _ ->
                        { clrType = next.Type; count = 1L; totalSize = next.Size }
                state.[name] <- value
                count <- count + 1
                state
            )
        init.Values.ToList()

    let loadDump (filePath) =
        let target = DataTarget.LoadCoreDump filePath
        let runtime = target.ClrVersions.[0].CreateRuntime()
        let dump = MemoryDump.Create target runtime
        dump

module TryChart =
    open XPlot.GoogleCharts

    let chartByTopCount (data : Loader.TypeHeapStats seq) =
        let source =
            data
            |> Seq.sortByDescending(fun d -> d.count)
            |> Seq.take 10
            |> Seq.map(fun d ->
                let name =
                    d.clrType.Name
                    |> Option.ofObj
                    |> Option.defaultValue "unknown type"
                (name, d.count)
            )
        let options =
            Options(
                title = "Top 10 Memory Allocations by Count",
                vAxis =
                    Axis(
                        title = "Count",
                        titleTextStyle = TextStyle(color = "red")
                    )
            )
        let chart =
            [source]
            |> Chart.Bar
            |> Chart.WithOptions options
            |> Chart.WithLabels ["Count";]
        chart.Show()


    let chartByTopsize (data : Loader.TypeHeapStats seq) =
        let source =
            data
            |> Seq.sortByDescending(fun d -> d.totalSize)
            |> Seq.take 10
            |> Seq.map(fun d ->
                let name =
                    d.clrType.Name
                    |> Option.ofObj
                    |> Option.defaultValue "unknown type"
                (name, d.totalSize)
            )
        let options =
            Options(
                title = "Top 10 Memory Allocations by Size",
                vAxis =
                    Axis(
                        title = "Size in bytes",
                        titleTextStyle = TextStyle(color = "red")
                    )
            )
        let chart =
            [source]
            |> Chart.Bar
            |> Chart.WithOptions options
            |> Chart.WithLabels ["Size";]
        chart.Show()



    // let Bolivia = ["2004/05", 165.; "2005/06", 135.; "2006/07", 157.; "2007/08", 139.; "2008/09", 136.]
    // let Ecuador = ["2004/05", 938.; "2005/06", 1120.; "2006/07", 1167.; "2007/08", 1110.; "2008/09", 691.]
    // let Madagascar = ["2004/05", 522.; "2005/06", 599.; "2006/07", 587.; "2007/08", 615.; "2008/09", 629.]
    // let Average = ["2004/05", 614.6; "2005/06", 682.; "2006/07", 623.; "2007/08", 609.4; "2008/09", 569.6]

    // let series = [ "bars"; "bars"; "bars"; "lines" ]
    // let inputs = [ Bolivia; Ecuador; Madagascar; Average ]

    // let show () =
    //     let chart =
    //          inputs
    //          |> Chart.Combo
    //          |> Chart.WithOptions
    //               (Options(title = "Coffee Production",
    //                        series = [| for typ in series -> Series(typ) |]))
    //          |> Chart.WithLabels ["Bolivia"; "Ecuador"; "Madagascar"; "Average"]
    //          |> Chart.WithLegend true
    //          |> Chart.WithSize (600, 250)

    //     chart.Show()
