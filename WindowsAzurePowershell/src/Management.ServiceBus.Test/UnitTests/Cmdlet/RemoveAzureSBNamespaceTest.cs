﻿// ----------------------------------------------------------------------------------
//
// Copyright 2011 Microsoft Corporation
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

namespace Microsoft.WindowsAzure.Management.ServiceBus.Test.UnitTests.Cmdlet
{
    using System;
    using System.Management.Automation;
    using Microsoft.WindowsAzure.Management.CloudService.Test;
    using Microsoft.WindowsAzure.Management.CloudService.Test.Utilities;
    using Microsoft.WindowsAzure.Management.ServiceBus.Cmdlet;
    using Microsoft.WindowsAzure.Management.ServiceBus.Properties;
    using Microsoft.WindowsAzure.Management.Test.Stubs;
    using Microsoft.WindowsAzure.Management.Test.Tests.Utilities;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RemoveAzureSBNamespaceTests : TestBase
    {
        [TestInitialize]
        public void SetupTest()
        {
            Management.Extensions.CmdletSubscriptionExtensions.SessionManager = new InMemorySessionManager();
        }

        [TestMethod]
        public void RemoveAzureSBNamespaceSuccessfull()
        {
            // Setup
            SimpleServiceManagement channel = new SimpleServiceManagement();
            MockCommandRuntime mockCommandRuntime = new MockCommandRuntime();
            string name = "test";
            RemoveAzureSBNamespaceCommand cmdlet = new RemoveAzureSBNamespaceCommand(channel) { Name = name, CommandRuntime = mockCommandRuntime, PassThru = true };
            bool deleted = false;
            string expectedVerbose = string.Format(Resources.RemovingNamespaceMessage, name);
            channel.DeleteServiceBusNamespaceThunk = dsbn => { deleted = true; };

            // Test
            cmdlet.ExecuteCmdlet();

            // Assert
            string actual = mockCommandRuntime.VerboseChannel[0] as string;
            Assert.IsTrue(deleted);
            Assert.AreEqual<string>(expectedVerbose, actual);
            Assert.IsTrue((bool)mockCommandRuntime.WrittenObjects[0]);
        }

        [TestMethod]
        public void RemoveAzureSBNamespaceWithInvalidNamesFail()
        {
            // Setup
            string[] invalidNames = { "1test", "test#", "test invaid", "-test", "_test" };

            foreach (string invalidName in invalidNames)
            {
                MockCommandRuntime mockCommandRuntime = new MockCommandRuntime();
                RemoveAzureSBNamespaceCommand cmdlet = new RemoveAzureSBNamespaceCommand() { Name = invalidName, CommandRuntime = mockCommandRuntime };
                ArgumentException expected = new ArgumentException(string.Format(Resources.InvalidNamespaceName, invalidName), "Name");

                // Test
                cmdlet.ExecuteCmdlet();

                // Assert
                ErrorRecord actual = mockCommandRuntime.ErrorRecords[0];
                Assert.AreEqual<string>(expected.Message, actual.Exception.Message);
            }
        }

        [TestMethod]
        public void RemoveAzureSBNamespaceWithInternalServerError()
        {
            // Setup
            SimpleServiceManagement channel = new SimpleServiceManagement();
            MockCommandRuntime mockCommandRuntime = new MockCommandRuntime();
            string name = "test";
            RemoveAzureSBNamespaceCommand cmdlet = new RemoveAzureSBNamespaceCommand(channel) { Name = name, CommandRuntime = mockCommandRuntime };
            string expected = Resources.RemoveNamespaceErrorMessage;
            channel.DeleteServiceBusNamespaceThunk = dsbn => { throw new Exception(Resources.InternalServerErrorMessage); };

            // Test
            cmdlet.ExecuteCmdlet();

            // Assert
            ErrorRecord actual = mockCommandRuntime.ErrorRecords[0];
            Assert.AreEqual<string>(expected, actual.Exception.Message);
        }
    }
}