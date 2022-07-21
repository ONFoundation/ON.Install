using InstallerApp.BackupRestore;
using InstallerApp.Security;
using InstallerApp.Terraform;
using ON.Installer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace InstallerApp
{
    /// <summary>
    /// Interaction logic for DeployWindow.xaml
    /// </summary>
    public partial class DeployWindow : Window
    {
        internal MainModel MyModel;
        internal DirectoryInfo DeployRootD;
        internal KeyHelper keyHelper;
        internal ResourceHelper resHelper = new ResourceHelper();
        internal TerraformHelper terraformHelper;
        FileInfo LogFile;
        internal bool needDockerInstalled;
        internal BackupRestoreServer backup;

        public DeployWindow()
        {
            terraformHelper = new TerraformHelper(this);
            InitializeComponent();
        }

        internal async Task StartDeploying()
        {
            needDockerInstalled = true;
            MyModel = MainWindow.MainModel;
            keyHelper = new KeyHelper(MyModel.Credentials.MasterKey);
            DeployRootD = MainWindow.TerraformLocation;
            LogFile = new FileInfo(DeployRootD.FullName + "/log.txt");

            Task createServer = CreateServer();
            await WaitOnTask(createServer, txtCreateServer);
            if (!createServer.IsCompletedSuccessfully)
                return;

            Task installDocker = Terraform.InstallDocker.Runner.InstallDocker(this);
            await WaitOnTask(installDocker, txtDocker);
            if (!installDocker.IsCompletedSuccessfully)
                return;

            if (MyModel.ProductSelection.Homepage == ProductSelectionModel.HomepageProducts.CMS)
            {
                Task deploySite = Terraform.Deploy.CMS.Runner.DeploySite(this);
                await WaitOnTask(deploySite, txtDeploySite);
                if (!deploySite.IsCompletedSuccessfully)
                    return;
            }
            else
            {
                Task deploySite = Terraform.Deploy.Business.Runner.DeploySite(this);
                await WaitOnTask(deploySite, txtDeploySite);
                if (!deploySite.IsCompletedSuccessfully)
                    return;
            }

            if (MyModel.ProductSelection.Homepage == ProductSelectionModel.HomepageProducts.CMS)
            {
                Task testSite = Deploy.DNSHelper.TestSite(this);
                await WaitOnTask(testSite, txtTestSite);
                if (!testSite.IsCompletedSuccessfully)
                    return;
            }


            backup = new BackupRestoreServer(MainWindow.BackupLocation, new ServiceNameHelper(MyModel.Server.IP), keyHelper);


            if (MyModel.ProductSelection.Homepage == ProductSelectionModel.HomepageProducts.CMS)
            {
                Task loadData = Deploy.LoadInitialData.Load(this);
                await WaitOnTask(loadData, txtLoadData);
                if (!loadData.IsCompletedSuccessfully)
                    return;
            }

            Task changeDns = Deploy.Godaddy.ChangeDNS(this);
            await WaitOnTask(changeDns, txtChangeDNS);
            if (!changeDns.IsCompletedSuccessfully)
                return;

            Task testDns = Deploy.DNSHelper.TestDNS(this);
            await WaitOnTask(testDns, txtTestDNS);
            if (!testDns.IsCompletedSuccessfully)
                return;

        }

        internal async Task CreateServer()
        {
            //await Terraform.CreateServer.Azure.Runner.CreateServerAzure(this);
            await Terraform.CreateServer.Digitalocean.Runner.CreateServeDigitalOcean(this);
            //await Terraform.CreateServer.AWS.Runner.CreateServerAWS(this);
        }

        List<string> lines = new();
        public async Task AddLine(string inStr)
        {
            if (inStr == null)
                return;

            var str = Regex.Replace(inStr, @"\e\[(\d+;)*(\d+)?[ABCDHJKfmsu]", "");
            lines.AddRange(str.Replace("\r", "").Split('\n').Select(l => l.Trim()));
            lines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            while (lines.Count > 10)
                lines.RemoveAt(0);

            this.Dispatcher.Invoke(() =>
            {
                txtOutput.Text = string.Join('\n', lines);
            });

            await AppendLog(str);
        }

        private async Task AppendLog(string str)
        {
            await File.AppendAllTextAsync(LogFile.FullName, str + "\n");
        }

        private async Task WaitOnTask(Task t, TextBlock txt)
        {
            DateTime start = DateTime.Now;

            while (!t.IsCompleted)
            {
                txt.Text = $"Running... {(int)(DateTime.Now - start).TotalSeconds}s";
                await Task.Delay(1000);
            }

            if (t.IsCompletedSuccessfully)
                txt.Text = $"Done... {(int)(DateTime.Now - start).TotalSeconds}s";
            else
                txt.Text = $"Error!... {(int)(DateTime.Now - start).TotalSeconds}s";
        }
    }
}
