using Windows.ApplicationModel;

namespace LiveWallpaperStudio.App.Services;

public static class StartupTaskService
{
    private const string TaskId = "LoopdeskStartup";

    public static async Task<StartupTaskState?> GetStateAsync()
    {
        try { return (await StartupTask.GetAsync(TaskId)).State; }
        catch { return null; }
    }

    public static async Task<StartupTaskState?> SetEnabledAsync(bool enabled)
    {
        try
        {
            StartupTask task = await StartupTask.GetAsync(TaskId);
            if (!enabled)
            {
                task.Disable();
                return task.State;
            }
            return task.State == StartupTaskState.Enabled ? task.State : await task.RequestEnableAsync();
        }
        catch { return null; }
    }
}
