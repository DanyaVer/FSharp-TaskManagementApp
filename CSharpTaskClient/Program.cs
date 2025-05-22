// Microsoft.FSharp.Core потрібен для роботи з F# типами як Option, List, Map
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;
using static TaskManagementLib.Domain;
using TaskManagementLib;

namespace CSharpTaskClient;

public class Program
{
    /// <summary>
    /// Допоміжна функція для обробки F# Option в C#
    /// </summary>
    public static T? UnwrapOption<T>(FSharpOption<T> option, T? defaultValue = default)
    {
        return FSharpOption<T>.get_IsSome(option) ? option.Value : defaultValue;
    }

    /// <summary>
    /// Допоміжна функція для обробки нашого F# OperationResult в C#
    /// </summary>
    public static void HandleFSharpOperationResult<TSuccess, TError>(
        string operationName,
        OperationResult<TSuccess, TError> result,
        Action<TSuccess> onSuccess,
        Action<TError> onFailure)
    {
        if (result is OperationResult<TSuccess, TError>.Success successResult)
        {
            Console.WriteLine($"C#: УСПІХ ({operationName}): Операція виконана.");
            onSuccess(successResult.Item);
        }
        else if (result is OperationResult<TSuccess, TError>.Failure failureResult)
        {
            Console.WriteLine($"C#: ПОМИЛКА ({operationName}): {failureResult.Item}");
            onFailure(failureResult.Item);
        }
    }


    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("--- C# клієнт для F# Task Management ---");

        // Використання F# нативних типів
        Console.WriteLine("\n--- Демонстрація F# типів у C# ---");

        // Створення F# запису User
        var userId1 = UserId.NewUserId(1); // Використовуємо фабричний метод NewUserId для DU з одним кейсом
        var fsharpUser1 = new User(userId1, "Катерина з C#");
        Console.WriteLine($"Створено F# User: ID={UnwrapOption(fsharpUser1.Id.Item, -1)}, Name={fsharpUser1.Name}");

        // Створення F# розміченого об'єднання Status та Priority
        var statusInProgress = Status.InProgress; // Доступ до кейсів DU
        var priorityHigh = Priority.High;
        Console.WriteLine($"F# Status: {statusInProgress}, F# Priority: {priorityHigh}");

        // Використання F# функцій
        Console.WriteLine("\n--- Демонстрація F# функцій у C# ---");
        var task1Id = 101;
        // Виклик F# функції createTask
        var fsharpTask1 = Operations.createTask(task1Id, "Завдання з C#");
        Console.WriteLine($"Створено F# Task через функцію: ID={fsharpTask1.Id}, Title='{fsharpTask1.Title}'");

        // Оновлення статусу завдання
        var updatedTask1 = Operations.updateTaskStatus(statusInProgress, fsharpTask1);
        updatedTask1 = Operations.updateTaskPriority(priorityHigh, updatedTask1);
        updatedTask1 = Operations.addTagToTask("csharp_tag", updatedTask1);

        // Використання функції, що приймає інтерфейс IDisplayable
        // F# Task реалізує IDisplayable
        Operations.printDisplayableItem(updatedTask1);
        // F# User реалізує IDisplayable
        Operations.printDisplayableItem(fsharpUser1); 

        // Створення F# колекції (наприклад, списку) для передачі у F# функцію
        var taskList = ListModule.OfArray([updatedTask1, Operations.createTask(102, "Інше завдання")]);
        var highPriorityTasks = Operations.getTasksByPriority(
            priorityHigh, 
            MapModule.OfSeq(taskList.Select(t => new Tuple<int, Domain.Task>(t.Id, t)))); // Потрібно передати Map
        Console.WriteLine($"Знайдено {highPriorityTasks.Length} завдань з високим пріоритетом:");
        foreach (var task in highPriorityTasks)
            Operations.printDisplayableItem(task);


        // Використання F# класу TaskManagerService
        Console.WriteLine("\n--- Демонстрація F# класу TaskManagerService у C# ---");
        // Створення екземпляра F# класу
        var taskService = new TaskManagerService.TaskManagerService();
        Console.WriteLine($"Створено F# TaskManagerService. Початкова кількість завдань: {taskService.TaskCount}");

        // Додавання завдання через метод класу
        var serviceTask1 = taskService.AddTask(
            201,
            "Завдання з сервісу C#",
            FSharpOption<string>.Some("Детальний опис для завдання з сервісу"),
            FSharpOption<Priority>.Some(Priority.Medium),
            FSharpOption<Status>.None
        );
        Console.WriteLine($"Додано завдання через сервіс: ID={serviceTask1.Id}, Title='{serviceTask1.Title}'");
        Console.WriteLine($"Кількість завдань у сервісі: {taskService.TaskCount}");

        // Отримання завдання за ID з сервісу
        var retrievedTaskOpt = taskService.TryGetTaskById(201);
        if (FSharpOption<Domain.Task>.get_IsSome(retrievedTaskOpt))
        {
            Console.WriteLine($"Отримано завдання з сервісу: {retrievedTaskOpt.Value.Title}");
            Operations.printDisplayableItem(retrievedTaskOpt.Value);
        }
        else
        {
            Console.WriteLine("Завдання 201 не знайдено в сервісі.");
        }

        // Оновлення завдання через сервіс
        // Створення F# функції (лямбди) з C#
        var csharpUpdateFn = FuncConvert.FromFunc<Domain.Task, Domain.Task>(t =>
            new Domain.Task(t.Id, t.Title, t.Description, t.AssignedTo, t.Project, Status.Done, t.CurrentPriority, t.Tags)
        );
        var updateServiceResult = taskService.UpdateTask(201, csharpUpdateFn);

        HandleFSharpOperationResult("Оновлення завдання 201 через сервіс", updateServiceResult,
            _ =>
            { // onSuccess (unit -> void)
                var updatedServiceTaskOpt = taskService.TryGetTaskById(201);
                if (FSharpOption<Domain.Task>.get_IsSome(updatedServiceTaskOpt))
                    Operations.printDisplayableItem(updatedServiceTaskOpt.Value);
            },
            error => Console.WriteLine($"C#: Помилка оновлення: {error}") // onError
        );


        // Отримання всіх завдань з сервісу
        Console.WriteLine("Всі завдання з TaskManagerService:");
        var allServiceTasks = taskService.AllTasks; // Це Seq<Task> = IEnumerable<Task>
        foreach (var task in allServiceTasks)
            Operations.printDisplayableItem(task); // Використовуємо F# функцію для виводу

        Console.WriteLine("\n--- Завершення роботи C# клієнта ---");
    }
}