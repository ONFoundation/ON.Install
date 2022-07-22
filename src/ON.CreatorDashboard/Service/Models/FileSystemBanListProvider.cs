using Google.Protobuf;
using Microsoft.Extensions.Options;
using ON.CreatorDashboard.Service.Interfaces;
using ON.Fragments.CreatorDashboard.Subscribers;

namespace ON.CreatorDashboard.Service.Models
{
    public class FileSystemBanListProvider : IBanListProvider
    {
        private readonly DirectoryInfo dataDir;
        private readonly FileInfo listFile;

        public FileSystemBanListProvider(IOptions<AppSettings> settings)
        {
            var root = new DirectoryInfo(settings.Value.DataStore);
            root.Create();
            dataDir = root.CreateSubdirectory("server");
            listFile = new FileInfo(dataDir.FullName + "/bans");
        }

        public async Task<BanList> GetAll()
        {
            if (!listFile.Exists)
                return new BanList();

            var bytes = await File.ReadAllBytesAsync(listFile.FullName);
            BanList list = BanList.Parser.ParseFrom(bytes);
            return list;
        }

        public async Task SaveAll(BanList list)
        {
            await File.WriteAllBytesAsync(listFile.FullName, list.ToByteArray());
        }
    }
}
