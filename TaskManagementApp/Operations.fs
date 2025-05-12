module Operations

open Domain

// --- Функції для створення та оновлення завдань ---

// Створення нового завдання
let createTask (id: TaskId) (title: string) : Task =
    { Id = id
      Title = title
      Description = None
      AssignedTo = None
      Project = None
      CurrentStatus = New
      CurrentPriority = Medium }

// Оновлення статусу завдання
let updateTaskStatus (newStatus: Status) (task: Task) : Task = { task with CurrentStatus = newStatus }

// Оновлення пріоритету завдання
let updateTaskPriority (newPriority: Priority) (task: Task) : Task =
    { task with
        CurrentPriority = newPriority }

// Призначення користувача
let assignUserToTask (userId: UserId) (task: Task) : Task = { task with AssignedTo = Some userId }

// Призначення проекту
let assignTaskToProject (projectId: ProjectId) (task: Task) : Task = { task with Project = Some projectId }

// Додавання опису
let addTaskDescription (description: string) (task: Task) : Task =
    { task with
        Description = Some description }

// Оновлює завдання у списку і повертає новий список
let tryUpdateTaskInList (taskIdToUpdate: TaskId) (updateFn: Task -> Task) (taskList: Task list) : Task list option =
    match taskList |> List.tryFind (fun t -> t.Id = taskIdToUpdate) with
    | Some taskToUpdate ->
        let updatedTask = updateFn taskToUpdate

        let newList =
            taskList |> List.map (fun t -> if t.Id = taskIdToUpdate then updatedTask else t)

        Some newList
    | None -> None

// Оновлює завдання, якщо існує, і повертає оновлений список (або той же)
let updateTaskInList (taskIdToUpdate: TaskId) (updateFn: Task -> Task) (taskList: Task list) : Result<Task list, UpdateError> =
    match tryUpdateTaskInList taskIdToUpdate updateFn taskList with
        | Some newTaskList -> Ok newTaskList
        | None -> Error (TaskNotFound taskIdToUpdate)


/// <summary>
/// Намагається замінити завдання у списку на оновлене.
/// Повертає новий список з оновленим завданням у Some, якщо завдання з таким ID знайдено.
/// Повертає None, якщо завдання з ID оновлюваного завдання не знайдено у списку.
/// </summary>
/// <param name="taskToUpdate">Завдання, яке потрібно оновити (з новим станом).</param>
/// <param name="taskList">Поточний список завдань.</param>
/// <returns>
/// <c>Some</c> з новим списком, якщо завдання знайдено та замінено;
/// інакше <c>None</c>.
/// </returns>
let tryReplaceTaskInList (taskToUpdate: Task) (taskList: Task list) : Task list option =
    match taskList |> List.tryFind (fun t -> t.Id = taskToUpdate.Id) with
    | Some _ ->
        let newList =
            taskList |> List.map (fun t -> if t.Id = taskToUpdate.Id then taskToUpdate else t)

        Some newList
    | None -> None

/// <summary>
/// Замінює завдання у списку на оновлене.
/// Якщо завдання з таким ID знайдено, повертає новий список з оновленим завданням.
/// Якщо завдання не знайдено, генерує виняток <see cref="TaskNotFoundForUpdateException"/>.
/// </summary>
/// <param name="taskToUpdate">Завдання, яке потрібно оновити (з новим станом).</param>
/// <param name="taskList">Поточний список завдань.</param>
/// <returns>Новий список завдань з оновленим завданням.</returns>
/// <exception cref="TaskNotFoundForUpdateException">
/// Генерується, якщо завдання з ID <paramref name="taskToUpdate"/> не знайдено у списку <paramref name="taskList"/>.
/// </exception>
let replaceTaskInList (taskToUpdate: Task) (taskList: Task list) : Task list =
    match tryReplaceTaskInList taskToUpdate taskList with
        | Some newTaskList -> newTaskList
        | None -> raise (TaskNotFoundForUpdateException taskToUpdate.Id)



// --- Функції для отримання/фільтрації завдань ---

// Отримання завдань за статусом
let getTasksByStatus (status: Status) (taskList: Task list) : Task list =
    taskList |> List.filter (fun task -> task.CurrentStatus = status)

// Отримання завдань за пріоритетом
let getTasksByPriority (priority: Priority) (taskList: Task list) : Task list =
    taskList |> List.filter (fun task -> task.CurrentPriority = priority)

// Отримання завдань за користувачем
let getTasksByUser (userId: UserId) (taskList: Task list) : Task list =
    taskList
    |> List.filter (fun task ->
        match task.AssignedTo with
        | Some assignedId -> if assignedId = userId then true else false
        | None -> false)


// --- Допоміжні та інші функції ---

// Форматування інформації про завдання для виводу
let formatTaskInfo (task: Task) : string =
    let assignedUserStr =
        match task.AssignedTo with
        | Some(UserId userId) -> $"Призначено користувачу: %d{userId}"
        | None -> "Не призначено"

    let projectStr =
        match task.Project with
        | Some projId -> $"Проект: %d{projId}"
        | None -> "Без проекту"

    let descriptionStr =
        match task.Description with
        | Some desc -> $"Опис: %s{desc}"
        | None -> "Опис відсутній"

    sprintf
        "Завдання ID: %d | Назва: %s\n  Статус: %A | Пріоритет: %A\n  %s\n  %s\n  %s\n------------------------------------"
        task.Id
        task.Title
        task.CurrentStatus
        task.CurrentPriority
        descriptionStr
        assignedUserStr
        projectStr

// Отримання кортежа (кількість завдань, список назв)
let getTaskTitlesAndCount (taskList: Task list) : (int * string list) =
    let count = List.length taskList
    let titles = taskList |> List.map (fun task -> task.Title)
    (count, titles)

// Функція для отримання назви завдання
let getTaskTitle (task: Task) : string = task.Title

// Функція для перетворення рядка у верхній регістр
let toUpper (str: string) : string = str.ToUpper()
