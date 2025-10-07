using System.Collections.Concurrent;
using PKHeX.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKHeXWebSocketServer.Services;

namespace PKHeXWebSocketServer;

public class PKHeXService
{
    private readonly ConcurrentDictionary<string, SaveFile> _sessions = new();
    
    private readonly SaveFileService _saveFileService;
    private readonly PokemonManagementService _pokemonManagementService;
    private readonly PokemonStorageService _pokemonStorageService;
    private readonly GameKnowledgeService _gameKnowledgeService;
    private readonly TrainerProfileService _trainerProfileService;
    private readonly ProgressionService _progressionService;
    private readonly CommunicationService _communicationService;
    private readonly WorldFacilitiesService _worldFacilitiesService;

    public PKHeXService()
    {
        _saveFileService = new SaveFileService();
        _pokemonManagementService = new PokemonManagementService();
        _pokemonStorageService = new PokemonStorageService();
        _gameKnowledgeService = new GameKnowledgeService();
        _trainerProfileService = new TrainerProfileService();
        _progressionService = new ProgressionService();
        _communicationService = new CommunicationService();
        _worldFacilitiesService = new WorldFacilitiesService();
    }

    private string ErrorResponse(string message) => JsonConvert.SerializeObject(new { error = message });

    private SaveFile? GetSave(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var save) ? save : null;
    }

    public SaveFile? GetSaveFile(string sessionId)
    {
        return GetSave(sessionId);
    }

    public async Task<string> LoadSaveFileAsync(string base64Data)
    {
        var (response, save) = await _saveFileService.LoadSaveFileAsync(base64Data);
        
        if (save == null)
            return response;

        var sessionId = Guid.NewGuid().ToString();
        _sessions[sessionId] = save;

        try
        {
            var responseObj = JsonConvert.DeserializeObject<JObject>(response);
            if (responseObj != null)
            {
                responseObj["sessionId"] = sessionId;
                return JsonConvert.SerializeObject(responseObj);
            }
        }
        catch (JsonException)
        {
            // If deserialization fails, create a new response object
        }

        // Fallback: create a minimal response with sessionId
        return JsonConvert.SerializeObject(new { success = true, sessionId });
    }

    public string SaveSaveFile(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _saveFileService.SaveSaveFile(save);
    }

    public string GetSaveInfo(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _saveFileService.GetSaveInfo(save);
    }

    public string GetPokemonData(int box, int slot, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.GetPokemonData(save, box, slot);
    }

    public string GetAllPokemon(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.GetAllPokemon(save);
    }

    public string ExportShowdown(int box, int slot, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.ExportShowdown(save, box, slot);
    }

    public string SetPokemonData(int box, int slot, string base64PokemonData, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.SetPokemonData(save, box, slot, base64PokemonData);
    }

    public string ModifyPokemon(int box, int slot, JObject modifications, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.ModifyPokemon(save, box, slot, modifications);
    }

    public string DeletePokemon(int box, int slot, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.DeletePokemon(save, box, slot);
    }

    public string MovePokemon(int fromBox, int fromSlot, int toBox, int toSlot, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.MovePokemon(save, fromBox, fromSlot, toBox, toSlot);
    }

    public string ImportShowdown(int box, int slot, string showdownText, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.ImportShowdown(save, box, slot, showdownText);
    }

    public string GetTrainerInfo(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.GetTrainerInfo(save);
    }

    public string SetTrainerInfo(JObject trainerData, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.SetTrainerInfo(save, trainerData);
    }

    public string CheckLegality(int box, int slot, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.CheckLegality(save, box, slot);
    }

    public string LegalizePokemon(int box, int slot, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.LegalizePokemon(save, box, slot);
    }

    public string GetBoxNames(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonStorageService.GetBoxNames(save);
    }

    public string GetParty(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.GetParty(save);
    }

    public string GetPartySlot(int slot, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.GetPartySlot(save, slot);
    }

    public string SetPartySlot(int slot, string base64PokemonData, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.SetPartySlot(save, slot, base64PokemonData);
    }

    public string DeletePartySlot(int slot, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.DeletePartySlot(save, slot);
    }

    public string ListSessions()
    {
        var sessionIds = _sessions.Keys.ToList();
        return JsonConvert.SerializeObject(new { sessionIds, count = sessionIds.Count });
    }

    public string UnloadSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out _))
        {
            return JsonConvert.SerializeObject(new { success = true, message = $"Session '{sessionId}' unloaded successfully" });
        }
        return ErrorResponse($"Session ID '{sessionId}' not found");
    }

    public string GetPouchItems(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _gameKnowledgeService.GetPouchItems(save);
    }

    public string AddItemToPouch(int itemId, int count, int? pouchIndex, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _gameKnowledgeService.AddItemToPouch(save, itemId, count, pouchIndex);
    }

    public string RemoveItemFromPouch(int itemId, int count, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _gameKnowledgeService.RemoveItemFromPouch(save, itemId, count);
    }

    public string GetPokedex(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _gameKnowledgeService.GetPokedex(save);
    }

    public string SetPokedexCaught(int species, bool value, int form, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _gameKnowledgeService.SetPokedexCaught(save, species, value, form);
    }

    public string SetPokedexSeen(int species, bool value, int form, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _gameKnowledgeService.SetPokedexSeen(save, species, value, form);
    }

    public string GetBadges(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.GetBadges(save);
    }

    public string SetBadge(int badgeIndex, bool value, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.SetBadge(save, badgeIndex, value);
    }

    public string GetEventFlag(int flagIndex, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _progressionService.GetEventFlag(save, flagIndex);
    }

    public string SetEventFlag(int flagIndex, bool value, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _progressionService.SetEventFlag(save, flagIndex, value);
    }

    public string GetEventConst(int constIndex, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _progressionService.GetEventConst(save, constIndex);
    }

    public string SetEventConst(int constIndex, int value, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _progressionService.SetEventConst(save, constIndex, value);
    }

    public string GetDaycare(int location, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonStorageService.GetDaycare(save, location);
    }

    public string GetBoxWallpapers(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonStorageService.GetBoxWallpapers(save);
    }

    public string SetBoxWallpaper(int box, int wallpaper, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonStorageService.SetBoxWallpaper(save, box, wallpaper);
    }

    public string GetSecondsPlayed(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.GetSecondsPlayed(save);
    }

    public string SetGameTime(int hours, int minutes, int seconds, string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.SetGameTime(save, hours, minutes, seconds);
    }

    public string GetMysteryGifts(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _communicationService.GetMysteryGifts(save);
    }

    public string GetMysteryGiftFlags(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _communicationService.GetMysteryGiftFlags(save);
    }

    public string GetSpeciesName(int id)
    {
        return _gameKnowledgeService.GetSpeciesName(id);
    }

    public string GetMoveName(int id)
    {
        return _gameKnowledgeService.GetMoveName(id);
    }

    public string GetAbilityName(int id)
    {
        return _gameKnowledgeService.GetAbilityName(id);
    }

    public string GetItemName(int id)
    {
        return _gameKnowledgeService.GetItemName(id);
    }

    public string GetNatureName(int id)
    {
        return _gameKnowledgeService.GetNatureName(id);
    }

    public string GetTypeName(int id)
    {
        return _gameKnowledgeService.GetTypeName(id);
    }

    public string GetAllSpecies()
    {
        return _gameKnowledgeService.GetAllSpecies();
    }

    public string GetAllMoves()
    {
        return _gameKnowledgeService.GetAllMoves();
    }

    public string GetAllAbilities()
    {
        return _gameKnowledgeService.GetAllAbilities();
    }

    public string GetAllItems()
    {
        return _gameKnowledgeService.GetAllItems();
    }

    public string GetAllNatures()
    {
        return _gameKnowledgeService.GetAllNatures();
    }

    public string GetAllTypes()
    {
        return _gameKnowledgeService.GetAllTypes();
    }

    public string GetSecondsToStart(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.GetSecondsToStart(save);
    }

    public string SetSecondsToStart(string sessionId, uint seconds)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.SetSecondsToStart(save, seconds);
    }

    public string GetSecondsToFame(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.GetSecondsToFame(save);
    }

    public string SetSecondsToFame(string sessionId, uint seconds)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.SetSecondsToFame(save, seconds);
    }

    public string GetRecords(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _progressionService.GetRecords(save);
    }

    public string SetRecord(string sessionId, int index, int value)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _progressionService.SetRecord(save, index, value);
    }

    public string GetRecordValue(string sessionId, int index)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _progressionService.GetRecordValue(save, index);
    }

    public string GetCoins(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.GetCoins(save);
    }

    public string SetCoins(string sessionId, uint coins)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.SetCoins(save, coins);
    }

    public string GetBattlePoints(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.GetBattlePoints(save);
    }

    public string SetBattlePoints(string sessionId, uint battlePoints)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.SetBattlePoints(save, battlePoints);
    }

    public string GetRivalName(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.GetRivalName(save);
    }

    public string SetRivalName(string sessionId, string rivalName)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.SetRivalName(save, rivalName);
    }

    public string GetBattleBox(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonStorageService.GetBattleBox(save);
    }

    public string SetBattleBoxSlot(string sessionId, int slot, string base64PokemonData)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonStorageService.SetBattleBoxSlot(save, slot, base64PokemonData);
    }

    public string GetHallOfFame(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _progressionService.GetHallOfFame(save);
    }

    public string SetHallOfFameEntry(string sessionId, int index, JArray team)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _progressionService.SetHallOfFameEntry(save, index, team);
    }

    public string GetMailbox(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _communicationService.GetMailbox(save);
    }

    public string GetMailMessage(string sessionId, int index)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _communicationService.GetMailMessage(save, index);
    }

    public string SetMailMessage(string sessionId, int index, JObject mailData)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _communicationService.SetMailMessage(save, index, mailData);
    }

    public string DeleteMail(string sessionId, int index)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _communicationService.DeleteMail(save, index);
    }

    public string GetRibbons(string sessionId, int box, int slot)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.GetRibbons(save, box, slot);
    }

    public string SetRibbon(string sessionId, int box, int slot, string ribbonName, bool value)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.SetRibbon(save, box, slot, ribbonName, value);
    }

    public string GetRibbonCount(string sessionId, int box, int slot)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.GetRibbonCount(save, box, slot);
    }

    public string GetContestStats(string sessionId, int box, int slot)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.GetContestStats(save, box, slot);
    }

    public string SetContestStat(string sessionId, int box, int slot, string stat, int value)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _pokemonManagementService.SetContestStat(save, box, slot, stat, value);
    }

    public string GetBattleFacilityStats(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _progressionService.GetBattleFacilityStats(save);
    }

    public string GetTrainerAppearance(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.GetTrainerAppearance(save);
    }

    public string SetTrainerAppearance(string sessionId, JObject appearanceData)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.SetTrainerAppearance(save, appearanceData);
    }

    public string GetTrainerCard(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _trainerProfileService.GetTrainerCard(save);
    }

    public string GetSecretBase(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _worldFacilitiesService.GetSecretBase(save);
    }

    public string GetEntralinkData(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _worldFacilitiesService.GetEntralinkData(save);
    }

    public string GetFestivalPlaza(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _worldFacilitiesService.GetFestivalPlaza(save);
    }

    public string GetPokePelago(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _worldFacilitiesService.GetPokePelago(save);
    }

    public string GetPokeJobs(string sessionId)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _worldFacilitiesService.GetPokeJobs(save);
    }

    public string GetMysteryGiftCard(string sessionId, int index)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _communicationService.GetMysteryGiftCard(save, index);
    }

    public string SetMysteryGiftCard(string sessionId, int index, JObject cardData)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _communicationService.SetMysteryGiftCard(save, index, cardData);
    }

    public string DeleteMysteryGift(string sessionId, int index)
    {
        var save = GetSave(sessionId);
        if (save == null)
            return ErrorResponse($"Session ID '{sessionId}' not found");

        return _communicationService.DeleteMysteryGift(save, index);
    }
}
