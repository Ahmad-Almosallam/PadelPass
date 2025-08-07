namespace PadelPass.Core.Services;

public interface IGlobalLocalizer
{
    string this[string key] { get; }
    string this[string key, params object[] arguments] { get; }
    string GetString(string key);
    string GetString(string key, params object[] arguments);
}