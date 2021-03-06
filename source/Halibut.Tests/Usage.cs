// Copyright 2012-2013 Octopus Deploy Pty. Ltd.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Net;
using Halibut.Client;
using Halibut.Protocol;
using Halibut.Server;
using Halibut.Server.Security;
using NUnit.Framework;

namespace Halibut.Tests
{
    [TestFixture]
    public class Usage
    {
        [Test]
        public void AliceCanSendMessagesToBob()
        {
            using (var bob = new HalibutServer(new IPEndPoint(IPAddress.Any, 8013), Certificates.Bob))
            {
                bob.Services.Register<IEchoService, EchoService>();
                bob.Options.ClientCertificateValidator = v => v.Thumbprint == Certificates.AlicePublicThumbprint ? CertificateValidationResult.Valid : CertificateValidationResult.Rejected;
                bob.Start();

                var alice = new HalibutClient(Certificates.Alice);
                var echo = alice.Create<IEchoService>(new Uri("rpc://localhost:8013"), Certificates.BobPublicThumbprint);

                Assert.That(echo.SayHello("Hi Bob, it's Alice"), Is.EqualTo("Hello!"));
            }
        }

        [Test]
        public void AliceOnlySendsMessagesToBob()
        {
            using (var eve = new HalibutServer(new IPEndPoint(IPAddress.Any, 8013), Certificates.Eve))
            {
                eve.Services.Register<IEchoService, EchoService>();
                eve.Options.ClientCertificateValidator = v => v.Thumbprint == Certificates.AlicePublicThumbprint ? CertificateValidationResult.Valid : CertificateValidationResult.Rejected;
                eve.Start();

                var alice = new HalibutClient(Certificates.Alice);
                var echo = alice.Create<IEchoService>(new Uri("rpc://localhost:8013"), Certificates.BobPublicThumbprint);

                var ex = Assert.Throws<JsonRpcException>(() => echo.SayHello("Hi Bob, it's Eve"));
                Assert.That(ex.Message, Is.StringContaining("We aborted the connection because the remote host was not authenticated"));
            }
        }

        [Test]
        public void BobOnlyAcceptsMessagesFromAlice()
        {
            using (var bob = new HalibutServer(new IPEndPoint(IPAddress.Any, 8013), Certificates.Bob))
            {
                bob.Services.Register<IEchoService, EchoService>();
                bob.Options.ClientCertificateValidator = v => v.Thumbprint == Certificates.AlicePublicThumbprint ? CertificateValidationResult.Valid : CertificateValidationResult.Rejected;
                bob.Start();

                var eve = new HalibutClient(Certificates.Eve);
                var echo = eve.Create<IEchoService>(new Uri("rpc://localhost:8013"), Certificates.BobPublicThumbprint);

                var ex = Assert.Throws<JsonRpcException>(() => echo.SayHello("Hi Bob, it's Eve"));
                Assert.That(ex.Message, Is.StringContaining("This can happen when the remote server does not trust the certificate that we provided."));
            }
        }

        [Test]
        public void ServerExceptionsAreWrappedAsJsonExceptions()
        {
            using (var bob = new HalibutServer(new IPEndPoint(IPAddress.Any, 8013), Certificates.Bob))
            {
                bob.Services.Register<IEchoService, EchoService>();
                bob.Options.ClientCertificateValidator = v => v.Thumbprint == Certificates.AlicePublicThumbprint ? CertificateValidationResult.Valid : CertificateValidationResult.Rejected;
                bob.Start();

                var alice = new HalibutClient(Certificates.Alice);
                var echo = alice.Create<IEchoService>(new Uri("rpc://localhost:8013"), Certificates.BobPublicThumbprint);

                var jsonex = Assert.Throws<JsonRpcException>(() => echo.Crash());
                Assert.That(jsonex.Message, Is.StringContaining("divide by zero"));
            }
        }

        [Test]
        public void SupportsDifferentServiceContractMethods()
        {
            using (var bob = new HalibutServer(new IPEndPoint(IPAddress.Any, 8013), Certificates.Bob))
            {
                bob.Services.Register<ISupportedServices, SupportedServices>();
                bob.Options.ClientCertificateValidator = v => v.Thumbprint == Certificates.AlicePublicThumbprint ? CertificateValidationResult.Valid : CertificateValidationResult.Rejected;
                bob.Start();

                var alice = new HalibutClient(Certificates.Alice);
                var echo = alice.Create<ISupportedServices>(new Uri("rpc://localhost:8013"), Certificates.BobPublicThumbprint);

                echo.MethodReturningVoid(12, 14);

                Assert.That(echo.Hello(), Is.EqualTo("Hello"));
                Assert.That(echo.Hello("a"), Is.EqualTo("Hello a"));
                Assert.That(echo.Hello("a", "b"), Is.EqualTo("Hello a b"));
                Assert.That(echo.Hello("a", "b", "c"), Is.EqualTo("Hello a b c"));
                Assert.That(echo.Hello("a", "b", "c", "d"), Is.EqualTo("Hello a b c d"));
                Assert.That(echo.Hello("a", "b", "c", "d", "e"), Is.EqualTo("Hello a b c d e"));
                Assert.That(echo.Hello("a", "b", "c", "d", "e", "f"), Is.EqualTo("Hello a b c d e f"));
                Assert.That(echo.Hello("a", "b", "c", "d", "e", "f", "g"), Is.EqualTo("Hello a b c d e f g"));
                Assert.That(echo.Hello("a", "b", "c", "d", "e", "f", "g", "h"), Is.EqualTo("Hello a b c d e f g h"));
                Assert.That(echo.Hello("a", "b", "c", "d", "e", "f", "g", "h", "i"), Is.EqualTo("Hello a b c d e f g h i"));
                Assert.That(echo.Hello("a", "b", "c", "d", "e", "f", "g", "h", "i", "j"), Is.EqualTo("Hello a b c d e f g h i j"));
                Assert.That(echo.Hello("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k"), Is.EqualTo("Hello a b c d e f g h i j k"));

                Assert.That(echo.Add(1, 2), Is.EqualTo(3));
                Assert.That(echo.Add(1.00, 2.00), Is.EqualTo(3.00));
                Assert.That(echo.Add(1.10M, 2.10M), Is.EqualTo(3.20M));

                Assert.That(echo.Ambiguous("a", "b"), Is.EqualTo("Hello string"));
                Assert.That(echo.Ambiguous("a", new Tuple<string, string>("a", "b")), Is.EqualTo("Hello tuple"));

                var ex = Assert.Throws<JsonRpcException>(() => echo.Ambiguous("a", (string) null));
                Assert.That(ex.Message, Is.StringContaining("Ambiguous"));
            }
        }

        #region Nested type: EchoService

        public class EchoService : IEchoService
        {
            public string SayHello(string name)
            {
                return "Hello!";
            }

            public bool Crash()
            {
                throw new DivideByZeroException();
            }
        }

        #endregion

        #region Nested type: IEchoService

        public interface IEchoService
        {
            string SayHello(string name);

            bool Crash();
        }

        #endregion

        #region Nested type: ISupportedServices

        public interface ISupportedServices
        {
            void MethodReturningVoid(long a, long b);

            long Add(long a, long b);
            double Add(double a, double b);
            decimal Add(decimal a, decimal b);

            string Hello();
            string Hello(string a);
            string Hello(string a, string b);
            string Hello(string a, string b, string c);
            string Hello(string a, string b, string c, string d);
            string Hello(string a, string b, string c, string d, string e);
            string Hello(string a, string b, string c, string d, string e, string f);
            string Hello(string a, string b, string c, string d, string e, string f, string g);
            string Hello(string a, string b, string c, string d, string e, string f, string g, string h);
            string Hello(string a, string b, string c, string d, string e, string f, string g, string h, string i);
            string Hello(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j);
            string Hello(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, string k);

            string Ambiguous(string a, string b);
            string Ambiguous(string a, Tuple<string, string> b);
        }

        #endregion

        #region Nested type: SupportedServices

        public class SupportedServices : ISupportedServices
        {
            public void MethodReturningVoid(long a, long b)
            {
            }

            public long Add(long a, long b)
            {
                return a + b;
            }

            public double Add(double a, double b)
            {
                return a + b;
            }

            public decimal Add(decimal a, decimal b)
            {
                return a + b;
            }

            public string Hello()
            {
                return "Hello";
            }

            public string Hello(string a)
            {
                return "Hello " + a;
            }

            public string Hello(string a, string b)
            {
                return "Hello " + string.Join(" ", a, b);
            }

            public string Hello(string a, string b, string c)
            {
                return "Hello " + string.Join(" ", a, b, c);
            }

            public string Hello(string a, string b, string c, string d)
            {
                return "Hello " + string.Join(" ", a, b, c, d);
            }

            public string Hello(string a, string b, string c, string d, string e)
            {
                return "Hello " + string.Join(" ", a, b, c, d, e);
            }

            public string Hello(string a, string b, string c, string d, string e, string f)
            {
                return "Hello " + string.Join(" ", a, b, c, d, e, f);
            }

            public string Hello(string a, string b, string c, string d, string e, string f, string g)
            {
                return "Hello " + string.Join(" ", a, b, c, d, e, f, g);
            }

            public string Hello(string a, string b, string c, string d, string e, string f, string g, string h)
            {
                return "Hello " + string.Join(" ", a, b, c, d, e, f, g, h);
            }

            public string Hello(string a, string b, string c, string d, string e, string f, string g, string h, string i)
            {
                return "Hello " + string.Join(" ", a, b, c, d, e, f, g, h, i);
            }

            public string Hello(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j)
            {
                return "Hello " + string.Join(" ", a, b, c, d, e, f, g, h, i, j);
            }

            public string Hello(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, string k)
            {
                return "Hello " + string.Join(" ", a, b, c, d, e, f, g, h, i, j, k);
            }

            public string Ambiguous(string a, string b)
            {
                return "Hello string";
            }

            public string Ambiguous(string a, Tuple<string, string> b)
            {
                return "Hello tuple";
            }
        }

        #endregion
    }
}