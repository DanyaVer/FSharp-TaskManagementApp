module TaskManagementApp.Domain

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

// Запис для Користувача
type User = { Id: UserId; Name: string }

// Запис для Проєкту
type Project = { Id: ProjectId; Name: string }

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


type OperationError =
    | TaskNotFound of TaskId
    | ItemNotFound of string
    | ValidationError of string

type OperationResult<'Success, 'Error> =
    | Success of 'Success
    | Failure of 'Error

exception TaskNotFoundForUpdateException of TaskId
