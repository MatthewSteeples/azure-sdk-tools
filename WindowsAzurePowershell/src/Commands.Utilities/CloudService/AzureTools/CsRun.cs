﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Commands.Utilities.CloudService.AzureTools
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Common;
    using Microsoft.WindowsAzure.Commands.Utilities.Properties;

    public class CsRun 
    {
        public int DeploymentId { get; private set; }
        private string _csrunPath;
        public CsRun(string emulatorDirectory)
        {
            _csrunPath = Path.Combine(emulatorDirectory, Resources.CsRunExe);
        }

        // a test seam used for unit testing this class 
        internal ProcessHelper CommandRunner { get; set; }
        /// <summary>
        /// Deploys package on local machine. This method does following:
        /// 1. Starts compute emulator.
        /// 2. Starts storage emulator.
        /// 3. Remove all previous deployments in the emulator.
        /// 4. Deploys the package on local machine.
        /// </summary>
        /// <param name="packagePath">Path to package which will be deployed</param>
        /// <param name="configPath">Local configuration path to used with the package</param>
        /// <param name="launch">Switch which opens browser for web roles after deployment</param>
        /// <param name="mode">Emulator mode: Full or Express</param>
        /// <param name="standardOutput">Standard output of deployment</param>
        /// <param name="standardError">Standard error of deployment</param>
        /// <returns>Deployment id associated with the deployment</returns>
        public int StartEmulator(string packagePath, 
                string configPath, 
                bool launch, 
                ComputeEmulatorMode mode,
                out string standardOutput, 
                out string standardError)
        {
            // Starts compute emulator.
            StartComputeEmulator(mode, out standardOutput, out standardError);

            // Remove all previous deployments in the emulator.
            RemoveAllDeployments(out standardOutput, out standardError);

            // Deploys the package on local machine.
            string arguments = string.Format(Resources.RunInEmulatorArguments, packagePath, configPath, (launch) ? Resources.CsRunLanuchBrowserArg : string.Empty);
            StartCsRunProcess(mode, arguments, out standardOutput, out standardError);

            // Get deployment id for future use.
            DeploymentId = GetDeploymentCount(standardOutput);
            standardOutput = GetRoleInfoMessage(standardOutput);

            return DeploymentId;
        }

        private void StartComputeEmulator(ComputeEmulatorMode mode, out string standardOutput, out string standardError)
        {
            StartCsRunProcess(mode, Resources.CsRunStartComputeEmulatorArg, out standardOutput, out standardError);
        }
      
        public void StopComputeEmulator()
        {
            string output, error;
            StartCsRunProcess(Resources.CsRunStopEmulatorArg, out output, out error);
        }   

        public void RemoveDeployment(int deploymentId, out string standardOutput, out string standardError)
        {
            StartCsRunProcess(string.Format(Resources.CsRunRemoveDeploymentArg, deploymentId), out standardOutput, out standardError);
        }

        public void RemoveAllDeployments(out string standardOutput, out string standardError)
        {
            StartCsRunProcess(Resources.CsRunRemoveAllDeploymentsArg, out standardOutput, out standardError);
        }

        public void UpdateDeployment(int deploymentId, string configPath, out string standardOutput, out string standardError)
        {
            StartCsRunProcess(string.Format(Resources.CsRunUpdateDeploymentArg, deploymentId, configPath), out standardOutput, out standardError);
        }

        private void StartCsRunProcess(string arguments, out string standardOutput, out string standardError)
        {
            ProcessHelper runner = GetCommandRunner();
            runner.StartAndWaitForProcess(_csrunPath, arguments);
            standardOutput = runner.StandardOutput;
            standardError = runner.StandardError;
            // If there's an error from the CsRun tool, we want to display that
            // error message.
            if (!string.IsNullOrEmpty(standardError))
            {
                if (!IsStorageEmulatorError(standardError))
                {
                    throw new InvalidOperationException(
                        string.Format(Resources.CsRun_StartCsRunProcess_UnexpectedFailure, standardError));
                }
            }
        }

        private void StartCsRunProcess(ComputeEmulatorMode mode, string arguments, out string standardOutput, out string standardError)
        {
            if (mode == ComputeEmulatorMode.Express)
            {
                arguments += " " + Resources.CsRunEmulatorExpressArg;
            }
            StartCsRunProcess(arguments, out standardOutput, out standardError);
        }

        private ProcessHelper GetCommandRunner()
        {
            if (CommandRunner == null)
            {
                CommandRunner = new ProcessHelper();
            }
            return CommandRunner;
        }

        private bool IsStorageEmulatorError(string error)
        {
            return error.IndexOf("storage emulator", StringComparison.OrdinalIgnoreCase) != -1;
        }

        private int GetDeploymentCount(string text)
        {
            Regex deploymentRegex = new Regex("deployment16\\((\\d+)\\)", RegexOptions.Multiline);
            int value = -1;

            Match match = deploymentRegex.Match(text);

            if (match.Success)
            {
                string digits = match.Captures[0].Value;
                int.TryParse(digits, out value);
            }

            return value;

        }

        public static string GetRoleInfoMessage(string emulatorOutput)
        {
            var regex = new Regex(Resources.EmulatorOutputSitesRegex);
            var match = regex.Match(emulatorOutput);
            var builder = new StringBuilder();
            while (match.Success)
            {
                builder.AppendLine(string.Format(Resources.EmulatorRoleRunningMessage, match.Value.Substring(0, match.Value.Length - 2)));
                match = match.NextMatch();
            }
            var roleInfo = builder.ToString(0, builder.Length - 2);
            return roleInfo;
        }
    }
}