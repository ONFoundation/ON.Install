using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerApp.Terraform.CreateServer.AWS
{
    internal class Runner
    {
        internal static async Task CreateServerAWS(DeployWindow window)
        {
            await window.AddLine("--- Create Server ---");

            var targetD = new DirectoryInfo($"{window.DeployRootD.FullName}/createServer/aws");
            var terraD = new DirectoryInfo(targetD.FullName + "/.terraform");
            var varF = new FileInfo(targetD.FullName + "/variables.tf");

            if (!targetD.Exists)
            {
                targetD.Create();
                await window.resHelper.SaveCreateAWS(targetD);
            }
            //Create ssh key to apply to server
            var ssh = Security.SshHelper.CreateRSAKey("temp@onf");
            //Environment variables with the server information to create
            var envVars = new Dictionary<string, string>();
            envVars["prefix"] = "onf-" + window.MyModel.DNS.Name.Replace(".", "-");
            envVars["location"] = "us-east-2";
            envVars["username"] = "ubuntu";
            envVars["sshPub"] = ssh.pubKey;
            //
            if (!terraD.Exists)
            {
                await window.terraformHelper.RunTerraform(targetD, "init", envVars);
            }

            await window.terraformHelper.RunTerraform(targetD, "apply -auto-approve", envVars);
            await window.terraformHelper.RunTerraform(targetD, "refresh", envVars);

            var addyLine = (await File.ReadAllLinesAsync(targetD.FullName + "/terraform.tfstate")).FirstOrDefault(l => l.Contains("\"public_ip\""));
            var addy = addyLine.GetBetween(": \"", "\"");
            window.MyModel.Server.IP = addy;
            window.MyModel.Server.User = "ubuntu";

            await Terraform.ChangeSsh.Runner.ChangeSshKey(window, ssh.privKey);
        }
    }
}
