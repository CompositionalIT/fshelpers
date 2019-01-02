module FSharp.Core.Result

let private doBindExn errorMapper func =
    try func()
    with ex -> ex |> errorMapper |> Error
/// Converts exceptions to Errors for functions that already return a Result.
let bindExn errorMapper func = doBindExn errorMapper func
/// Converts exceptions to Errors for standard (non-Result) functions with the ability to specify the error type.
let ofExn errorMapper func = (func >> Ok) |> bindExn errorMapper
/// Combines two results into a tupled Ok. If both sides error, the first one is taken.
let inline combine resultB resultA =
    match resultA, resultB with
    | Ok a, Ok b -> Ok (a, b)
    | Error err, _ | _, Error err -> Error err
/// Lazily combines two results into a tupled Ok or error. If the first fails,
/// errors will not be merged. Instead, the first error will eagerly fail. Only if the
/// first result is Ok will the second result be evaluated.
let inline combineL resultB resultA =
    match resultA with
    | Ok a ->
        match (resultB()) with
        | Ok b -> Ok (a, b)
        | Error err -> Error err
    | Error err -> Error err
/// Converts Some -> Ok and None -> Error.
let inline ofOption msg = function Some x -> Ok x | None -> Error msg
/// Converts ValueSome -> Ok and ValueNone -> Error.
let inline ofOptionValue msg = function ValueSome x -> Ok x | ValueNone -> Error msg
/// Converts Some -> Ok and None -> Error, lazily generating the error msg.
let inline ofOptionL getMsg = function Some x -> Ok x | None -> Error (getMsg())
let inline private ignoreResult (_:Result<_,_>) = ()
/// Perform a side-effectful operation on a Result.
let iter (mapper:_ -> unit) result = result |> Result.map mapper |> ignoreResult
/// Explodes a Result<List 'T> into a List<Result 'T>.
let expand = function
    | Ok res -> res |> Seq.map Ok |> Seq.toArray
    | Error x -> [| Error x |]
/// Splits a result into Oks and errors
let inline partition results =
    let oks = ResizeArray()
    let errors = ResizeArray()
    results
    |> Seq.iter(function
        | Ok v -> oks.Add v
        | Error err -> errors.Add err)
    oks.ToArray(), errors.ToArray()
let inline private sortedDistinct x = x |> Array.distinct |> Array.sort
/// Merges results so that List<Result 'T> becomes Result<List 'T>. A single error will in the list will prevent Ok.
let inline merge results =
    match partition results with
    | oks, [||] -> Ok oks
    | _, errors -> Error errors.[0]
/// Merges results of unit so that List<Result unit> becomes Result<unit>. A single error will in the list will prevent Ok.
let inline mergeIgnore results = merge results |> Result.map ignore<unit seq>
/// Merges results so that List<Result 'T> becomes Result<List 'T>. A single Ok is sufficient to retain Ok; errors in this case are discarded.
/// However, if all cases are errors, they are merged into an error result.
let inline mergeOptimistic results =
    match partition results with
    | [||], [||] -> Ok [||]
    | [||], errors -> Error (Array.distinct errors)
    | oks, _ -> Ok oks
let toOption = function Ok x -> Some x | Error _ -> None
/// Performs side-effectful actions e.g. logging on both Ok and failure cases.
let sideEffect (onOk:_ -> unit) (onError:_ -> unit) result =
    match result with
    | Ok v as s -> onOk v; s
    | Error errors as e -> onError errors; e
/// Removes any errors from a list of results, allowing you to log them as needed.
let inline cleanse logger describeError results =
    let oks, errors = results |> partition
    errors |> Array.distinct |> Array.iter(describeError >> logger)
    oks
/// Converts a result to an option, logging any errors before converting to None.
let toOptionLog logger =
    let sideEffect = sideEffect ignore logger
    sideEffect >> toOption
/// Logs any errors of a side-effectful result before returning unit.
let logErrorsIgnore logger describeError (results:Result<unit, _> seq) =
    results
    |> cleanse logger describeError
    |> ignore
/// Replaces a failure case with a default result.
let withDefault defaultResult = function Ok v -> v | Error _ -> defaultResult
let ignore = ignoreResult
let isOk = function Ok _ -> true | _ -> false
let isError result = result |> isOk |> not
