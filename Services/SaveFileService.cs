using PKHeX.Core;
using PKHeXWebSocketServer.Metadata;

namespace PKHeXWebSocketServer.Services;

public class SaveFileService : ServiceBase
{
    [ApiAction("loadSave", ApiCategory.Save, "Load a Pokémon save file from base64-encoded data.", RequiresSession = false)]
    public Task<(string response, SaveFile? save)> LoadSaveFileAsync([ApiParameter(Format = "base64")] string base64Data)
    {
        try
        {
            if (string.IsNullOrEmpty(base64Data))
                return Task.FromResult((ErrorResponse("Base64 save data is required"), (SaveFile?)null));

            byte[] data;
            try
            {
                data = Convert.FromBase64String(base64Data);
            }
            catch (FormatException)
            {
                return Task.FromResult((ErrorResponse("Invalid base64 data"), (SaveFile?)null));
            }

            var save = SaveUtil.GetSaveFile(data);

            if (save == null)
                return Task.FromResult((ErrorResponse("Unable to load save file"), (SaveFile?)null));

            var features = DetectFeatures(save);

            var response = SuccessResponse(new
            {
                success = true,
                saveType = save.GetType().Name,
                game = save.Version.ToString(),
                generation = save.Generation,
                trainerName = save.OT,
                trainerID = save.DisplayTID,
                boxCount = save.BoxCount,
                boxSlotCount = save.BoxSlotCount,
                features = features
            });

            return Task.FromResult((response, (SaveFile?)save));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading save file: {ex}");
            return Task.FromResult((ErrorResponse("Failed to load save file"), (SaveFile?)null));
        }
    }

    [ApiAction("saveSave", ApiCategory.Save, "Export the current save file as base64-encoded data.")]
    public string SaveSaveFile(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            Memory<byte> data = save.Write();
            string base64 = Convert.ToBase64String(data.ToArray());

            return SuccessResponse(new { success = true, data = base64 });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving save file: {ex}");
            return ErrorResponse("Failed to export save file");
        }
    }

    [ApiAction("getSaveInfo", ApiCategory.Save, "Get general information about the loaded save file.")]
    public string GetSaveInfo(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var features = DetectFeatures(save);

            var info = new
            {
                saveType = save.GetType().Name,
                game = save.Version.ToString(),
                generation = save.Generation,
                trainerName = save.OT,
                trainerID = save.DisplayTID,
                secretID = save.DisplaySID,
                gender = save.Gender,
                playTime = $"{save.PlayedHours}h {save.PlayedMinutes}m",
                money = save.Money,
                boxCount = save.BoxCount,
                boxSlotCount = save.BoxSlotCount,
                partyCount = save.PartyCount,
                features = features
            };

            return SuccessResponse(info);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting save info: {ex}");
            return ErrorResponse("Failed to get save info");
        }
    }

    private string[] DetectFeatures(SaveFile save)
    {
        var saveType = save.GetType();
        var featureList = new List<string>();

        if (save is ITrainerStatRecord)
        {
            featureList.Add("trainerRecords");
            featureList.Add("battleFacility");
        }

        if (saveType.GetProperty("Rival") != null)
            featureList.Add("rivalName");

        if (saveType.GetProperty("BattleBoxCount") != null || saveType.GetProperty("BattleTree") != null)
            featureList.Add("battleBox");

        if (saveType.GetProperty("Coins") != null)
            featureList.Add("coins");

        if (saveType.GetProperty("BP") != null)
            featureList.Add("battlePoints");

        if (saveType.GetProperty("HallOfFameData") != null)
            featureList.Add("hallOfFame");

        if (saveType.GetProperty("Mail") != null)
            featureList.Add("mail");

        var testPk = save.BlankPKM;
        if (testPk is IRibbonIndex)
            featureList.Add("ribbons");

        if (testPk is IContestStats)
            featureList.Add("contestStats");

        if (save.Generation >= 6 && save.Generation <= 8)
        {
            var saveTypeName = saveType.Name;
            if (saveTypeName.Contains("SAV6") || saveTypeName.Contains("SAV7") || saveTypeName.Contains("SAV8"))
                featureList.Add("trainerAppearance");
        }

        if ((save.Generation == 3 || save.Generation == 4 || save.Generation == 6) && 
            saveType.GetProperty("SecretBase") != null)
            featureList.Add("secretBase");

        if (save.Generation == 5 && saveType.GetProperty("Entralink") != null)
            featureList.Add("entralink");

        if (save.Generation == 7 && saveType.GetProperty("FestivalPlaza") != null)
            featureList.Add("festivalPlaza");

        if (save.Generation == 7 && saveType.GetProperty("PokePelago") != null)
            featureList.Add("pokePelago");

        if (save.Generation == 8 && saveType.GetProperty("PokeJobs") != null)
            featureList.Add("pokeJobs");

        if (saveType.GetProperty("MysteryGiftCards") != null)
            featureList.Add("mysteryGiftCards");

        featureList.Add("secondsToStart");
        featureList.Add("secondsToFame");

        return featureList.ToArray();
    }
}
