namespace PasteIt.Core
{
    public interface IUpdateFeedClient
    {
        UpdateInfo? GetLatestRelease();
    }
}
