using ON.Fragments.Content;

namespace ON.Content.Rumble.Service.Models
{
    public interface IFileSystemRumbleProvider
    {
        Task<RumbleData> GetData();
        Task SaveData(RumbleData data);
        Task<bool> IsDuplicateVideo(string videoId);
    }
}
