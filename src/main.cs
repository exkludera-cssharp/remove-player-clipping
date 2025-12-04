using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Microsoft.Extensions.Logging;

public class Plugin : BasePlugin, IPluginConfig<Config>
{
    public static readonly MemoryFunctionWithReturn<IntPtr, long, long> SetSolidMask = new(GameData.GetSignature("SetSolidMask"));
    public static readonly int SetSolidMaskPointer = Schema.GetSchemaOffset("CBaseModelEntity", "m_Collision");
    public const long SOLID_MASK_PLAYERCLIP = 16;

    public bool RemovePlayerClipping;

    public override string ModuleName => "Remove Player Clipping";
    public override string ModuleVersion => "";
    public override string ModuleAuthor => "exkludera"; // i barely did anything :D, https://gist.github.com/21Joakim/9825489a715216abe2f1dba5f1749ad2

    public Config Config { get; set; } = new Config();
    public void OnConfigParsed(Config config) => Config = config;

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(OnMapStart);

        SetSolidMask.Hook(SetSolidMaskHook, HookMode.Pre);
    }

    public override void Unload(bool hotReload)
    {
        RemoveListener<Listeners.OnMapStart>(OnMapStart);

        SetSolidMask.Unhook(SetSolidMaskHook, HookMode.Pre);
    }

    public void OnMapStart(string mapname)
    {
        RemovePlayerClipping = Config.Maps.Contains(mapname);;
    }

    public HookResult SetSolidMaskHook(DynamicHook hook)
    {
        if (!RemovePlayerClipping)
            return HookResult.Continue;

        IntPtr pointer = hook.GetParam<IntPtr>(0);
        long flag = hook.GetParam<long>(1);

        try
        {
            CCSPlayerPawn pawn = new(pointer - SetSolidMaskPointer);
            if (pawn.Bot != null)
                return HookResult.Continue;
        }
        catch (Exception ex)
        {
            Logger.LogError($"[RemovePlayerClipping] Error in SetSolidMaskHook: {ex.Message}");
            return HookResult.Continue;
        }

        hook.SetParam(1, flag & ~SOLID_MASK_PLAYERCLIP);

        return HookResult.Continue;
    }
}