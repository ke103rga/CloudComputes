using System.Collections.Generic;

namespace CitiesGame.Services;

public class CitiesChecker
{
    public HashSet<string> Cities { get; private set; }
    public Dictionary<int, HashSet<string>> LobbyPlayedCities { get; private set; }

    public CitiesChecker()
    {
        Cities = new HashSet<string>();
        LobbyPlayedCities = new Dictionary<int, HashSet<string>>();
        LoadCities(); // Метод для загрузки городов
    }

    private void LoadCities()
    {
        // Заполняем Cities реальными данными в нижнем регистре
        Cities.Add("москва");
        Cities.Add("архангельск");
        Cities.Add("санкт-петербург");
        Cities.Add("новосибирск");
        Cities.Add("екатеринбург");
        Cities.Add("нижний новгород");
        Cities.Add("казань");
        Cities.Add("челябинск");
        Cities.Add("омск");
        Cities.Add("самара");
        Cities.Add("ростов-на-дону");
        Cities.Add("ufa");
        Cities.Add("красноярск");
        Cities.Add("перми");
        Cities.Add("воронеж");
        Cities.Add("волгоград");
        Cities.Add("саратов");
        Cities.Add("тюмень");
        Cities.Add("ижевск");
        Cities.Add("благовещенск");
        Cities.Add("чебоксары");
        Cities.Add("иркутск");
        Cities.Add("ярославль");
        Cities.Add("хабаровск");
        Cities.Add("тольятти");
        Cities.Add("калуга");
        Cities.Add("ставрополь");
        Cities.Add("липецк");
        Cities.Add("тверь");
        Cities.Add("ульяновск");
        Cities.Add("барнаул");
        Cities.Add("магнитогорск");
        Cities.Add("сочи");
        Cities.Add("набережные челны");
        Cities.Add("кемерово");
        Cities.Add("ес4629ентиуки");
        Cities.Add("абакан");
        Cities.Add("арзамас");
        Cities.Add("жуковский");
        Cities.Add("рязань");
        Cities.Add("сыктывкар");
        Cities.Add("владикавказ");
        Cities.Add("мурманск");
        Cities.Add("симферополь");
        Cities.Add("томск");
        Cities.Add("сургут");
        Cities.Add("брянск");
        Cities.Add("кострома");
        Cities.Add("иваново");
        Cities.Add("пенза");
    }

    public void AddLobbyPlayedCities(int lobbyId)
    {
        LobbyPlayedCities[lobbyId] = new HashSet<string>();
    }
    
    public void RemoveLobbyPlayedCities(int lobbyId)
    {
        LobbyPlayedCities.Remove(lobbyId);
    }

    public bool IsCityValid(string city, int lobbyId, out string errorMessage)
    {
        // Проверка существования города
        // if (!Cities.Contains(city))
        // {
        //     errorMessage = "City does not exist";
        //     return false;   
        // }
        
        // Проверка на повторение
        if (LobbyPlayedCities.ContainsKey(lobbyId) &&
            LobbyPlayedCities[lobbyId].Contains(city))
        {
            errorMessage = "City already used";
            return false;   
        }
            

        AddPlayedCity(lobbyId, city);

        errorMessage = "";
        return true;
    }

    private void AddPlayedCity(int lobbyId, string city)
    {
        if (!LobbyPlayedCities.ContainsKey(lobbyId))
        {
            LobbyPlayedCities[lobbyId] = new HashSet<string>();
        }
        LobbyPlayedCities[lobbyId].Add(city);
    }
}