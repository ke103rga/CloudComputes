namespace CitiesGame.Services;

using System;
using System.Collections.Generic;

public class LobbiesCollection
{
    public Dictionary<int, CitiesGameLobby> ActiveLobbies { get; private set; }
    public Dictionary<int, CitiesGameLobby> NotFullLobbies { get; private set; } 
    
    private Dictionary<int, CitiesGameLobby> CapacityLobbies { get; set; } // Лобби по каждому capacity

    private int lobbyIdCounter = 0;

    public LobbiesCollection()
    {
        ActiveLobbies = new Dictionary<int, CitiesGameLobby>();
        NotFullLobbies = new Dictionary<int, CitiesGameLobby>();
        CapacityLobbies = new Dictionary<int, CitiesGameLobby>();
    }

    public CitiesGameLobby CreateLobby(int capacity)
    {
        var newLobby = new CitiesGameLobby(++lobbyIdCounter, capacity);
        NotFullLobbies.Add(newLobby.Id, newLobby);
        CapacityLobbies.Add(capacity, newLobby);
        return newLobby;
    }
    
    public CitiesGameLobby FindOrCreateLobby(int capacity)
    {
        CitiesGameLobby notFullLobby;
        if (!CapacityLobbies.ContainsKey(capacity))
        {
            notFullLobby = CreateLobby(capacity);
        }
        else
        {
            notFullLobby = CapacityLobbies.GetValueOrDefault(capacity);
        }

        return notFullLobby;
    }

    public void MakeLobbyActive(int lobbyId)
    {
        CitiesGameLobby lobby = NotFullLobbies.GetValueOrDefault(lobbyId);
        ActiveLobbies[lobbyId] = lobby;
        NotFullLobbies.Remove(lobbyId);
        CapacityLobbies.Remove(lobby.Capacity);
    }

    public void RemoveLobby(int lobbyId)
    {
        ActiveLobbies.Remove(lobbyId);
    } 
}