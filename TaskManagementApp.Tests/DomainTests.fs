namespace TaskManagementLib.Tests.Domain

open Microsoft.VisualStudio.TestTools.UnitTesting
open FsUnit
open TaskManagementLib.Domain
open System.Collections.Generic

[<TestClass>]
type DomainParsingTests() =

    // --- Тести для Status.FromString ---
    static member StatusTestData: IEnumerable<obj[]> =
        [| [| "new" :> obj; Status.New |]
           [| "InProgress" :> obj; Status.InProgress |]
           [| "DONE" :> obj; Status.Done |]
           [| "blocked" :> obj; Status.Blocked |] |]
        :> IEnumerable<obj[]>

    // Параметризований тест
    [<DataTestMethod>]
    [<DynamicData(nameof (DomainParsingTests.StatusTestData))>]
    member _.``Status_FromString_ValidInputs_ReturnsCorrectStatus``(input: string, expected: Status) =
        let actual = Status.FromString input
        actual |> should equal expected

    [<TestMethod>]
    [<DataRow("invalidStatus")>]
    [<DataRow("UnknownStatus")>]
    [<DataRow("ASDWD")>]
    [<DataRow("")>]
    member _.``Status_FromString_UnknownInput_ReturnsUnknownStatus``(nonExisting: string) =
        let actual = Status.FromString nonExisting
        actual |> should equal Status.UnknownStatus


    // --- Тести для Priority.FromString ---
    static member PriorityTestData: IEnumerable<obj[]> =
        [| [| "low" :> obj; Priority.Low |]
           [| "MEDIUM" :> obj; Priority.Medium |]
           [| "High" :> obj; Priority.High |] |]
        :> IEnumerable<obj[]>

    // Параметризований тест
    [<DataTestMethod>]
    [<DynamicData(nameof (DomainParsingTests.PriorityTestData))>]
    member _.``Priority_FromString_ValidInputs_ReturnsCorrectPriority``(input: string, expected: Priority) =
        let actual = Priority.FromString input
        actual |> should equal expected

    [<TestMethod>]
    [<DataRow("veryHigh")>]
    [<DataRow("veryHigh1231")>]
    [<DataRow("1")>]
    [<DataRow("")>]
    member _.``Priority_FromString_UnknownInput_ReturnsUnknownPriority``(nonExisting: string) =
        let actual = Priority.FromString nonExisting
        actual |> should equal Priority.UnknownPriority


[<TestClass>]
type DomainDisplayableTests() =

    let testUser: User = { Id = UserId 1; Name = "Тест Юзер" }
    let testProject = { Id = 100; Name = "Тест Проєкт" }

    let testTask =
        { Id = 1
          Title = "Тестове Завдання"
          Description = Some "Опис тестового завдання"
          AssignedTo = Some(UserId 1)
          Project = Some 100
          CurrentStatus = Status.New
          CurrentPriority = Priority.High
          Tags = set [ "тест"; "fsharp" ] }

    // --- Тести для User.GetDisplayString ---
    [<TestMethod>]
    member _.``User_GetDisplayString_FormatsCorrectly``() =
        let displayString = (testUser :> IDisplayable).GetDisplayString()
        displayString |> should contain "Тест Юзер"
        displayString |> should contain "(ID: UserId 1)" // Перевірка UserId відображення

    [<TestMethod>] // Другий тест для User.GetDisplayString (можливо, з іншими даними або аспектом)
    member _.``User_GetDisplayString_ContainsNameAndId``() =
        let user: User =
            { Id = UserId 2
              Name = "Інший Користувач" }

        let displayString = (user :> IDisplayable).GetDisplayString()
        displayString |> should startWith "Користувач: Інший Користувач"
        displayString |> should endWith "(ID: UserId 2)"

    // --- Тести для Project.GetDisplayString ---
    [<TestMethod>]
    member _.``Project_GetDisplayString_FormatsCorrectly``() =
        let displayString = (testProject :> IDisplayable).GetDisplayString()
        displayString |> should equal "Проєкт: Тест Проєкт (ID: 100)"

    [<TestMethod>]
    member _.``Project_GetDisplayString_ContainsNameAndId``() =
        let project = { Id = 202; Name = "Важливий Проєкт" }
        let displayString = (project :> IDisplayable).GetDisplayString()
        displayString |> should startWith "Проєкт: Важливий Проєкт"
        displayString |> should endWith "(ID: 202)"


    // --- Тести для Task.GetDisplayString ---
    [<TestMethod>]
    member _.``Task_GetDisplayString_ContainsBasicInfo``() =
        let displayString = (testTask :> IDisplayable).GetDisplayString()
        displayString |> should contain "Тестове Завдання"
        displayString |> should contain "(ID: 1)"
        displayString |> should contain "Статус: New"
        displayString |> should contain "Пріоритет: High"
        displayString |> should contain "Опис: Опис тестового завдання"
        displayString |> should contain "Призначено користувачу ID UserId 1"
        displayString |> should contain "Проєкт ID 100"
        // Важливо, що виводться у алфавітному порядку
        displayString |> should contain "Теги: fsharp, тест"


    [<TestMethod>]
    member _.``Task_GetDisplayString_NoOptionalFields_FormatsCorrectly``() =
        let taskNoOptionals =
            { Id = 2
              Title = "Просте Завдання"
              Description = None
              AssignedTo = None
              Project = None
              CurrentStatus = Status.Done
              CurrentPriority = Priority.Low
              Tags = Set.empty }

        let displayString = (taskNoOptionals :> IDisplayable).GetDisplayString()
        displayString |> should contain "Просте Завдання"
        displayString |> should contain "(ID: 2)"
        displayString |> should contain "Статус: Done"
        displayString |> should contain "Пріоритет: Low"
        displayString |> should not' (contain "Опис:") // Перевірка відсутності
        displayString |> should not' (contain "Призначено користувачу")
        displayString |> should not' (contain "Проєкт ID")
        displayString |> should not' (contain "Теги:") // Якщо Set.empty, тегів не буде
