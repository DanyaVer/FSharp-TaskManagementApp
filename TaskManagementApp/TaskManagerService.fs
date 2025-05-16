module TaskManagementApp.TaskManagerService

open TaskManagementApp.Domain
open TaskManagementApp.Operations

[<AllowNullLiteral>]
type TaskManagerService =
    val mutable private tasks: Map<TaskId, Task>

    new(initialTasks: seq<Task>) = { tasks = initialTasks |> Seq.map (fun t -> t.Id, t) |> Map.ofSeq }

    new() = { tasks = Map.empty }

    member this.AllTasks: seq<Task> = this.tasks |> Map.values |> Seq.cast

    member this.TaskCount: int = Map.count this.tasks

    member this.AddTask(id: TaskId, title: string, ?description: string, ?priority: Priority, ?status: Status) : Task =
        let mutable newTask = createTask id title

        match description with
        | Some desc -> newTask <- addTaskDescription desc newTask
        | None -> ()

        match priority with
        | Some p -> newTask <- updateTaskPriority p newTask
        | None -> ()

        match status with
        | Some s -> newTask <- updateTaskStatus s newTask
        | None -> ()

        this.tasks <- Map.add newTask.Id newTask this.tasks
        newTask

    member this.TryGetTaskById(taskId: TaskId) : Task option = Map.tryFind taskId this.tasks

    member this.UpdateTask(taskId: TaskId, updateFn: Task -> Task) : OperationResult<unit, OperationError> =
        match tryUpdateTaskInMap taskId updateFn this.tasks with
        | Success newMap ->
            this.tasks <- newMap
            Success()
        | Failure err -> Failure err

    member this.RemoveTask(taskId: TaskId) : OperationResult<unit, OperationError> =
        match removeTaskFromCollection taskId this.tasks with
        | Success newMap ->
            this.tasks <- newMap
            Success()
        | Failure err -> Failure err

    member this.GetTasksByStatus(status: Status) : seq<Task> =
        getTasksByStatus status this.tasks

    member this.GetTasksByTag(tag: string) : seq<Task> =
        getTasksByTag tag this.tasks
