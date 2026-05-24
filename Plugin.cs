using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using HolidayCountdown.Controls;
using HolidayCountdown.Services;
using HolidayCountdown.Views.SettingsPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HolidayCountdown;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        var holidayService = new HolidayService(PluginConfigFolder);
        services.AddSingleton(holidayService);

        services.AddComponent<HolidayCountdownComponent, HolidayCountdownSettingsControl>();
        services.AddSettingsPage<HolidayDataSettingsPage>();
    }
}
