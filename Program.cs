using System.Management;
using System.Security.Principal;
using Microsoft.Win32;

const string themesPersonalizeRegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
const string appsUseLightThemeRegistryValueName = "AppsUseLightTheme";
const string systemUsesLightThemeRegistryValueName = "SystemUsesLightTheme";

const string blueLightReductionState =
    @"\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CloudStore\\Store\\DefaultAccount\\Current\\default$windows.data.bluelightreduction.bluelightreductionstate\\windows.data.bluelightreduction.bluelightreductionstate";
const string blueLightReductionStateValueName = "Data";

try
{
    var currentUser = WindowsIdentity.GetCurrent();
    var query = new WqlEventQuery("SELECT * FROM RegistryValueChangeEvent WHERE " +
                                  "Hive = 'HKEY_USERS'" +
                                  $@"AND KeyPath = '{currentUser.User?.Value}{blueLightReductionState}' AND ValueName='{blueLightReductionStateValueName}'");

    Console.WriteLine("Waiting for User/Windows to change Windows Night light settings...");

    var watcher = new ManagementEventWatcher(query);
    watcher.EventArrived += HandleNightLightChangeEvent;
    watcher.Start();

    Console.ReadKey();

    watcher.Stop();
}
catch (ManagementException managementException)
{
    Console.WriteLine("Could not watch Windows Night light change event: " + managementException.Message);
}

void HandleNightLightChangeEvent(object sender, EventArrivedEventArgs e)
{
    Console.WriteLine("Night light has changed");

    using var personalizeSubKey = Registry.CurrentUser.OpenSubKey(themesPersonalizeRegistryKeyPath, true);
    var currentThemeColor =
        (WindowsThemeColor)(personalizeSubKey?.GetValue(appsUseLightThemeRegistryValueName) ?? WindowsThemeColor.Dark);

    Console.WriteLine($"Current theme color: {currentThemeColor}");

    var invertedThemeColor =
        currentThemeColor == WindowsThemeColor.Light ? WindowsThemeColor.Dark : WindowsThemeColor.Light;

    Console.WriteLine($"Setting theme color: {invertedThemeColor}");

    try
    {
        personalizeSubKey?.SetValue(appsUseLightThemeRegistryValueName, invertedThemeColor, RegistryValueKind.DWord);
        personalizeSubKey?.SetValue(systemUsesLightThemeRegistryValueName, invertedThemeColor, RegistryValueKind.DWord);
    }
    catch (Exception exception)
    {
        Console.WriteLine(exception);
        throw;
    }
}

internal enum WindowsThemeColor
{
    Dark = 0,
    Light = 1
}