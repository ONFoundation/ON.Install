using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InstallerApp.Terraform
{
    internal class ResourceHelper
    {
        Assembly assembly;
        string[] names;

        public ResourceHelper()
        {
            assembly = Assembly.GetExecutingAssembly();
            names = assembly.GetManifestResourceNames();
        }

        public async Task SaveChangeSSH(DirectoryInfo dir)
        {
            await Save("InstallerApp.Terraform.ChangeSsh.", dir);
        }

        public async Task SaveCreateAzure(DirectoryInfo dir)
        {
            await Save("InstallerApp.Terraform.CreateServer.Azure.", dir);
        }

        public async Task SaveCreateDigitalocean(DirectoryInfo dir)
        {
            await Save("InstallerApp.Terraform.CreateServer.Digitalocean.", dir);
        }
        
        public async Task SaveCreateAWS(DirectoryInfo dir)
        {
            await Save("InstallerApp.Terraform.CreateServer.AWS.", dir);
        }

        public async Task SaveDeployCMS(DirectoryInfo dir)
        {
            await Save("InstallerApp.Terraform.Deploy.CMS.", dir);
        }

        public async Task SaveDeployBusiness(DirectoryInfo dir)
        {
            await Save("InstallerApp.Terraform.Deploy.Business.", dir);
        }

        public async Task SaveInstallDocker(DirectoryInfo dir)
        {
            await Save("InstallerApp.Terraform.InstallDocker.", dir);
        }

        private async Task Save(string prefix, DirectoryInfo dir)
        {
            foreach(var name in GetFileNames(prefix))
            {
                using var s = assembly.GetManifestResourceStream(name.fullName);
                using var f = File.Create(dir.FullName + "/" + name.fileName);
                await s.CopyToAsync(f);
            }
        }

        private IEnumerable<(string fullName, string fileName)> GetFileNames(string prefix)
        {
            return names.Where(n => n.StartsWith(prefix)).Select(n => (n, n.Replace(prefix, "")));
        }
    }
}
