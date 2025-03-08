using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;

namespace CitiesGame.Services;


using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;

public class CitiesGameServer : CitiesGame.CitiesGameBase
{
    private readonly object _lock = new object();
    private readonly ILogger<CitiesGameServer> _logger; // Логгер
    private CitiesChecker _citiesChecker = new CitiesChecker();
    private HashSet<string> ActivePlayers { get; set; }
    private LobbiesCollection ActiveLobbies { get; set; }
    private Dictionary<string, int> PlayersToLobbies { get; set; }

    public CitiesGameServer(ILogger<CitiesGameServer> logger)
    {
        _logger = logger;   
        ActivePlayers = new HashSet<string>();
        ActiveLobbies = new LobbiesCollection();
        PlayersToLobbies = new Dictionary<string, int>();
    }
    
    private void OnPlayerRemoved(string playerName)
    {
        lock (_lock)
        {
            // Удаление игрока из списка активных игроков сервера
            ActivePlayers.Remove(playerName);
            // Также удаляем из PlayersToLobbies
            PlayersToLobbies.TryGetValue(playerName, out var lobbyId);
            PlayersToLobbies.Remove(playerName);

            // CitiesGameLobby lobby = ActiveLobbies.ActiveLobbies[lobbyId];
            // if (lobby.Status == GameStatus.Finished)
            // {
            //     ActiveLobbies.RemoveLobby(lobbyId);
            //     _citiesChecker.RemoveLobbyPlayedCities(lobbyId);
            // }
            
            // Здесь можно также добавить логику для проверки статуса игры и обновления состояния
            _logger.LogInformation($"Player {playerName} was removed from lobby {lobbyId}."); // Логирование удаления игрока
        }
    }

    public override Task<ConnectionResponse> Connect(ConnectionRequest request, ServerCallContext context)
    {
        lock (_lock)
        {
            if (ActivePlayers.Contains(request.PlayerName))
            {
                _logger.LogWarning("Attempt of connection by {PlayerName}, which is already in game.", request.PlayerName);
                return Task.FromResult(new ConnectionResponse
                {
                    ConnectionStatus = "Failed",
                    ErrorMessage = "Player already active."
                });
            }

            // Найти неполное лобби или создать новое
            CitiesGameLobby lobby = ActiveLobbies.FindOrCreateLobby(request.LobbyCapacity);

            lobby.ActivePlayers.Add(request.PlayerName);
            PlayersToLobbies[request.PlayerName] = lobby.Id;
            ActivePlayers.Add(request.PlayerName);
            _logger.LogInformation("Player {PlayerName} connected to lobby {LobbyId}.", request.PlayerName, lobby.Id);
        
            if (lobby.ActivePlayers.Count == lobby.Capacity)
            {
                StartGame(lobby);
                ActiveLobbies.MakeLobbyActive(lobby.Id);
                _logger.LogInformation($"Game started in lobby {lobby.Id}."); // Логируем начало игры
                // Подписка на событие удаления игрока
                lobby.PlayerRemoved += OnPlayerRemoved;
            }

            ActivePlayers.Add(request.PlayerName);

            return Task.FromResult(new ConnectionResponse
            {
                ConnectionStatus = "Success",
                ErrorMessage = ""
            }); 
        }
    }

    private void StartGame(CitiesGameLobby lobby)
    {
        lock (_lock)
        {
            lobby.StartGame();
            _citiesChecker.AddLobbyPlayedCities(lobby.Id);
        }
    }

    public override Task<GameStateResponse> GetGameState(PlayerRequest request, ServerCallContext context)
    {
        lock (_lock)
        {
            if (!PlayersToLobbies.TryGetValue(request.PlayerName, out var lobbyId))
            {
                _logger.LogWarning("Player {PlayerName} send GetGameState request but he isn't inside lobby.", request.PlayerName);
                return Task.FromResult(new GameStateResponse
                {
                    GameStatus = "Error",
                    ActivePlayers = "",
                    CurrentTurnPlayer = "",
                    TimerOutTime = Timestamp.FromDateTime(DateTime.UtcNow),
                    LastChangeTime = Timestamp.FromDateTime(DateTime.UtcNow),
                    LastChangeDesc = "Player not found.",
                    LastPlayedCity = ""
                });
            }

            // Формируем ответ с состоянием игры
            if (ActiveLobbies.ActiveLobbies.TryGetValue(lobbyId, out var lobby))
            {
                if (lobby.ActivePlayers.Count == 1)
                {
                    ActiveLobbies.RemoveLobby(lobbyId);
                    _citiesChecker.RemoveLobbyPlayedCities(lobbyId);  
                }
            }
            else ActiveLobbies.NotFullLobbies.TryGetValue(lobbyId, out lobby);
        
            _logger.LogInformation("Request GetGameState from player {PlayerName} from lobby {LobbyId}.", request.PlayerName, lobbyId);
            return Task.FromResult(new GameStateResponse
            {
                GameStatus = lobby.Status.ToString(),
                ActivePlayers = string.Join(", ", lobby.ActivePlayers),
                CurrentTurnPlayer = lobby.CurrentTurnPlayer,
                TimerOutTime = lobby.CurrentTurnTimerOut,
                LastChangeTime = lobby.LastChangeTimestamp,
                LastChangeDesc = lobby.LastChangeDesc,
                LastPlayedCity = lobby.LastPlayedCity
            });
        }
    }

    public override Task<TurnResponse> PlayTurn(TurnRequest request, ServerCallContext context)
    {
        lock (_lock)
        {
            if (!PlayersToLobbies.TryGetValue(request.PlayerName, out var lobbyId) ||
                !ActiveLobbies.ActiveLobbies.TryGetValue(lobbyId, out var lobby))
            {
                return Task.FromResult(new TurnResponse()
                {
                    TurnIsCorrect = false,
                    ErrorMessage = "Player have already eliminated from the game"
                });
            }

            bool turnIsCorrect = _citiesChecker.IsCityValid(request.CityName, lobbyId, out string errorMessage);
            if (turnIsCorrect)
            {
                lobby.LastPlayedCity = request.CityName;
                lobby.PrepareNewTurnState();
                _logger.LogInformation($"Player {request.PlayerName} from lobby {lobbyId} played correct turn with city {request.CityName}.");
            }
            else
            {
                _logger.LogWarning($"Player {request.PlayerName} from lobby {lobbyId} made incorrect turn {errorMessage}.");
            }
            return Task.FromResult(new TurnResponse()
            {
                TurnIsCorrect = turnIsCorrect,
                ErrorMessage = errorMessage
            }); 
        }
    }
}