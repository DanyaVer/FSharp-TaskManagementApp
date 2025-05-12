module Program

open System
open Domain // Доступ до типів
open Operations // Доступ до функцій

// Допоміжна функція для друку списку завдань
let printTaskList title tasks =
    printfn "\n--- %s ---" title
    if List.isEmpty tasks then
        printfn "Не знайдено."
    else
        tasks |> List.iter (formatTaskInfo >> printfn "%s") // Композиція formatTaskInfo і printfn

[<EntryPoint>]
let main argv =
    Console.OutputEncoding <- Text.Encoding.UTF8
    printfn "--- Моделювання системи керування завданнями ---"

    // --- Створення початкових даних ---
    let user1 : User = { Id = UserId 1; Name = "Олена" }
    let user2 : User = { Id = UserId 2; Name = "Андрій" }
    let project1 : Project = { Id = 101; Name = "Реліз v1.0" }

    // Створення завдань з використанням конвеєрів (Вимога 3.f)
    let task1 =
        createTask 1 "Налаштувати середовище"
        |> addTaskDescription "Встановити .NET SDK та VS Code/Rider"
        |> assignUserToTask user1.Id
        |> assignTaskToProject project1.Id
        |> updateTaskPriority High

    let task2 =
        createTask 2 "Написати типи даних"
        |> assignUserToTask user1.Id
        |> assignTaskToProject project1.Id
        |> updateTaskStatus InProgress

    let task3 =
        createTask 3 "Реалізувати логіку"
        |> assignUserToTask user2.Id
        |> assignTaskToProject project1.Id

    let task4 =
        createTask 4 "Написати тести"

    let task5 =
        createTask 5 "Підготувати документацію"
        |> assignUserToTask user1.Id
        |> updateTaskStatus Blocked
        |> updateTaskPriority Low

    let mutable tasks = [ task1; task2; task3; task4; task5 ]

    printTaskList "Початковий список завдань" tasks

    // --- Демонстрація частково застосованих функцій ---
    printfn "\n--- Демонстрація частково застосованих функцій ---"
    let markTaskAsDone = updateTaskStatus Done
    let prioritizeTaskAsHigh = updateTaskPriority High

    let updatedTask4 = task4 |> markTaskAsDone |> prioritizeTaskAsHigh
    tasks <- replaceTaskInList updatedTask4 tasks
    printfn "Оновлене завдання 4:"
    printfn "%s" (formatTaskInfo updatedTask4)

    // --- Демонстрація фільтрації (використовує карровані функції) ---
    let inProgressTasks = getTasksByStatus InProgress tasks
    printTaskList "Завдання зі статусом 'InProgress'" inProgressTasks

    let getHighPriorityTasks = getTasksByPriority High // Часткове застосування
    let highPriorityTasks = getHighPriorityTasks tasks
    printTaskList "Завдання з пріоритетом 'High'" highPriorityTasks

    let user1Tasks = getTasksByUser user1.Id tasks
    let (UserId userIdValue) = user1.Id 
    printTaskList (sprintf "Завдання, призначені користувачу %s (ID: %d)" user1.Name userIdValue) user1Tasks
    // --- Демонстрація використання кортежа ---
    let (taskCount, taskTitles) = getTaskTitlesAndCount tasks
    printfn "\n--- Демонстрація кортежа ---"
    printfn "Загальна кількість завдань: %d" taskCount
    printfn "Назви завдань: %A" taskTitles

    // --- Демонстрація композиції функцій ---
    printfn "\n--- Демонстрація композиції функцій (>>) ---"
    let getUpperTaskTitle = getTaskTitle >> toUpper // Композиція: getTitle а потім toUpper

    match List.tryHead tasks with
    | Some firstTask ->
        let upperTitle = getUpperTaskTitle firstTask
        printfn "Назва першого завдання у верхньому регістрі: %s" upperTitle
    | None -> printfn "Список завдань порожній."

    printfn "\n--- Завершення демонстрації ---"
    0