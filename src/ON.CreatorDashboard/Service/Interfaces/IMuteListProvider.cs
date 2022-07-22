using ON.Fragments.CreatorDashboard.Subscribers;

namespace ON.CreatorDashboard.Service.Interfaces
{
    public interface IMuteListProvider
    {
        Task<MuteList> GetAll();
        Task SaveAll(MuteList list);
    }
}
