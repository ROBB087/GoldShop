using GoldShopCore.Services;

namespace GoldShopWpf.Services;

public static class StartupValidator
{
    public static bool TryValidate(out string userMessage)
    {
        userMessage = string.Empty;

        if (!AppRuntimePolicy.IsProductionBuild || AppRuntimePolicy.AllowUnofficialRun)
        {
            if (AppRuntimePolicy.AllowUnofficialRun)
            {
                FileLogService.LogWarning(
                    "Startup validation",
                    $"Unofficial run override is enabled. Executable: {AppRuntimePolicy.CurrentExecutablePath}");
            }

            return true;
        }

        if (AppRuntimePolicy.IsRunningFromOfficialInstallLocation())
        {
            return true;
        }

        userMessage = UiText.Format("MsgUnofficialAppCopy", AppRuntimePolicy.OfficialExecutablePath);
        FileLogService.LogWarning(
            "Startup validation",
            $"Blocked unofficial executable path. Current: {AppRuntimePolicy.CurrentExecutablePath} | Official: {AppRuntimePolicy.OfficialExecutablePath}");
        return false;
    }
}
