module TaskManagementApp.Program

open System
open TaskManagementApp.Domain
open TaskManagementApp.Operations
open TaskManagementApp.TaskManagerService

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
    let user2: User = { Id = UserId 2; Name = "Андрій" }
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


    printTaskList "Початковий список завдань" (getAllTasks tasksMap)

    // --- Демонстрація оновлення завдання у Map з використанням OperationResult ---
    printfn "\n--- Демонстрація оновлення завдання у Map ---"

    let updateFnForTask4 task =
        task
        |> updateTaskStatus Done
        |> updateTaskPriority High
        |> addTagToTask "critical"

    let updateResult = tryUpdateTaskInMap originalTask4.Id updateFnForTask4 tasksMap

    handleOperationResult "Оновлення завдання 4" updateResult (fun newMap ->
        tasksMap <- newMap // Оновлюємо стан, якщо успішно

        match getTaskFromCollectionById originalTask4.Id tasksMap with
        | Some updatedT4 -> printfn "Оновлене завдання 4:\n%s" (formatTaskInfo updatedT4)
        | None -> printfn "Неймовірно, завдання 4 зникло після оновлення!")

    // Спроба оновити неіснуюче завдання
    let nonExistentTaskId = 99

    let updateNonExistentResult =
        tryUpdateTaskInMap nonExistentTaskId (updateTaskStatus Done) tasksMap

    handleOperationResult "Оновлення неіснуючого завдання" updateNonExistentResult (fun _ -> ())


    // --- Демонстрація фільтрації завдань ---
    let inProgressTasks = getTasksByStatus InProgress tasksMap
    printTaskList "Завдання зі статусом 'InProgress'" inProgressTasks

    let highPriorityTasks = getTasksByPriority High tasksMap
    printTaskList "Завдання з пріоритетом 'High'" highPriorityTasks

    let (UserId userIdValue) = user1.Id
    let user1Tasks = getTasksByUser user1.Id tasksMap
    printTaskList (sprintf "Завдання, призначені користувачу %s (ID: %d)" user1.Name userIdValue) user1Tasks

    // Демонстрація фільтрації за тегом
    let codingTasks = getTasksByTag "coding" tasksMap
    printTaskList "Завдання з тегом 'coding'" codingTasks


    // --- Демонстрація власної узагальненої функції tryFindById ---
    printfn "\n--- Демонстрація узагальненої функції tryFindById ---"

    let users: User seq =
        [ { Id = UserId 10
            Name = "Тест Користувач1" }
          { Id = UserId 20
            Name = "Тест Користувач2" } ]

    match tryFindById (UserId 10) users with
    | Some user -> printfn "Знайдено користувача за ID (UserId 10): %s" user.Name
    | None -> printfn "Користувача з ID (UserId 10) не знайдено."

    match tryFindById (UserId 30) users with
    | Some user -> printfn "Знайдено користувача за ID (UserId 30): %s" user.Name
    | None -> printfn "Користувача з ID (UserId 30) не знайдено (очікувано)."


    // --- Демонстрація видалення завдання з використанням OperationResult ---
    printfn "\n--- Демонстрація видалення завдання ---"
    let taskIdToRemove = task5.Id
    let removalResult = removeTaskFromCollection taskIdToRemove tasksMap

    handleOperationResult "Видалення завдання 3" removalResult (fun newMap ->
        tasksMap <- newMap
        printTaskList (sprintf "Список завдань ПІСЛЯ видалення завдання ID %d" taskIdToRemove) (getAllTasks tasksMap))

    // Спроба видалити неіснуюче
    let removalNonExistentResult = removeTaskFromCollection nonExistentTaskId tasksMap
    handleOperationResult "Видалення неіснуючого завдання" removalNonExistentResult (fun _ -> ())


    // --- Демонстрація кортежа та композиції ---
    printfn "\n--- Демонстрація кортежа ---"

    let taskTitlesAndCountDisplayable (taskCount, taskTitles) =
        { new IDisplayable with
            member _.GetDisplayString() =
                let titlesStr = String.concat ", " taskTitles
                sprintf "Зведена інформація: Всього завдань - %d. Назви: [%s]" taskCount titlesStr }

    printDisplayableItem (taskTitlesAndCountDisplayable (getTaskTitlesAndCount tasksMap))

    printfn "\n--- Завершення демонстрації (v3.0) ---"
    0
