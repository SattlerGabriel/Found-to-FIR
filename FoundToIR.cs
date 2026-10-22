using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.InRaid;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace FoundToIR;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.boombah.foundtoir";
    public string Name { get; init; } = "FoundToIR";
    public string Author { get; init; } = "Boombah";
    public List<string>? Contributors { get; init; }
    public Version Version { get; init; } = new("1.0.0");
    public Range SptVersion { get; init; } = new("~4.1.6");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class FoundToIR(ISptLogger<FoundToIR> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        new SetInventoryPatch().Enable();
        logger.Success("[FoundToIR] Items brought into raid become FiR on a successful (non run-through) extract");
        return Task.CompletedTask;
    }
}

public class SetInventoryPatch : AbstractPatch
{
    protected override MethodBase? GetTargetMethod() => typeof(InRaidHelper).GetMethod(nameof(InRaidHelper.SetInventory));

    [PatchPrefix]
    public static void Prefix(PmcData postRaidProfile, bool isSurvived)
    {
        var equipment = postRaidProfile.Inventory?.Equipment;
        if (!isSurvived || equipment is null || postRaidProfile.Inventory?.Items is null)
            return;

        foreach (Item item in postRaidProfile.Inventory.Items.GetItemWithChildren(equipment.Value).Where(item => item.Id != equipment.Value))
        {
            item.Upd ??= new Upd();
            item.Upd.SpawnedInSession = true;
        }
    }
}
