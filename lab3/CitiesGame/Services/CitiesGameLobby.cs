using Google.Protobuf.WellKnownTypes;

namespace CitiesGame.Services;

using System;
using System.Collections.Generic;
// using System.Timers;

public class CitiesGameLobby
{
    
    int TURN_DURATION_SECONDS = 30;
    private Random randomPlayerSelector;
    public event Action<string> PlayerRemoved; // Событие для уведомления об удалении игрока
    
    public int Id { get; }
    public int Capacity { get;}
    public GameStatus Status { get; set; }
    public List<string> ActivePlayers { get; set; }
    public string CurrentTurnPlayer { get; set; }
    public Timer? CurrentTurnTimer { get; set; }
    public Timestamp CurrentTurnTimerOut { get; set; }
    
    public Timestamp LastChangeTimestamp { get; set; }
    
    public string LastChangeDesc { get; set; }
    
    public string LastPlayedCity { get; set; }

    public CitiesGameLobby(int id, int capacity)
    {
        Id = id;
        Capacity = capacity;
        Status = GameStatus.WaitingForPlayers;
        ActivePlayers = new List<string>();
        CurrentTurnPlayer = "";
        LastChangeTimestamp = Timestamp.FromDateTime(DateTime.UtcNow);
        LastChangeDesc = $"Lobby was created with id {id}";
        LastPlayedCity = "";
        
        randomPlayerSelector = new Random();
    }

    public void StartGame()
    {
        Status = GameStatus.Active;
        LastChangeTimestamp = Timestamp.FromDateTime(DateTime.UtcNow);
        LastChangeDesc = "Game was started";
        PrepareNewTurnState();
    }

    private string GetNewPlayer()
    {
        List<string> availablePlayers = ActivePlayers
            .Where(player => !player.Equals(CurrentTurnPlayer)).ToList();

        int randomIndex = randomPlayerSelector.Next(availablePlayers.Count);
        string newPlayer = availablePlayers[randomIndex];
        LastChangeTimestamp = Timestamp.FromDateTime(DateTime.UtcNow);
        LastChangeDesc = $"The turn goes to {newPlayer}.";
        return newPlayer;
    }

    public void PrepareNewTurnState()
    {
        // Если таймер уже существует, останавливаем и очищаем его
        CurrentTurnTimer?.Dispose(); // Останавливаем предыдущий таймер
        CurrentTurnTimer = null; // Сбрасываем ссылку на предыдущий таймер
        
        string currentTurnPlayer = GetNewPlayer();
        CurrentTurnPlayer = currentTurnPlayer;
        
        // Создаем и настраиваем таймер
        CurrentTurnTimer = new Timer(OnTurnTimerElapsed, null, TURN_DURATION_SECONDS * 1000, Timeout.Infinite);
        
        // Установка времени окончания текущего хода
        CurrentTurnTimerOut = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(TURN_DURATION_SECONDS));
    }
    
    private void OnTurnTimerElapsed(object sender)
    {
        ActivePlayers.Remove(CurrentTurnPlayer);
        string removedPlayer = CurrentTurnPlayer;
        Console.WriteLine($"Player {CurrentTurnPlayer} was removed from game");
        
        LastChangeTimestamp = Timestamp.FromDateTime(DateTime.UtcNow);
        LastChangeDesc = $"Player {CurrentTurnPlayer} was removed from game";
        
        if (ActivePlayers.Count == 1)
        {
            Status = GameStatus.Finished;
            LastChangeTimestamp = Timestamp.FromDateTime(DateTime.UtcNow);
            LastChangeDesc = $"Game was ended.\nPlayer {ActivePlayers[0]} win!";
            PlayerRemoved.Invoke(removedPlayer);
            return;
        }
        
        PlayerRemoved.Invoke(removedPlayer);
        PrepareNewTurnState();
    }
}