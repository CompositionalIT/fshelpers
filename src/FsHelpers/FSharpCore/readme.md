# Result.fs
This module contains extensions to the `Result` type that is bundled with FSharp.Core, adding a number of useful combinator functions for everyday use.

## Basic operations
### iter
Perform a side-effectful operation on a Result.
```fsharp
Ok 99 |> Result.iter (fun x -> x + 1) // does not compile
Ok 99 |> Result.iter (printfn "PRINTING %d") // prints "PRINTING 99"
Error 99 |> Result.iter (printfn "PRINTING %d") // does nothing
```

### withDefault
Replaces a failure case with a default result.

```fsharp
let x = Error "bad" // Result<_, string>
let y = x |> Result.withDefault 99 // 99
```

### isOk
Tests if a Result is Ok

```fsharp
Error "bad" |> isOk // false
Ok 99 |> isOk // true
```

### isError
Tests if a Result is an Error

```fsharp
Error "bad" |> isError // true
Ok 99 |> isError // false
```

### ignore
Ignores a Result

```fsharp
Ok 99 |> Result.ignore // ()
"test" |> Result.ignore // does not compile
```

### sideEffect
Performs side-effectful actions e.g. logging on both Ok and failure cases designed for insertion within a pipeline.

```fsharp
let x = Ok 99
let y = Error "Argh!"

x
|> Result.sideEffect (printfn "Success! %d") (printfn "Error! %s") // prints "Success! 99"
|> Result.map (fun x -> x + 10)
|> Result.sideEffect (printfn "Success! %d") (printfn "Error! %s") // prints "Success! 109"

y
|> Result.sideEffect (printfn "Success! %d") (printfn "Error! %s") // prints "Error! Argh"
|> Result.map (fun x -> x + 10)
|> Result.sideEffect (printfn "Success! %d") (printfn "Error! %s") // prints "Error! Argh"
```

## Composing Results
### combine
Combines two results into a tupled Ok. If both sides error, the first one is taken.
```fsharp
type MyResult = Result<int, string> // type hint to stop compiler inference errors
let a : MyResult = Ok 99
let b : MyResult = Ok 1
let c : MyResult = Error "Oh dear!"
let d : MyResult = Error "Oh noes!"

a |> Result.combine b // Ok (99, 1)
a |> Result.combine c // Error "Oh dear!"
c |> Result.combine d // Error "Oh dear!" - first result survives
```

### combineL
Lazily combines two results into a tupled Ok or error. If the first fails, errors will not be merged. Instead, the first error will eagerly fail. Only if the first result is Ok will the second result be evaluated.

```fsharp
let a : MyResult = Ok 99
let b() :  MyResult = Ok 1

a |> Result.combineL b // Ok (99, 1)
```

## Option combinators
### ofOption, ofValueOption
Converts `Some` -> `Ok` and `None` -> `Error`.

```fsharp
Some 99 |> Result.ofOption "Whoops!" // Ok 99
let x : Result<int,_> = None |> Result.ofOption "Whoops!" // Error "Whoops"
```

### ofOptionL
Converts `Some` -> `Ok` and `None` -> `Error`, lazily generating the error msg.

```fsharp
Some 99 |> Result.ofOptionL (fun _ -> "Whoops!") // Ok 99
let x : Result<int,_> = None |> Result.ofOptionL (fun _ -> "Whoops!") // Error "Whoops"
```

### toOption
Converts `Ok` -> `Some` and `Error` -> `None`

```fsharp
Ok 99 |> Result.toOption // Some 99
Error "Bad" |> Result.toOption // None
```

### toOptionLog
Converts a result to an option, logging any errors before converting to None.

```fsharp
Ok 99 |> Result.toOptionLog (printfn "BAD %A") // Some 99
let x : Option<int> = Error 99 |> Result.toOptionLog (printfn "BAD %A") // None, prints "BAD 99"
```

## Error handling
### ofExn
Converts exceptions to Errors for standard (non-Result) functions with the ability to specify the error type.
```fsharp
let getNumber() =
    failwith "Whoops!"
    99
    
let result = getNumber |> Result.ofExn (fun ex -> ex.Message) // Error "Whoops!"
```

### bindExn
Converts exceptions to Errors for functions that already return a Result.
```fsharp
let getNumber() =
    failwith "Whoops!"
    Ok 99
    
let result = getNumber |> Result.bindExn (fun ex -> ex.Message) // Error "Whoops!"
```

## Collections of Results
### expand
Expands a Result<List 'T> into a List<Result 'T>.

```fsharp
let x : Result<int, string> array = Ok [ 1; 2; 3 ] |> Result.expand // [| Ok 1; Ok 2; Ok 3 |]
```

### merge
Merges results so that List<Result 'T> becomes Result<List 'T>. A single error will in the list will prevent Ok.

```fsharp
let x : Result<int array, string> = [ Ok 1; Ok 2; Ok 3 ] |> Result.merge // Ok [| 1;2;3 |]
let y = [ Ok 1; Error "Bad"; Ok 3 ] |> Result.merge // Error "Bad"
```

### mergeOptimistic
Merges results so that `List<Result 'T>` becomes `Result<List 'T>`. A single Ok is sufficient to retain Ok; errors in this case are discarded. However, if all cases are errors, they are merged into an error result.

```fsharp
let x = [ Ok 1; Error "Bad"; Ok 3 ] |> Result.merge // Ok [| 1; 3 |]
```

### mergeIgnore
Merges results of `unit` so that `List<Result unit>` becomes `Result<unit>`. A single error will in the list will prevent Ok.
```fsharp
let x : Result<unit, string> =
    [ Ok (); Ok (); Ok () ] |> Result.mergeIgnore // Ok ()
    
let y = [ Ok 1; Error "DB Failure"; Ok 3 ] |> Result.merge // Error "DB Failure"
```

### partition
Partitions a `Result<'Ok, 'Error>` into `('Ok array * 'Error array`)

```fsharp
let oks, errors =
    [ Ok 123; Error "DB Failure"; Error "Another Error"; Ok 456 ]
    |> Result.partition

// oks = [|123;456|]
// errors = [|"DB Failure"; "Another Error"|]

```

### cleanse
Removes any errors from a list of results, allowing you to log them as needed.

```fsharp
let stuff =
    [ Ok 123; Error "DB Failure"; Error "Another Error"; Ok 456 ]
    |> Result.cleanse (printfn "ERROR: %s")

// ERROR: DB Failure
// ERROR: Another Error
// val stuff : int [] = [|123; 456|]
```

### cleanseSideEffect
Logs any errors of a collection of side-effectful results before returning unit.

```fsharp
do [ Ok (); Error "DB Failure"; Error "Another Error"; Ok () ]
   |> Result.cleanseSideEffects (printfn "ERROR: %s")

// ERROR: DB Failure
// ERROR: Another Error
// val it : unit = ()
```