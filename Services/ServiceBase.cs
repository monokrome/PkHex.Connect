using PKHeX.Core;
using Newtonsoft.Json;

namespace PKHeXWebSocketServer.Services;

public abstract class ServiceBase
{
    protected string ErrorResponse(string message) => JsonConvert.SerializeObject(new { error = message });
    
    protected string SuccessResponse(object data) => JsonConvert.SerializeObject(data);

    protected string? ValidateSave(SaveFile? save)
    {
        if (save == null)
            return ErrorResponse("No save file loaded");
        return null;
    }

    protected string? ValidateBoxSlot(SaveFile save, int box, int slot)
    {
        var saveError = ValidateSave(save);
        if (saveError != null) return saveError;

        if (box < 0 || box >= save.BoxCount)
            return ErrorResponse($"Invalid box index. Must be between 0 and {save.BoxCount - 1}");

        if (slot < 0 || slot >= save.BoxSlotCount)
            return ErrorResponse($"Invalid slot index. Must be between 0 and {save.BoxSlotCount - 1}");

        return null;
    }

    protected PKM? GetPokemonAt(SaveFile save, int box, int slot)
    {
        return save.GetBoxSlotAtIndex(box, slot);
    }

    protected void SetPokemonAt(SaveFile save, int box, int slot, PKM pokemon)
    {
        save.SetBoxSlotAtIndex(pokemon, box, slot);
    }
}
