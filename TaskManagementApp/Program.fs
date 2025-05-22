module TaskManagementApp.Program

open System
open TaskManagementLib.Domain
open TaskManagementLib.Operations
open TaskManagementLib.TaskManagerService

// Допоміжна функція для друку списку завдань
let printTaskList title (tasksToPrint: #seq<Task>) = // #seq для гнучкості
    printfn "\n--- %s ---" title

    if Seq.isEmpty tasksToPrint then
        printfn "Не знайдено."
    else
        tasksToPrint |> Seq.iter (formatTaskInfo >> printfn "%s")

// Допоміжна функція для обробки OperationResult
let handleOperationResult<'Success, 'Error>
    (operationName: string)
    (opResult: OperationResult<'Success, 'Error>)
    (successAction: 'Success -> unit)
    =
    match opResult with
    | Success result ->
        printfn "УСПІХ (%s): Операція виконана." operationName
        successAction result
    | Failure err -> printfn "ПОМИЛКА (%s): %A" operationName err


[<EntryPoint>]
let main argv =
    Console.OutputEncoding <- Text.Encoding.UTF8
    printfn "--- Моделювання системи керування завданнями (v3.0) ---"

    // --- Створення початкових даних ---
    let user1: User = { Id = UserId 1; Name = "Олена" }
    let project1: Project = { Id = 101; Name = "Реліз v1.0" }

    let task1 =
        createTask 1 "Налаштувати середовище"
        |> addTaskDescription "Встановити .NET SDK"
        |> assignUserToTask user1.Id
        |> assignTaskToProject project1.Id
        |> updateTaskPriority High
        |> addTagToTask "setup"
        |> addTagToTask "devops"

    let initialTasksForService = [task1; createTask 2 "Написати API"]
    
    let taskService = TaskManagerService(initialTasksForService)
    printfn "Початкова кількість завдань у сервісі: %d" taskService.TaskCount
    
    let task3 = taskService.AddTask(3, "Тестування модуля", description = "Покрити тестами новий API", priority = Priority.Medium)
    printfn "Завдання додано через сервіс: %s" ((task3 :> IDisplayable).GetDisplayString())
    printfn "Нова кількість завдань у сервісі: %d" taskService.TaskCount

    let updateStatusFn (t: Task) = { t with CurrentStatus = Status.InProgress }
    let updateOpResult = taskService.UpdateTask(task1.Id, updateStatusFn)
    handleOperationResult (sprintf "Оновлення завдання ID %d через сервіс" task1.Id) updateOpResult (fun _ ->
        match taskService.TryGetTaskById(task1.Id) with
        | Some updatedT1 -> printfn "Оновлене завдання ID 1: %s" ((updatedT1 :> IDisplayable).GetDisplayString())
        | None -> ()
    )

    printfn "Всі завдання з сервісу:"
    taskService.AllTasks |> Seq.iter (fun t -> printDisplayableItem (t :> IDisplayable))


    // --- Демонстрація кортежа та композиції ---
    printfn "\n--- Демонстрація кортежа ---"

    let taskTitlesAndCountDisplayable (taskCount, taskTitles) =
        { new IDisplayable with
            member _.GetDisplayString() =
                let titlesStr = String.concat ", " taskTitles
                sprintf "Зведена інформація: Всього завдань - %d. Назви: [%s]" taskCount titlesStr }

    printDisplayableItem (taskTitlesAndCountDisplayable (getTaskTitlesAndCount taskService.AllTasks))

    printfn "\n--- Завершення демонстрації (v3.0) ---"
    0
