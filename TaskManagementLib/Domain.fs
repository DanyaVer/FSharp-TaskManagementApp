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

// Розмічене об'єднання для Статусу
type Status =
    | New
    | InProgress
    | Done
    | Blocked

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
            let statusStr = sprintf "%A" this.CurrentStatus // %A для DU
            let priorityStr = sprintf "%A" this.CurrentPriority
            $"Завдання '%s{this.Title}' (ID: %d{this.Id}) - Статус: %s{statusStr}, Пріоритет: %s{priorityStr}"

type OperationError =
    | TaskNotFound of TaskId
    | ValidationError of string

type OperationResult<'Success, 'Error> =
    | Success of 'Success
    | Failure of 'Error

exception TaskNotFoundForUpdateException of TaskId