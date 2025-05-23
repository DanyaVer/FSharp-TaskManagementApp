module TaskAnalyserApp.Analysers

open TaskAnalyserApp.CsvLoader
open TaskManagementLib.Domain
open TaskManagementLib.Operations

// 1. Аналіз: Кількість завдань на кожного користувача
let private countTasksPerUserAsync (loadedData: LoadedData) : Async<TasksPerUserStats list> =
    async {
        let usersMap = loadedData.Users |> List.map (fun u -> u.Id, u.Name) |> Map.ofList

        return
            loadedData.Tasks
            |> List.groupBy (fun task -> task.AssignedTo) // Групуємо за AssignedTo (UserId option)
            |> List.choose (fun (optUserId, tasksInGroup) -> // Вибираємо тільки ті, де є UserId
                match optUserId with
                | Some userId ->
                    let userName =
                        Map.tryFind userId usersMap |> Option.defaultValue "Невідомий користувач"

                    Some
                        { UserId = userId
                          UserName = userName
                          TaskCount = List.length tasksInGroup }
                | None -> None)
            |> List.sortByDescending (fun stat -> stat.TaskCount)
    }

// 2. Аналіз: Середній "бал" пріоритету завдань для кожного статусу
let private calculateAveragePriorityPerStatusAsync (tasks: Task list) : Async<AveragePriorityPerStatus list> =
    async {
        return
            tasks
            |> List.groupBy (fun task -> task.CurrentStatus)
            |> List.map (fun (status, tasksInGroup) ->
                let totalPriorityScore =
                    tasksInGroup
                    |> List.map (fun task -> task.CurrentPriority)
                    |> List.sumBy priorityToScore

                let count = List.length tasksInGroup

                let avgScore =
                    if count > 0 then
                        float totalPriorityScore / float count
                    else
                        0.0

                { Status = status
                  AveragePriorityScore = avgScore })
            |> List.sortBy (fun item -> item.Status.ToString()) // Для послідовного виводу
    }

// 3. Аналіз: Кількість завдань у кожному проекті (включаючи завдання без проекту)
let private countTasksPerProjectAsync (loadedData: LoadedData) : Async<ProjectTaskCount list> =
    async {
        let projectsMap =
            loadedData.Projects |> List.map (fun p -> Some p.Id, p.Name) |> Map.ofList

        return
            loadedData.Tasks
            |> List.groupBy (fun task -> task.Project) // Групуємо за Project (ProjectId option)
            |> List.map (fun (optProjectId, tasksInGroup) ->
                let projectName =
                    match optProjectId with
                    | Some pId -> Map.tryFind (Some pId) projectsMap |> Option.defaultValue $"Проєкт ID: {pId}"
                    | None -> "Завдання без проекту"

                { ProjectId = optProjectId
                  ProjectName = projectName
                  TaskCount = List.length tasksInGroup })
            |> List.sortByDescending (fun stat -> stat.TaskCount)
    }

// Головна функція для запуску всіх аналізів
let runAnalysisAsync (loadedData: LoadedData) : Async<unit> =
    async {
        printfn "\n--- Початок аналізу даних ---"

        // Аналіз 1
        let! tasksPerUser = countTasksPerUserAsync loadedData
        printfn "\n1. Кількість завдань на кожного користувача:"

        if List.isEmpty tasksPerUser then
            printfn "  Дані відсутні або завдання не призначені."
        else
            tasksPerUser
            |> List.iter (fun stat ->
                let (UserId idVal) = stat.UserId
                printfn "  Користувач: %s (ID: %d) - %d завдань" stat.UserName idVal stat.TaskCount)

        // Аналіз 2
        let! avgPriorityPerStatus = calculateAveragePriorityPerStatusAsync loadedData.Tasks
        printfn "\n2. Середній бал пріоритету за статусами (1=Low, 2=Medium, 3=High):"

        if List.isEmpty avgPriorityPerStatus then
            printfn "  Дані про завдання відсутні."
        else
            avgPriorityPerStatus
            |> List.iter (fun stat ->
                printfn "  Статус: %A - Середній пріоритет: %.2f" stat.Status stat.AveragePriorityScore)

        // Аналіз 3
        let! tasksPerProject = countTasksPerProjectAsync loadedData
        printfn "\n3. Кількість завдань по проектах:"

        if List.isEmpty tasksPerProject then
            printfn "  Дані про проекти або завдання відсутні."
        else
            tasksPerProject
            |> List.iter (fun stat -> printfn "  Проект: %s - %d завдань" stat.ProjectName stat.TaskCount)

        printfn "\n--- Аналіз даних завершено ---"
    }
