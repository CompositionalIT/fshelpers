## Basic operations
### iter
```fsharp
Ok 99 |> Result.iter (fun x -> x + 1) // does not compile
Ok 99 |> Result.iter (printfn "PRINTING %d") // prints "PRINTING 99"
Error 99 |> Result.iter (printfn "PRINTING %d") // does nothing
```

### withDefault
```fsharp
let x = Error "bad" // Result<_, string>
let y = x |> Result.withDefault 99 // 99
```

### isOk
```fsharp
Error "bad" |> isOk // false
Ok 99 |> isOk // true
```

### isError
```fsharp
Error "bad" |> isError // true
Ok 99 |> isError // false
```

### ignore
```fsharp
Ok 99 |> Result.ignore // ()
"test" |> Result.ignore // does not compile
```

### sideEffect
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
As `combine`, but b is computed lazily e.g.

```fsharp
let a : MyResult = Ok 99
let b() :  MyResult = Ok 1

a |> Result.combineL b // Ok (99, 1)
```

## Option combinators
### ofOption, ofValueOption
```fsharp
Some 99 |> Result.ofOption "Whoops!" // Ok 99
let x : Result<int,_> = None |> Result.ofOption "Whoops!" // Error "Whoops"
```

### ofOptionL
Lazy version of `ofOption`.

```fsharp
Some 99 |> Result.ofOptionL (fun _ -> "Whoops!") // Ok 99
let x : Result<int,_> = None |> Result.ofOptionL (fun _ -> "Whoops!") // Error "Whoops"
```

### toOption
```fsharp
Ok 99 |> Result.toOption // Some 99
Error "Bad" |> Result.toOption // None
```

### toOptionLog
```fsharp
Ok 99 |> Result.toOptionLog (printfn "BAD %A") // Some 99
let x : Option<int> = Error 99 |> Result.toOptionLog (printfn "BAD %A") // None, prints "BAD 99"
```

## Error handling
### ofExn
```fsharp
let getNumber() =
    failwith "Whoops!"
    99
    
let result = getNumber |> Result.ofExn (fun ex -> ex.Message) // Error "Whoops!"
```

### bindExn
```fsharp
let getNumber() =
    failwith "Whoops!"
    Ok 99
    
let result = getNumber |> Result.bindExn (fun ex -> ex.Message) // Error "Whoops!"
```

## Collections of Results
### expand
### partition
### merge
### mergeIgnore
### mergeOptimistic
### cleanse
### logErrorsIgnore