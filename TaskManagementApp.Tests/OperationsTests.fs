namespace TaskManagementLib.Tests.Operations

open Microsoft.VisualStudio.TestTools.UnitTesting
open FsUnit
open TaskManagementLib.Domain
open TaskManagementLib
open System.Collections.Generic

[<TestClass>]
type TaskCreationAndUpdateTests() =

    let baseTask = Operations.createTask 1 "Базове завдання"

    // --- Тести для createTask ---
    [<TestMethod>]
    member _.``createTask_InitializesWithDefaults``() =
        let task = Operations.createTask 10 "Нове завдання"
        task.Id |> should equal 10
        task.Title |> should equal "Нове завдання"
        task.Description |> Option.isNone |> should equal true
        task.AssignedTo |> should equal None
        task.Project |> should equal None
        task.CurrentStatus |> should equal Status.New
        task.CurrentPriority |> should equal Priority.Medium
        // Не можна використовувати equal Set.Empty, бо всі seq порівнюються як об'єкти, а вони різні
        task.Tags |> should be Empty

    [<TestMethod>]
    member _.``createTask_WithDifferentIdAndTitle_AreSetCorrectly``() =
        let task = Operations.createTask 99 "Інше Завдання"
        task.Id |> should equal 99
        task.Title |> should equal "Інше Завдання"
        task.CurrentStatus |> should equal Status.New


    // --- Тести для updateTaskStatus ---
    [<TestMethod>]
    member _.``updateTaskStatus_ChangesStatusCorrectly``() =
        let updatedTask = Operations.updateTaskStatus Status.InProgress baseTask
        updatedTask.CurrentStatus |> should equal Status.InProgress
        updatedTask.Id |> should equal baseTask.Id // Перевірка, що інші поля не змінились

    [<TestMethod>]
    member _.``updateTaskStatus_ToDone_SetsDone``() =
        let updatedTask = Operations.updateTaskStatus Status.Done baseTask
        updatedTask.CurrentStatus |> should equal Status.Done
        updatedTask.Title |> should equal baseTask.Title


    // --- Тести для updateTaskPriority ---
    [<TestMethod>]
    member _.``updateTaskPriority_ChangesPriorityCorrectly``() =
        let updatedTask = Operations.updateTaskPriority Priority.High baseTask
        updatedTask.CurrentPriority |> should equal Priority.High
        updatedTask.CurrentStatus |> should equal baseTask.CurrentStatus

    [<TestMethod>]
    member _.``updateTaskPriority_ToLow_SetsLow``() =
        let updatedTask = Operations.updateTaskPriority Priority.Low baseTask
        updatedTask.CurrentPriority |> should equal Priority.Low


    // --- Тести для addTagToTask ---
    [<TestMethod>]
    member _.``addTagToTask_AddsNewTag``() =
        let taskWithTag = Operations.addTagToTask "новий_тег" baseTask
        taskWithTag.Tags |> should contain "новий_тег"
        Set.count taskWithTag.Tags |> should equal (Set.count baseTask.Tags + 1)

    [<TestMethod>]
    member _.``addTagToTask_AddingExistingTag_DoesNotDuplicate``() =
        let taskOnce = Operations.addTagToTask "тег1" baseTask
        let taskTwice = Operations.addTagToTask "тег1" taskOnce // Додаємо той самий тег
        taskTwice.Tags |> should contain "тег1"
        Set.count taskTwice.Tags |> should equal (Set.count taskOnce.Tags) // Кількість не має збільшитись


    // --- Тести для removeTagFromTask ---
    [<TestMethod>]
    member _.``removeTagFromTask_RemovesExistingTag``() =
        let taskWithTag = Operations.addTagToTask "тег_до_видалення" baseTask
        let taskWithoutTag = Operations.removeTagFromTask "тег_до_видалення" taskWithTag
        taskWithoutTag.Tags |> should not' (contain "тег_до_видалення")
        Set.count taskWithoutTag.Tags |> should equal (Set.count baseTask.Tags)

    [<TestMethod>]
    member _.``removeTagFromTask_RemovingNonExistingTag_DoesNothing``() =
        let taskAfterAttemptedRemove =
            Operations.removeTagFromTask "неіснуючий_тег" baseTask

        taskAfterAttemptedRemove.Tags |> should equal baseTask.Tags // Набір тегів не має змінитись

        Set.count taskAfterAttemptedRemove.Tags
        |> should equal (Set.count baseTask.Tags)


[<TestClass>]
type OperationsHelperFunctionsTests() =

    // --- Тести для priorityToScore ---
    static member PriorityScoreTestData: IEnumerable<obj[]> =
        [| [| Priority.Low :> obj; 1 |]
           [| Priority.Medium :> obj; 2 |]
           [| Priority.High :> obj; 3 |]
           [| Priority.UnknownPriority :> obj; 0 |] |]
        :> IEnumerable<obj[]>

    [<DataTestMethod>]
    [<DynamicData(nameof (OperationsHelperFunctionsTests.PriorityScoreTestData))>]
    member _.``priorityToScore_ReturnsCorrectScore``(priority: Priority, expectedScore: int) =
        let actualScore = Operations.priorityToScore priority
        actualScore |> should equal expectedScore

    [<TestMethod>]
    member _.``priorityToScore_ForUnknownPriority_IsZero``() =
        Operations.priorityToScore Priority.UnknownPriority |> should equal 0



[<TestClass>]
type OperationsCollectionTests() =

    let task1 =
        (Operations.createTask 1 "Task 1"
         |> Operations.updateTaskStatus Status.New
         |> Operations.addTagToTask "tagA")

    let task2 =
        (Operations.createTask 2 "Task 2"
         |> Operations.updateTaskStatus Status.InProgress
         |> Operations.addTagToTask "tagB")

    let task3 =
        (Operations.createTask 3 "Task 3"
         |> Operations.updateTaskStatus Status.New
         |> Operations.addTagToTask "tagA")

    let taskMap = Map.ofList [ (1, task1); (2, task2); (3, task3) ]

    // --- Тести для getTasksByStatus ---
    [<TestMethod>]
    member _.``getTasksByStatus_ReturnsCorrectTasks``() =
        let newTasks = Operations.getTasksByStatus Status.New taskMap
        newTasks |> should haveLength 2
        newTasks |> should contain task1
        newTasks |> should contain task3
        newTasks |> should not' (contain task2)

    [<TestMethod>]
    member _.``getTasksByStatus_NonExistingStatus_ReturnsEmptyList``() =
        let doneTasks = Operations.getTasksByStatus Status.Done taskMap
        doneTasks |> should be Empty


    // --- Тести для getTasksByTag ---
    [<TestMethod>]
    member _.``getTasksByTag_ReturnsCorrectTasks``() =
        let tagATasks = Operations.getTasksByTag "tagA" taskMap
        tagATasks |> should haveLength 2
        tagATasks |> should contain task1
        tagATasks |> should contain task3

    [<TestMethod>]
    member _.``getTasksByTag_NonExistingTag_ReturnsEmptyList``() =
        let unknownTagTasks = Operations.getTasksByTag "неіснуючий_тег" taskMap
        unknownTagTasks |> should be Empty




    
//[<TestClass>]
//type OperationsGenericsTests() =

//    // --- Тести для tryFindById (узагальнена функція) ---
//    type TestEntity = { Id: int; Value: string }

//    let entities =
//        [ { Id = 1; Value = "A" }; { Id = 2; Value = "B" }; { Id = 3; Value = "C" } ]
//        |> List.toSeq

//    [<TestMethod>]
//    member _.``tryFindById_FindsExistingEntity_ReturnsSome``() =
//        let result = Operations.tryFindById 2 entities
//        result |> should equal (Some { Id = 2; Value = "B" })

//    [<TestMethod>]
//    member _.``tryFindById_NonExistingEntity_ReturnsNone``() =
//        let result = Operations.tryFindById 4 entities
//        result |> should equal None
