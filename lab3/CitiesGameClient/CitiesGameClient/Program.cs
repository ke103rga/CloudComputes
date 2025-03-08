using Grpc.Net.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using CitiesGameClient;
using Google.Protobuf.WellKnownTypes;

class Program
{
    private static string playerName;
    private static GrpcChannel channel;
    private static CitiesGame.CitiesGameClient client;
    private static CancellationTokenSource cancellationTokenSource;
    private static Timer turnTimer;

    private static DateTime turnExpiryTime;
    private static string lastPlayedCity = "";

    private static Task gameStateTask;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Добро пожаловать в игру 'Города'!");

        // Подключаемся к gRPC серверу
        await ConnectToServer();

        // Отправляем запрос Connect и запускаем обработку состояния игры
        await StartGame();
        
        await gameStateTask; // Ожидание фоновой задачи

        Console.ReadLine();
    }

    private static async Task ConnectToServer()
    {
        Console.Write("Введите URL сервера (например, http://localhost:5000): ");
        var serverUrl = Console.ReadLine();
        serverUrl = "http://localhost:5257";
        channel = GrpcChannel.ForAddress(serverUrl);
        client = new CitiesGame.CitiesGameClient(channel);
    }

    private static async Task StartGame()
    {
        // Получаем имя игрока
        do
        {
            Console.Write("Введите ваше имя: ");
            playerName = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(playerName));

        // Запрашиваем вместимость лобби
        int lobbyCapacity;
        do
        {
            Console.Write("Введите вместительность лобби (от 3 до 5): ");
        } while (!int.TryParse(Console.ReadLine(), out lobbyCapacity) || (lobbyCapacity < 2 || lobbyCapacity > 5));

        // Отправляем запрос на подключение
        ConnectionResponse connectionResponse;
        do
        {
            connectionResponse = await client.ConnectAsync(new ConnectionRequest
            {
                PlayerName = playerName,
                LobbyCapacity = lobbyCapacity
            });

            if (connectionResponse.ConnectionStatus == "Failed")
            {
                Console.WriteLine($"Ошибка: {connectionResponse.ErrorMessage}. Пожалуйста, придумайте новое имя.");
                playerName = Console.ReadLine();
            }
        } while (connectionResponse.ConnectionStatus == "Failed");

        Console.WriteLine("Вы успешно подключены к игре!\n Ожидаем присоединения остальных игроков");

        // Запускаем обновление состояния игры
        cancellationTokenSource = new CancellationTokenSource();
        gameStateTask = Task.Run(() => GetGameStatePeriodically(cancellationTokenSource.Token));
    }

    private static async Task ProcessPlayerTurn(GameStateResponse gameStateResponse)
    {  
        Console.WriteLine("\nТеперь ваш ход!");
        char newWordFirstsLetter = '\0';
        Timestamp timerOutTime = gameStateResponse.TimerOutTime;
        
        string lastPlayedCity = gameStateResponse.LastPlayedCity;
        if (lastPlayedCity != "")
        {
            newWordFirstsLetter = lastPlayedCity[^1];
            if (newWordFirstsLetter == 'ь') newWordFirstsLetter = lastPlayedCity[^2];
            Console.WriteLine($"Последнее сыгранное слово: {lastPlayedCity}");
            Console.WriteLine($"Назовите город на букву: {newWordFirstsLetter} до {timerOutTime}");
        }
        else
        {
            Console.WriteLine($"Назовите любой город до {timerOutTime}");
        }
        StartTurnTimer(timerOutTime);
        
        Console.WriteLine("Введите название города:");
        while (true) // Ожидаем правильный ввод города
        {
            string cityName = Console.ReadLine();
            string errorMessage = "";

            if (!CheckCityName(cityName, newWordFirstsLetter,  out errorMessage))
            {
                Console.WriteLine($"Ошибка: {errorMessage}. Пожалуйста, введите новое название города.");
                continue;
            }
            cityName = cityName.ToLower();

            // Отправляем ход на сервер
            var turnResponse = await client.PlayTurnAsync(new TurnRequest
            {
                PlayerName = playerName,
                CityName = cityName
            });

            if (turnResponse.TurnIsCorrect)
            {
                Console.WriteLine("Ход принят, ожидаем следующего игрока...");
                turnTimer.Change(Timeout.Infinite, Timeout.Infinite); // Останавливаем таймер
                break; // Выход из внутреннего цикла
            }
            else
            {
                Console.WriteLine($"Ошибка: {turnResponse.ErrorMessage}. Пожалуйста, введите новое название города.");
            }
        }
        
    }

    private static bool CheckCityName(string cityName, char newWordFirstsLetter, out string errorMessage)
    {
        errorMessage = "";
        if (cityName.Length == 0)
        {
            errorMessage = $"Название города не должно быть пустой строкой";
            return false;
        }
        if (newWordFirstsLetter != '\0' & newWordFirstsLetter != cityName.ToLower()[0])
        {
            errorMessage = $"Название города должно начинаться с {newWordFirstsLetter}";
            return false;
        }
        return true;
    }

    private static async Task GetGameStatePeriodically(CancellationToken cancellationToken)
    {
        GameStateResponse lastGameState = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            var gameStateResponse = await client.GetGameStateAsync(new PlayerRequest { PlayerName = playerName });

            if (lastGameState?.LastChangeTime != gameStateResponse.LastChangeTime)
            {
                Console.WriteLine(gameStateResponse.LastChangeDesc); // Выводим изменение
                lastGameState = gameStateResponse;

                // Проверка, если теперь ваш ход
                if (gameStateResponse.CurrentTurnPlayer == playerName)
                {
                    await ProcessPlayerTurn(gameStateResponse);
                }
                
                // Проверка завершена ли игра
                List<string> activePlayers = gameStateResponse.ActivePlayers.Split(", ").ToList();
                string gameStatus = gameStateResponse.GameStatus;
                if (activePlayers.Count == 1 & gameStatus == "Finished" & activePlayers[0] == playerName)
                {
                    Console.WriteLine("Вы выиграли!!!");
                    cancellationTokenSource.Cancel(); // Остановка отправки GetGameState   
                }
            }

            await Task.Delay(10000); // Задержка в 1 секунду перед следующим запросом
        }
    }

    private static void StartTurnTimer(Timestamp turnExpiryTime)
    {
        DateTime turnExpireDateTime = turnExpiryTime.ToDateTime();
        
        // Рассчитываем время до окончания
        var timeSpan = (turnExpireDateTime - DateTime.UtcNow);
        
        // Если время уже истекло, вызываем метод сразу
        if (timeSpan.TotalMilliseconds <= 0)
        {
            TurnExpired(null);
            return;
        }

        // Создаем и настраиваем таймер
        turnTimer = new Timer(TurnExpired, null, (int)timeSpan.TotalMilliseconds, Timeout.Infinite);
    }

    private static void TurnExpired(object state)
    {
        Console.WriteLine("Время вышло! Вы проиграли.");
        cancellationTokenSource.Cancel(); // Остановка отправки GetGameState
        // Здесь можно предложить игроку начать новую игру или повторное подключение.
    }
}