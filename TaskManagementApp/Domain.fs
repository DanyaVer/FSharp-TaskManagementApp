module Domain

// Псевдоніми типів для ID
type TaskId = int
type UserId = UserId of int
type ProjectId = int

// Розмічене об'єднання для Пріоритету (Вимога 3.c)
type Priority =
    | Low
    | Medium
    | High

// Розмічене об'єднання для Статусу (Вимога 3.c)
type Status =
    | New
    | InProgress
    | Done
    | Blocked

// Запис для Користувача (Вимога 3.b)
type User = { Id: UserId; Name: string }

// Запис для Проєкту (Вимога 3.b)
type Project = { Id: ProjectId; Name: string }

// Запис для Завдання (Вимога 3.b)
type Task =
    { Id: TaskId
      Title: string
      Description: string option
      AssignedTo: UserId option
      Project: ProjectId option
      CurrentStatus: Status
      CurrentPriority: Priority }

type UpdateError =
    | TaskNotFound of TaskId

exception TaskNotFoundForUpdateException of TaskId
