using Google.Protobuf;
using Microsoft.Extensions.Options;
using ON.CreatorDashboard.Service.Interfaces;
using ON.Fragments.CreatorDashboard.Subscribers;

namespace ON.CreatorDashboard.Service.Models
{
    public class FileSystemMuteListProvider : IMuteListProvider
    {
        private readonly DirectoryInfo dataDir;
        private readonly FileInfo listFile;

        public FileSystemMuteListProvider(IOptions<AppSettings> settings) 
        {
            var root = new DirectoryInfo(settings.Value.DataStore);
            root.Create();
            dataDir = root.CreateSubdirectory("server");
            listFile = new FileInfo(dataDir.FullName + "/mutes");
        }

        public async Task<MuteList> GetAll()
        {
            if (!listFile.Exists)
                return new MuteList();
            
            var bytes = await File.ReadAllBytesAsync(listFile.FullName);
            MuteList list = MuteList.Parser.ParseFrom(bytes);
            return list;
        }

        public async Task SaveAll(MuteList list)
        {
            await File.WriteAllBytesAsync(listFile.FullName, list.ToByteArray());
        }
    }
}
