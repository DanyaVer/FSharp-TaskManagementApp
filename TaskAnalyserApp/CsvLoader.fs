module TaskAnalyserApp.CsvLoader

open System
open FSharp.Data
open TaskManagementLib.Domain
open Helpers

type TasksCsv =
    CsvProvider<
        "./Data/Tasks.csv",
        Schema="Id (int), Title, Description, AssignedTo_UserId (int option), Project_Id (int option), Status, Priority, Tags",
        HasHeaders=true,
        Separators=","
     >

type UsersCsv = CsvProvider<"./Data/Users.csv", Schema="UserId (int), Name", HasHeaders=true, Separators=",">
type ProjectsCsv = CsvProvider<"./Data/Projects.csv", Schema="ProjectId (int), Name", HasHeaders=true, Separators=",">


// Функція для перетворення рядка з CSV (від Type Provider) у наш доменний тип Task
let private csvRowToDomainTask (csvTask: TasksCsv.Row) : Task =
    let status = Status.FromString csvTask.Status

    let priority = Priority.FromString csvTask.Priority

    let tagsSet =
        if String.IsNullOrWhiteSpace(csvTask.Tags) then
            Set.empty
        else
            csvTask.Tags.Split(';') |> Array.map (_.Trim()) |> Set.ofArray

    { Id = csvTask.Id
      Title = csvTask.Title
      Description =
        if String.IsNullOrWhiteSpace(csvTask.Description) then
            None
        else
            Some csvTask.Description
      AssignedTo = csvTask.AssignedTo_UserId |> Option.map UserId
      Project = csvTask.Project_Id // Це вже int option, що відповідає ProjectId
      CurrentStatus = status
      CurrentPriority = priority
      Tags = tagsSet }

// Функція для перетворення рядка з CSV (від Type Provider) у наш доменний тип User
let private csvRowToDomainUser (csvUser: UsersCsv.Row) : User =
    { Id = UserId csvUser.UserId // Огортаємо в UserId
      Name = csvUser.Name }

// Функція для перетворення рядка з CSV (від Type Provider) у наш доменний тип Project
let private csvRowToDomainProject (csvProject: ProjectsCsv.Row) : Project =
    { Id = csvProject.ProjectId
      Name = csvProject.Name }

// Асинхронне завантаження та перетворення даних
// Завантажуємо Task, User, Project
type LoadedData =
    { Tasks: Task list
      Users: User list
      Projects: Project list }

/// Асинхронно завантажує дані з CSV файлів.
let loadDataAsync (tasksFilePath: string) (usersFilePath: string) (projectsFilePath: string) : Async<LoadedData> =
    async {
        try
            // Завантаження завдань
            let! tasksCsvAsync = TasksCsv.AsyncLoad(tasksFilePath)
            let domainTasks = tasksCsvAsync.Rows |> Seq.map csvRowToDomainTask |> List.ofSeq
            printfn "Завдання завантажено: %d" (List.length domainTasks)

            // Завантаження користувачів
            let! usersCsvAsync = UsersCsv.AsyncLoad(usersFilePath)
            let domainUsers = usersCsvAsync.Rows |> Seq.map csvRowToDomainUser |> List.ofSeq
            printfn "Користувачів завантажено: %d" (List.length domainUsers)

            // Завантаження проектів
            let! projectsCsvAsync = ProjectsCsv.AsyncLoad(projectsFilePath)

            let domainProjects =
                projectsCsvAsync.Rows |> Seq.map csvRowToDomainProject |> List.ofSeq

            printfn "Проектів завантажено: %d" (List.length domainProjects)

            return
                { Tasks = domainTasks
                  Users = domainUsers
                  Projects = domainProjects }
        with ex ->
            printfn "Помилка під час завантаження CSV: %s" ex.Message

            return
                { Tasks = []
                  Users = []
                  Projects = [] }
    // ??????????????????
    //reraisePreserveStackTrace ex
    //reraise ()
    // ??????????????????
    }
