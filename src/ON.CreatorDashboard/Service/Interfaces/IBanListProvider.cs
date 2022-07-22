using ON.Fragments.CreatorDashboard.Subscribers;

namespace ON.CreatorDashboard.Service.Interfaces
{
    public interface IBanListProvider
    {
        Task<BanList> GetAll();
        Task SaveAll(BanList list);
    }
}
