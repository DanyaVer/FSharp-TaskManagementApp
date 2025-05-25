module TaskManagementLib.Domain

// Псевдоніми типів для ID
type TaskId = int
type UserId = UserId of int
type ProjectId = int

// Розмічене об'єднання для Пріоритету
type Priority =
    | Low
    | Medium
    | High
    | UnknownPriority

    static member FromString(s: string) =
        match s.ToLowerInvariant() with
        | "low" -> Low
        | "medium" -> Medium
        | "high" -> High
        | _ -> UnknownPriority

// Розмічене об'єднання для Статусу
type Status =
    | New
    | InProgress
    | Done
    | Blocked
    | UnknownStatus


    static member FromString(s: string) =
        match s.ToLowerInvariant() with
        | "new" -> New
        | "inprogress" -> InProgress
        | "done" -> Done
        | "blocked" -> Blocked
        | _ -> UnknownStatus

type IDisplayable =
    abstract member GetDisplayString: unit -> string

// Запис для Користувача
type User =
    { Id: UserId
      Name: string }

    interface IDisplayable with
        member this.GetDisplayString() =
            $"Користувач: %s{this.Name} (ID: %A{this.Id})"


// Запис для Проєкту
type Project =
    { Id: ProjectId
      Name: string }

    interface IDisplayable with
        member this.GetDisplayString() =
            $"Проєкт: %s{this.Name} (ID: %d{this.Id})"

// Запис для Завдання
type Task =
    { Id: TaskId
      Title: string
      Description: string option
      AssignedTo: UserId option
      Project: ProjectId option
      CurrentStatus: Status
      CurrentPriority: Priority
      Tags: Set<string> }

    interface IDisplayable with
        member this.GetDisplayString() =
            let statusStr = sprintf "Статус: %A" this.CurrentStatus
            let priorityStr = sprintf "Пріоритет: %A" this.CurrentPriority

            let lines =
                [ yield statusStr
                  yield priorityStr

                  match this.Description with
                  | Some d when not (System.String.IsNullOrWhiteSpace d) -> yield $"Опис: {d}"
                  | _ -> ()

                  match this.AssignedTo with
                  | Some userId -> yield $"Призначено користувачу ID {userId}"
                  | None -> ()

                  match this.Project with
                  | Some projectId -> yield $"Проєкт ID {projectId}"
                  | None -> ()

                  if not (Set.isEmpty this.Tags) then
                      yield "Теги: " + (this.Tags |> Seq.sort |> String.concat ", ") ]
            
            $"Завдання: '{this.Title}' (ID: {this.Id})" +
            String.concat "\n - " lines


type TasksPerUserStats =
    { UserId: UserId
      UserName: string
      TaskCount: int }

type AveragePriorityPerStatus =
    { Status: Status
      AveragePriorityScore: float }

type ProjectTaskCount =
    { ProjectId: ProjectId option
      ProjectName: string
      TaskCount: int }


type OperationError =
    | TaskNotFound of TaskId
    | ValidationError of string

type OperationResult<'Success, 'Error> =
    | Success of 'Success
    | Failure of 'Error

exception TaskNotFoundForUpdateException of TaskId
