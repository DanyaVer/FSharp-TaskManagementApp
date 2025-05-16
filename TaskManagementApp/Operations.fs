module TaskManagementApp.Operations

open TaskManagementApp.Domain

/// <summary>
/// Узагальнена функція для пошуку сутності за її ID у колекції.
/// Сутність повинна мати властивість 'Id'.
/// </summary>
let inline tryFindById<'TEntity, 'TId when 'TEntity: (member Id: 'TId) and 'TId: equality>
    (idToFind: 'TId)
    (collection: seq<'TEntity>)
    : 'TEntity option =
    collection |> Seq.tryFind (fun item -> item.Id = idToFind)

// --- Функції для створення та оновлення завдань ---

// Створення нового завдання
let createTask (id: TaskId) (title: string) : Task =
    { Id = id
      Title = title
      Description = None
      AssignedTo = None
      Project = None
      CurrentStatus = New
      CurrentPriority = Medium
      Tags = Set.empty }

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

// Додавання тегу до завдання
let addTagToTask (tag: string) (task: Task) : Task =
    { task with
        Tags = Set.add tag task.Tags }

// Видалення тегу з завдання
let removeTagFromTask (tag: string) (task: Task) : Task =
    { task with
        Tags = Set.remove tag task.Tags }


// --- Функції для роботи з колекцією завдань ---

/// <summary>
/// Додає завдання до колекції Map. Повертає оновлену колекцію.
/// Якщо завдання з таким ID вже існує, воно буде замінене.
/// </summary>
let addTaskToCollection (task: Task) (taskMap: Map<TaskId, Task>) : Map<TaskId, Task> = Map.add task.Id task taskMap

/// <summary>
/// Видаляє завдання з колекції Map. Повертає OperationResult.
/// </summary>
let removeTaskFromCollection
    (taskId: TaskId)
    (taskMap: Map<TaskId, Task>)
    : OperationResult<Map<TaskId, Task>, OperationError> =
    match Map.containsKey taskId taskMap with
    | true -> Success(Map.remove taskId taskMap)
    | false -> Failure(TaskNotFound taskId)

/// <summary>
/// Отримує завдання з колекції Map за ID. Використовує Option.
/// </summary>
let getTaskFromCollectionById (taskId: TaskId) (taskMap: Map<TaskId, Task>) : Task option = Map.tryFind taskId taskMap

// Оновлює завдання у списку і повертає новий список
let tryUpdateTaskInMap
    (taskIdToUpdate: TaskId)
    (updateFn: Task -> Task)
    (taskMap: Map<TaskId, Task>)
    : OperationResult<Map<TaskId, Task>, OperationError> =
    match Map.tryFind taskIdToUpdate taskMap with
    | Some taskToUpdate ->
        let updatedTask = updateFn taskToUpdate

        if updatedTask.Id <> taskIdToUpdate then
            Failure(ValidationError "Функція оновлення не повинна змінювати ID завдання.")
        else
            Success(Map.add updatedTask.Id updatedTask taskMap) // Замінює існуюче
    | None -> Failure(TaskNotFound taskIdToUpdate)

// Оновлює завдання, якщо існує, і повертає оновлений список
let updateTaskInMap (taskIdToUpdate: TaskId) (updateFn: Task -> Task) (taskMap: Map<TaskId, Task>) : Map<TaskId, Task> =
    match tryUpdateTaskInMap taskIdToUpdate updateFn taskMap with
    | Success newTaskMap -> newTaskMap
    | Failure(TaskNotFound id) -> raise (TaskNotFoundForUpdateException id)
    | Failure(ValidationError msg) -> failwith $"Помилка валідації при оновленні завдання з ID {taskIdToUpdate}: {msg}"
    | Failure error -> failwith $"Помилка при оновленні завдання з ID {taskIdToUpdate}: {error}"

/// <summary>
/// Намагається замінити завдання у списку на оновлене.
/// Повертає новий список з оновленим завданням у Some, якщо завдання з таким ID знайдено.
/// Повертає None, якщо завдання з ID оновлюваного завдання не знайдено у списку.
/// </summary>
/// <param name="taskToUpdate">Завдання, яке потрібно оновити (з новим станом).</param>
/// <param name="taskMap">Поточний список завдань.</param>
/// <returns>
/// <c>Some</c> з новим списком, якщо завдання знайдено та замінено;
/// інакше <c>None</c>.
/// </returns>
let tryReplaceTaskInMap
    (taskToUpdate: Task)
    (taskMap: Map<TaskId, Task>)
    : OperationResult<Map<TaskId, Task>, OperationError> =
    match Map.tryFind taskToUpdate.Id taskMap with
    | Some _ -> Success(Map.add taskToUpdate.Id taskToUpdate taskMap) // Замінює існуюче
    | None -> Failure(TaskNotFound taskToUpdate.Id)

/// <summary>
/// Замінює завдання у списку на оновлене.
/// Якщо завдання з таким ID знайдено, повертає новий список з оновленим завданням.
/// Якщо завдання не знайдено, генерує виняток <see cref="TaskNotFoundForUpdateException"/>.
/// </summary>
/// <param name="taskToUpdate">Завдання, яке потрібно оновити (з новим станом).</param>
/// <param name="taskMap">Поточний список завдань.</param>
/// <returns>Новий список завдань з оновленим завданням.</returns>
/// <exception cref="TaskNotFoundForUpdateException">
/// Генерується, якщо завдання з ID <paramref name="taskToUpdate"/> не знайдено у списку <paramref name="taskMap"/>.
/// </exception>
let replaceTaskInMap (taskToUpdate: Task) (taskMap: Map<TaskId, Task>) : Map<TaskId, Task> =
    match tryReplaceTaskInMap taskToUpdate taskMap with
    | Success newTaskMap -> newTaskMap
    | Failure(TaskNotFound id) -> raise (TaskNotFoundForUpdateException taskToUpdate.Id)
    | Failure error -> failwith $"Помилка при оновленні завдання з ID {taskToUpdate.Id}: {error}"


// --- Функції для отримання/фільтрації завдань ---

// Отримання всіх завдань як список
let getAllTasks (taskMap: Map<TaskId, Task>) : Task list = taskMap |> Map.toList |> List.map snd

let getTasksByFilter (filter: Task -> bool) (taskMap: Map<TaskId, Task>) : Task list =
    taskMap |> Map.values |> Seq.filter filter |> List.ofSeq

// Отримання завдань за статусом
let getTasksByStatus (status: Status) (taskMap: Map<TaskId, Task>) : Task list =
    getTasksByFilter (fun task -> task.CurrentStatus = status) taskMap

// Отримання завдань за пріоритетом
let getTasksByPriority (priority: Priority) (taskMap: Map<TaskId, Task>) : Task list =
    getTasksByFilter (fun task -> task.CurrentPriority = priority) taskMap

// Отримання завдань за користувачем
let getTasksByUser (userId: UserId) (taskMap: Map<TaskId, Task>) : Task list =
    getTasksByFilter
        (fun task ->
            match task.AssignedTo with
            | Some assignedId -> if assignedId = userId then true else false
            | None -> false)
        taskMap

// Отримання завдань за тегом
let getTasksByTag (tag: string) (taskMap: Map<TaskId, Task>) : Task list =
    getTasksByFilter (fun task -> Set.contains tag task.Tags) taskMap


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

    let tagsStr =
        if Set.isEmpty task.Tags then
            "Теги відсутні"
        else
            "Теги: " + (task.Tags |> Set.toList |> String.concat ", ")

    sprintf
        "Завдання ID: %d | Назва: %s\n  Статус: %A | Пріоритет: %A\n  %s\n  %s\n  %s\n  %s\n-------------------------------"
        task.Id
        task.Title
        task.CurrentStatus
        task.CurrentPriority
        descriptionStr
        assignedUserStr
        projectStr
        tagsStr

// Отримання кортежа (кількість завдань, список назв)
let getTaskTitlesAndCount (taskMap: Map<TaskId, Task>) : (int * string list) =
    let count = Map.count taskMap
    let titles = taskMap |> Map.values |> Seq.map (fun task -> task.Title) |> List.ofSeq
    (count, titles)

// Функція для отримання назви завдання
let getTaskTitle (task: Task) : string = task.Title

// Функція для перетворення рядка у верхній регістр
let toUpper (str: string) : string = str.ToUpper()

// Допоміжна функція для виведення інформації за допомогою interface IDisplayable
let printDisplayableItem (item: IDisplayable) =
    printfn "Інформація з IDisplayable: %s" (item.GetDisplayString())
