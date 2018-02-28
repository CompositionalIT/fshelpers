module Cit.Helpers.Arm

open Microsoft.Azure.Management.ResourceManager.Fluent
open Microsoft.Azure.Management.ResourceManager.Fluent.Authentication
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System

module Parameters =
    type ParameterValue<'T> = { value : 'T }
    let toPValue v = { value = v }
    let private toBoxedPValue v = v |> toPValue |> box
    type ArmParameter =
    | ArmString of string
    | ArmInt of int
    | ArmBool of bool
    type ParameterType = Simple of (string * ArmParameter) list | Typed of obj
    let rec getArmParameterValue = function
        | ArmString s -> toBoxedPValue s
        | ArmInt i -> toBoxedPValue i
        | ArmBool b -> toBoxedPValue b

type DeploymentOutputs<'T> = Map<string, Parameters.ParameterValue<'T>>
type SimpleOutput = DeploymentOutputs<obj>
type AuthenticationCredentials = { ClientId : Guid; ClientSecret : string; TenantId : Guid }
type DeploymentStatus<'T> = DeploymentInProgress of state:string * operations:int | DeploymentError of statusCode:string * message:string | DeploymentCompleted of deployment:'T
type DeploymentMode =
    | Complete | Incremental
    member this.AsFluent = match this with | Complete -> Models.DeploymentMode.Complete | Incremental -> Models.DeploymentMode.Incremental
type ResourceGroupType =
    | New of name:string * Core.Region | Existing of name:string
type Deployment =
    { DeploymentName : string
      ResourceGroup : ResourceGroupType
      ArmTemplate : string
      Parameters : Parameters.ParameterType
      DeploymentMode : DeploymentMode }
type AuthenticatedContext = AuthenticatedContext of IResourceManager

module Internal =
    let (|Accepted|Running|Succeeded|Failed|Other|) = function
        | "Accepted" -> Accepted
        | "Running" -> Running
        | "Failed" -> Failed
        | "Succeeded" -> Succeeded
        | other -> Other other

    /// Creates parameters from key/value string pairs used by the Fluent API.
    let buildArmParameters keyValues =
        keyValues
        |> List.map (fun (k, v) -> k, Parameters.getArmParameterValue v)
        |> Map
        |> JsonConvert.SerializeObject

    let toDeploymentOutputs : obj -> _ = function
        | :? JObject as outputs ->
            outputs
            |> string
            |> fun s -> Some(Newtonsoft.Json.JsonConvert.DeserializeObject<'T> s)
        | null -> None
        | _ -> failwith "Unknown output type!"

    let rec monitorDeployment (deployment:IDeployment) = seq {
        let deployment = deployment.Refresh()
        let operations = deployment.DeploymentOperations.List() |> Seq.toArray
        yield DeploymentInProgress(deployment.ProvisioningState, operations.Length)
        match deployment.ProvisioningState with
        | Running | Accepted | Other _ ->            
            Async.Sleep 5000 |> Async.RunSynchronously
            yield! monitorDeployment deployment
        | Failed ->
            yield!
                operations
                |> Array.choose(fun operation ->
                    match operation.ProvisioningState with
                    | Failed -> Some(operation.StatusCode, string operation.StatusMessage)
                    | _ -> None)
                |> Seq.map DeploymentError
            failwith "Failed to complete deployment successfully."
        | Succeeded -> yield DeploymentCompleted (deployment.Outputs |> toDeploymentOutputs) }

    let buildDefinition (AuthenticatedContext resourceManager) deployment =
        let parameters =
            match deployment.Parameters with
            | Parameters.Simple parameters -> buildArmParameters parameters
            | Parameters.Typed o -> JsonConvert.SerializeObject o
        let tryCreateRg, withTemplate =
            let definition =
                resourceManager
                    .Deployments
                    .Define(deployment.DeploymentName.Replace(" ", "_"))
            match deployment.ResourceGroup with
            | New (name, region) ->
                let tryCreateRg() = resourceManager.ResourceGroups.Define(name).WithRegion(region).Create() |> ignore
                tryCreateRg, definition.WithNewResourceGroup(name, region)
            | Existing name -> ignore, definition.WithExistingResourceGroup(name)

        tryCreateRg()
        withTemplate            
            .WithTemplate(deployment.ArmTemplate)
            .WithParameters(parameters)
            .WithMode(deployment.DeploymentMode.AsFluent)

open Internal

/// Authenticates to Azure using the supplied credentials for a specific subscription.
let authenticate credentials (subscriptionId:Guid) =
    let spi = AzureCredentialsFactory().FromServicePrincipal(string credentials.ClientId, credentials.ClientSecret, string credentials.TenantId, AzureEnvironment.AzureGlobalCloud)
    ResourceManager
        .Authenticate(spi)
        .WithSubscription(string subscriptionId)
        |> AuthenticatedContext

/// Deploys an ARM template, providing a stream of progress updates and culminating with any outputs.
let deployWithProgress authContext =
    buildDefinition authContext
    >> fun fluent ->
        fluent.BeginCreate()
    >> monitorDeployment

/// Deploys an ARM template, returning any outputs.
let deploy authContext =
    deployWithProgress authContext
    >> Seq.choose(function | DeploymentCompleted outputs -> Some outputs | DeploymentError _ | DeploymentInProgress _ -> None)
    >> Seq.head

/// Creates an basic deployment using the supplied arguments.
let createSimple name resourceGroup region template parameters =
    { DeploymentName = name
      ResourceGroup = New(resourceGroup, region)
      ArmTemplate = template
      Parameters = parameters
      DeploymentMode = Incremental }

/// Creates and executes a basic deployment using the supplied arguments.
let deploySimple name resourceGroup region template parameters auth =
    createSimple name resourceGroup region template parameters
    |> deploy auth