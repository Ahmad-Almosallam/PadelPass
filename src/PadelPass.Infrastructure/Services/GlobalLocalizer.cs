using Microsoft.Extensions.Localization;
using PadelPass.Core;
using PadelPass.Core.Services;

namespace PadelPass.Infrastructure.Services;

public class GlobalLocalizer : IGlobalLocalizer
{
    private readonly IStringLocalizer<GlobalResource> _localizer;

    public GlobalLocalizer(IStringLocalizer<GlobalResource> localizer)
    {
        _localizer = localizer;
    }

    public string this[string key] => _localizer[key];

    public string this[string key, params object[] arguments] => _localizer[key, arguments];

    public string GetString(string key)
    {
        return _localizer[key];
    }

    public string GetString(string key, params object[] arguments)
    {
        return _localizer[key, arguments];
    }
}