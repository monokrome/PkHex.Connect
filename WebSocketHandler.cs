using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKHeXWebSocketServer.Metadata;

namespace PKHeXWebSocketServer;

public class WebSocketHandler
{
    private readonly WebSocket _webSocket;
    private readonly PKHeXService _pkheXService;

    public WebSocketHandler(WebSocket webSocket)
    {
        _webSocket = webSocket;
        _pkheXService = new PKHeXService();
    }

    public async Task HandleConnectionAsync()
    {
        var buffer = new byte[1024 * 1024];

        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                
                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    Console.WriteLine("Client disconnected");
                    break;
                }

                ms.Seek(0, SeekOrigin.Begin);
                var message = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine($"Received message ({ms.Length} bytes)");

                var response = await ProcessMessageAsync(message);
                await SendMessageAsync(response);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket error: {ex}");
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Server error occurred", CancellationToken.None);
            }
        }
    }

    private bool ActionExists(string? actionName)
    {
        if (string.IsNullOrEmpty(actionName))
            return false;

        // Special cases that aren't annotated as API actions
        if (actionName == "listSessions" || actionName == "loadSave")
            return true;

        // Check all service types in the Services namespace
        var assembly = typeof(PKHeXService).Assembly;
        var serviceTypes = assembly.GetTypes()
            .Where(t => t.Namespace == "PKHeXWebSocketServer.Services" && !t.IsAbstract);

        foreach (var serviceType in serviceTypes)
        {
            var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                var apiActionAttr = method.GetCustomAttribute<ApiActionAttribute>();
                if (apiActionAttr != null && apiActionAttr.Name == actionName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool ActionRequiresSession(string? actionName)
    {
        if (string.IsNullOrEmpty(actionName))
            return true;

        // Special cases that aren't annotated as API actions
        if (actionName == "listSessions" || actionName == "loadSave")
            return false;

        // Check all service types in the Services namespace
        var assembly = typeof(PKHeXService).Assembly;
        var serviceTypes = assembly.GetTypes()
            .Where(t => t.Namespace == "PKHeXWebSocketServer.Services" && !t.IsAbstract);

        foreach (var serviceType in serviceTypes)
        {
            var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                var apiActionAttr = method.GetCustomAttribute<ApiActionAttribute>();
                if (apiActionAttr != null && apiActionAttr.Name == actionName)
                {
                    return apiActionAttr.RequiresSession;
                }
            }
        }

        // Default to requiring session if action not found
        return true;
    }

    private async Task<string> ProcessMessageAsync(string message)
    {
        try
        {
            var request = JObject.Parse(message);
            var action = request["action"]?.ToString();
            var sessionId = request["sessionId"]?.ToString();

            // First check if action exists
            if (!ActionExists(action))
            {
                return JsonConvert.SerializeObject(new { error = "Unknown action" });
            }

            // Then check if this action requires a session
            if (ActionRequiresSession(action) && string.IsNullOrEmpty(sessionId))
            {
                return JsonConvert.SerializeObject(new { error = "sessionId is required for this action" });
            }

            // Special handling for async loadSave
            if (action == "loadSave")
            {
                return await _pkheXService.LoadSaveFileAsync(request["data"]?.ToString() ?? "");
            }

            return action switch
            {
                "listSessions" => _pkheXService.ListSessions(),
                "getSpeciesName" => _pkheXService.GetSpeciesName(request["species"]?.ToObject<int>() ?? 0),
                "getMoveName" => _pkheXService.GetMoveName(request["move"]?.ToObject<int>() ?? 0),
                "getAbilityName" => _pkheXService.GetAbilityName(request["ability"]?.ToObject<int>() ?? 0),
                "getItemName" => _pkheXService.GetItemName(request["item"]?.ToObject<int>() ?? 0),
                "getNatureName" => _pkheXService.GetNatureName(request["nature"]?.ToObject<int>() ?? 0),
                "getTypeName" => _pkheXService.GetTypeName(request["type"]?.ToObject<int>() ?? 0),
                "getAllSpecies" => _pkheXService.GetAllSpecies(),
                "getAllMoves" => _pkheXService.GetAllMoves(),
                "getAllAbilities" => _pkheXService.GetAllAbilities(),
                "getAllItems" => _pkheXService.GetAllItems(),
                "getAllNatures" => _pkheXService.GetAllNatures(),
                "getAllTypes" => _pkheXService.GetAllTypes(),
                "saveSave" => _pkheXService.SaveSaveFile(sessionId!),
                "getPokemon" => _pkheXService.GetPokemonData(request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0, sessionId!),
                "setPokemon" => _pkheXService.SetPokemonData(request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0, request["data"]?.ToString() ?? "", sessionId!),
                "modifyPokemon" => _pkheXService.ModifyPokemon(request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0, request["modifications"] as JObject ?? new JObject(), sessionId!),
                "deletePokemon" => _pkheXService.DeletePokemon(request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0, sessionId!),
                "movePokemon" => _pkheXService.MovePokemon(request["fromBox"]?.ToObject<int>() ?? 0, request["fromSlot"]?.ToObject<int>() ?? 0, request["toBox"]?.ToObject<int>() ?? 0, request["toSlot"]?.ToObject<int>() ?? 0, sessionId!),
                "getBoxNames" => _pkheXService.GetBoxNames(sessionId!),
                "getSaveInfo" => _pkheXService.GetSaveInfo(sessionId!),
                "getTrainerInfo" => _pkheXService.GetTrainerInfo(sessionId!),
                "setTrainerInfo" => _pkheXService.SetTrainerInfo(request["trainer"] as JObject ?? new JObject(), sessionId!),
                "getAllPokemon" => _pkheXService.GetAllPokemon(sessionId!),
                "checkLegality" => _pkheXService.CheckLegality(request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0, sessionId!),
                "legalizePokemon" => _pkheXService.LegalizePokemon(request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0, sessionId!),
                "importShowdown" => _pkheXService.ImportShowdown(request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0, request["showdown"]?.ToString() ?? "", sessionId!),
                "exportShowdown" => _pkheXService.ExportShowdown(request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0, sessionId!),
                "getParty" => _pkheXService.GetParty(sessionId!),
                "getPartySlot" => _pkheXService.GetPartySlot(request["slot"]?.ToObject<int>() ?? 0, sessionId!),
                "setPartySlot" => _pkheXService.SetPartySlot(request["slot"]?.ToObject<int>() ?? 0, request["data"]?.ToString() ?? "", sessionId!),
                "deletePartySlot" => _pkheXService.DeletePartySlot(request["slot"]?.ToObject<int>() ?? 0, sessionId!),
                "unloadSession" => _pkheXService.UnloadSession(sessionId!),
                "getPouchItems" => _pkheXService.GetPouchItems(sessionId!),
                "addItemToPouch" => _pkheXService.AddItemToPouch(request["itemId"]?.ToObject<int>() ?? 0, request["count"]?.ToObject<int>() ?? 1, request["pouchIndex"]?.ToObject<int?>(), sessionId!),
                "removeItemFromPouch" => _pkheXService.RemoveItemFromPouch(request["itemId"]?.ToObject<int>() ?? 0, request["count"]?.ToObject<int>() ?? 1, sessionId!),
                "getPokedex" => _pkheXService.GetPokedex(sessionId!),
                "setPokedexCaught" => _pkheXService.SetPokedexCaught(request["species"]?.ToObject<int>() ?? 0, request["value"]?.ToObject<bool>() ?? true, request["form"]?.ToObject<int>() ?? 0, sessionId!),
                "setPokedexSeen" => _pkheXService.SetPokedexSeen(request["species"]?.ToObject<int>() ?? 0, request["value"]?.ToObject<bool>() ?? true, request["form"]?.ToObject<int>() ?? 0, sessionId!),
                "getBadges" => _pkheXService.GetBadges(sessionId!),
                "setBadge" => _pkheXService.SetBadge(request["badgeIndex"]?.ToObject<int>() ?? 0, request["value"]?.ToObject<bool>() ?? true, sessionId!),
                "getEventFlag" => _pkheXService.GetEventFlag(request["flagIndex"]?.ToObject<int>() ?? 0, sessionId!),
                "setEventFlag" => _pkheXService.SetEventFlag(request["flagIndex"]?.ToObject<int>() ?? 0, request["value"]?.ToObject<bool>() ?? true, sessionId!),
                "getEventConst" => _pkheXService.GetEventConst(request["constIndex"]?.ToObject<int>() ?? 0, sessionId!),
                "setEventConst" => _pkheXService.SetEventConst(request["constIndex"]?.ToObject<int>() ?? 0, request["value"]?.ToObject<int>() ?? 0, sessionId!),
                "getDaycare" => _pkheXService.GetDaycare(request["location"]?.ToObject<int>() ?? 0, sessionId!),
                "getBoxWallpapers" => _pkheXService.GetBoxWallpapers(sessionId!),
                "setBoxWallpaper" => _pkheXService.SetBoxWallpaper(request["box"]?.ToObject<int>() ?? 0, request["wallpaper"]?.ToObject<int>() ?? 0, sessionId!),
                "getSecondsPlayed" => _pkheXService.GetSecondsPlayed(sessionId!),
                "setGameTime" => _pkheXService.SetGameTime(request["hours"]?.ToObject<int>() ?? 0, request["minutes"]?.ToObject<int>() ?? 0, request["seconds"]?.ToObject<int>() ?? 0, sessionId!),
                "getMysteryGifts" => _pkheXService.GetMysteryGifts(sessionId!),
                "getMysteryGiftFlags" => _pkheXService.GetMysteryGiftFlags(sessionId!),
                "getSecondsToStart" => _pkheXService.GetSecondsToStart(sessionId!),
                "setSecondsToStart" => _pkheXService.SetSecondsToStart(sessionId!, request["seconds"]?.ToObject<uint>() ?? 0),
                "getSecondsToFame" => _pkheXService.GetSecondsToFame(sessionId!),
                "setSecondsToFame" => _pkheXService.SetSecondsToFame(sessionId!, request["seconds"]?.ToObject<uint>() ?? 0),
                "getRecords" => _pkheXService.GetRecords(sessionId!),
                "setRecord" => _pkheXService.SetRecord(sessionId!, request["index"]?.ToObject<int>() ?? 0, request["value"]?.ToObject<int>() ?? 0),
                "getRecordValue" => _pkheXService.GetRecordValue(sessionId!, request["index"]?.ToObject<int>() ?? 0),
                "getCoins" => _pkheXService.GetCoins(sessionId!),
                "setCoins" => _pkheXService.SetCoins(sessionId!, request["coins"]?.ToObject<uint>() ?? 0),
                "getBattlePoints" => _pkheXService.GetBattlePoints(sessionId!),
                "setBattlePoints" => _pkheXService.SetBattlePoints(sessionId!, request["battlePoints"]?.ToObject<uint>() ?? 0),
                "getRivalName" => _pkheXService.GetRivalName(sessionId!),
                "setRivalName" => _pkheXService.SetRivalName(sessionId!, request["rivalName"]?.ToString() ?? ""),
                "getBattleBox" => _pkheXService.GetBattleBox(sessionId!),
                "setBattleBoxSlot" => _pkheXService.SetBattleBoxSlot(sessionId!, request["slot"]?.ToObject<int>() ?? 0, request["data"]?.ToString() ?? ""),
                "getHallOfFame" => _pkheXService.GetHallOfFame(sessionId!),
                "setHallOfFameEntry" => _pkheXService.SetHallOfFameEntry(sessionId!, request["index"]?.ToObject<int>() ?? 0, request["team"] as JArray ?? new JArray()),
                "getMailbox" => _pkheXService.GetMailbox(sessionId!),
                "getMailMessage" => _pkheXService.GetMailMessage(sessionId!, request["index"]?.ToObject<int>() ?? 0),
                "setMailMessage" => _pkheXService.SetMailMessage(sessionId!, request["index"]?.ToObject<int>() ?? 0, request["mailData"] as JObject ?? new JObject()),
                "deleteMail" => _pkheXService.DeleteMail(sessionId!, request["index"]?.ToObject<int>() ?? 0),
                "getRibbons" => _pkheXService.GetRibbons(sessionId!, request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0),
                "setRibbon" => _pkheXService.SetRibbon(sessionId!, request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0, request["ribbonName"]?.ToString() ?? "", request["value"]?.ToObject<bool>() ?? false),
                "getRibbonCount" => _pkheXService.GetRibbonCount(sessionId!, request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0),
                "getContestStats" => _pkheXService.GetContestStats(sessionId!, request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0),
                "setContestStat" => _pkheXService.SetContestStat(sessionId!, request["box"]?.ToObject<int>() ?? 0, request["slot"]?.ToObject<int>() ?? 0, request["stat"]?.ToString() ?? "", request["value"]?.ToObject<int>() ?? 0),
                "getBattleFacilityStats" => _pkheXService.GetBattleFacilityStats(sessionId!),
                "getTrainerAppearance" => _pkheXService.GetTrainerAppearance(sessionId!),
                "setTrainerAppearance" => _pkheXService.SetTrainerAppearance(sessionId!, request["appearanceData"] as JObject ?? new JObject()),
                "getTrainerCard" => _pkheXService.GetTrainerCard(sessionId!),
                "getSecretBase" => _pkheXService.GetSecretBase(sessionId!),
                "getEntralinkData" => _pkheXService.GetEntralinkData(sessionId!),
                "getFestivalPlaza" => _pkheXService.GetFestivalPlaza(sessionId!),
                "getPokePelago" => _pkheXService.GetPokePelago(sessionId!),
                "getPokeJobs" => _pkheXService.GetPokeJobs(sessionId!),
                "getMysteryGiftCard" => _pkheXService.GetMysteryGiftCard(sessionId!, request["index"]?.ToObject<int>() ?? 0),
                "setMysteryGiftCard" => _pkheXService.SetMysteryGiftCard(sessionId!, request["index"]?.ToObject<int>() ?? 0, request["cardData"] as JObject ?? new JObject()),
                "deleteMysteryGift" => _pkheXService.DeleteMysteryGift(sessionId!, request["index"]?.ToObject<int>() ?? 0),
                _ => JsonConvert.SerializeObject(new { error = "Unknown action" })
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex}");
            return JsonConvert.SerializeObject(new { error = "Failed to process request" });
        }
    }

    private async Task SendMessageAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var chunkSize = 1024 * 1024;
        var offset = 0;

        while (offset < bytes.Length)
        {
            var count = Math.Min(chunkSize, bytes.Length - offset);
            var endOfMessage = offset + count >= bytes.Length;
            
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes, offset, count),
                WebSocketMessageType.Text,
                endOfMessage,
                CancellationToken.None);
            
            offset += count;
        }
    }
}
