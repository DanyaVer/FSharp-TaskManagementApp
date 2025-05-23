open System
open System.IO
open TaskAnalyserApp.CsvLoader
open TaskAnalyserApp.Analysers
open TaskManagementLib.Operations
open System.Reflection

// --- Глобальні константи та допоміжні функції ---

/// Сигнал, який користувач може ввести для виходу з програми або пропуску файлу.
let ExitSignal = "-1"

/// Код виходу при успішному завершенні.
let ExitCodeSuccess = 0

/// Код виходу при виникненні помилки або відміні користувачем.
let ExitCodeFailure = 1

let rootDirectory =
    let executableLocation = Assembly.GetExecutingAssembly().Location
    let executableDirectory = Path.GetDirectoryName(executableLocation)

    Path.Combine(executableDirectory, @"..\..\..\") |> Path.GetFullPath // Це нормалізує шлях, обробляючи `..`


/// Допоміжна функція для резолвінгу відносних шляхів відносно кореневої директорії проекту.
/// Це дозволяє користувачу вводити `data/tasks.csv`, а програма знайде його правильно.
let resolvePath (inputPath: string) : string =
    if Path.IsPathRooted(inputPath) then
        // Якщо шлях вже абсолютний, використовуємо його як є
        inputPath
    else
        // Якщо шлях відносний, поєднуємо його з кореневою директорією проекту
        Path.Combine(rootDirectory, inputPath)
        // Path.GetFullPath автоматично нормалізує шлях (вирішує ".\" ".." та уніфікує роздільники)
        |> Path.GetFullPath

/// Перевіряє існування файлу з регістронезалежним порівнянням.
/// Повертає опцію з повним, нормалізованим шляхом, якщо файл знайдено, або None.
let pathExistsCaseInsensitive (filePath: string) : string option =
    if filePath.Length = 0 then
        None
    else
        // Спочатку нормалізуємо шлях. Це вирішує ".\" ".." та уніфікує роздільники.
        let normalizedPath = Path.GetFullPath(filePath)

        if File.Exists(normalizedPath) then
            Some normalizedPath
        else
            let dir = Path.GetDirectoryName(normalizedPath)
            let requestedFileName = Path.GetFileName(normalizedPath)

            // Перевіряємо, чи існує директорія. Якщо ні, то файлу точно немає.
            if String.IsNullOrEmpty(dir) || not (Directory.Exists(dir)) then
                None
            else
                // Перебираємо всі файли в директорії та порівнюємо ім'я файлу регістронезалежно.
                // Використовуємо try/with для обробки можливих помилок доступу (наприклад, System.UnauthorizedAccessException)
                try
                    Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly)
                    |> Seq.tryFind (fun f ->
                        Path.GetFileName(f).Equals(requestedFileName, StringComparison.OrdinalIgnoreCase))
                with
                | :? System.UnauthorizedAccessException ->
                    // Можливо, немає прав доступу до директорії. Обробляємо це як "файл не знайдено".
                    None
                | _ ->
                    // Інші непередбачені помилки.
                    None


// --- Функції взаємодії з користувачем ---

/// Читає валідний шлях до файлу, або повертає None при введенні ExitSignal.
/// Повідомляє користувача про помилки та способи виходу.
let rec promptForFile (label: string) : string option =
    printfn $"{label}"
    printf ">"

    let input = Console.ReadLine().Trim()

    if input = ExitSignal then
        printfn "Вихід за запитом користувача."
        None
    else
        let resolvedPath = resolvePath input

        // Використовуємо нову функцію для регістронезалежного пошуку
        match pathExistsCaseInsensitive resolvedPath with
        | Some path -> Some path
        | None ->
            printfn "" // Додатковий відступ
            printfn $"[ПОМИЛКА] Файл '{input}' не знайдено або до нього немає доступу."
            printfn $"Будь ласка, перевірте шлях та спробуйте ще раз."
            printfn $"Введіть '{ExitSignal}' для виходу з програми."
            printfn "" // Додатковий відступ
            promptForFile label // Рекурсивний виклик для повторного запиту


[<EntryPoint>]
let main argv =
    Console.OutputEncoding <- Text.Encoding.UTF8
    Console.Clear()
    printfn "--- Аналізатор даних завдань з CSV ---"
    printfn "Ця програма завантажує та аналізує дані завдань, користувачів та проектів з CSV-файлів."
    printfn $"Введіть шлях до файлу або '{ExitSignal}' в будь-який момент для виходу."
    printfn "-------------------------------------"
    printfn ""

    // Допоміжна функція для отримання шляху: або з аргументів командного рядка, або через запит користувача.
    let getArgOrPrompt index label =
        if argv.Length > index then
            let rawArg = argv.[index].Trim()

            if rawArg = ExitSignal then
                printfn $"Аргумент [{index}] = '{rawArg}' вказує на вихід."
                None
            else
                let resolvedArgPath = resolvePath rawArg

                match pathExistsCaseInsensitive resolvedArgPath with
                | Some path ->
                    printfn $"Використовую шлях з аргументів: '{path}'"
                    Some path
                | None ->
                    printfn $"[ПОМИЛКА] Аргумент [{index}] = '{rawArg}' не знайдено як валідний файл."
                    printfn $"Будь ласка, введіть шлях вручну або '{ExitSignal}' для виходу."
                    printfn ""
                    promptForFile label // Якщо аргумент невалідний, просимо користувача ввести вручну
        else
            promptForFile label // Якщо аргумент не надано, просимо користувача ввести вручну

    // Отримуємо шляхи до всіх необхідних файлів
    match getArgOrPrompt 0 "Введіть шлях до файлу CSV із завданнями (напр., data/tasks.csv):" with
    | None -> ExitCodeFailure // Користувач вирішив вийти
    | Some tasksFilePath ->
        printfn ""

        match getArgOrPrompt 1 "Введіть шлях до файлу CSV із користувачами (напр., data/users.csv):" with
        | None -> ExitCodeFailure
        | Some usersFilePath ->
            printfn ""

            match getArgOrPrompt 2 "Введіть шлях до файлу CSV із проектами (напр., data/projects.csv):" with
            | None -> ExitCodeFailure
            | Some projectsFilePath ->

                printfn "\nФайли знайдено. Починаю завантаження та аналіз..."

                // Виконуємо асинхронну операцію синхронно
                async {
                    let! loadedData = loadDataAsync tasksFilePath usersFilePath projectsFilePath

                    if
                        List.isEmpty loadedData.Tasks
                        && List.isEmpty loadedData.Users
                        && List.isEmpty loadedData.Projects
                    then
                        printfn
                            "[ПОМИЛКА] Не вдалося завантажити дані з усіх файлів. Перевірте файли та повідомлення про помилки вище."
                        // Додаємо інструкцію, як вийти
                        printfn $"Будь ласка, перевірте, чи файли не порожні та мають правильний формат."
                    else
                        printfn "\n--- Завантажені дані ---"
                        printDisplayableItemSeq loadedData.Projects "Projects"
                        printDisplayableItemSeq loadedData.Users "Users"
                        printDisplayableItemSeq loadedData.Tasks "Tasks"

                        printfn "\n--- Починаємо аналіз ---"
                        do! runAnalysisAsync loadedData
                }
                |> Async.RunSynchronously

                printfn "\n-------------------------------------"
                printfn "Роботу програми завершено. Натисніть Enter для виходу."
                Console.ReadLine() |> ignore // Чекаємо натискання Enter перед закриттям консолі
                ExitCodeSuccess // Успішне завершення
