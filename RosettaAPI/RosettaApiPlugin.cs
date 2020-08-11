using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO.Compression;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Neo.Plugins
{
    public sealed class RosettaApiPlugin : Plugin
    {
        private IWebHost host;

        public override void Configure()
        {
            RosettaApiSettings.Load(GetConfiguration());
        }

        protected override void OnPluginsLoaded()
        {
            var dflt = RosettaApiSettings.Default;
            host = new WebHostBuilder().UseKestrel(options => options.Listen(dflt.BindAddress, dflt.Port, listenOptions =>
            {
                // default is unlimited
                options.Limits.MaxConcurrentConnections = 50;
                // default is 2 minutes
                options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(1);
                // default is 30 seconds
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);

                if (string.IsNullOrEmpty(dflt.SslCert)) return;
                listenOptions.UseHttps(dflt.SslCert, dflt.SslCertPassword, httpsConnectionAdapterOptions =>
                {
                    if (dflt.TrustedAuthorities is null || dflt.TrustedAuthorities.Length == 0)
                        return;
                    httpsConnectionAdapterOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    httpsConnectionAdapterOptions.ClientCertificateValidation = (cert, chain, err) =>
                    {
                        if (err != SslPolicyErrors.None)
                            return false;
                        X509Certificate2 authority = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
                        return dflt.TrustedAuthorities.Contains(authority.Thumbprint);
                    };
                });
            }))
            .ConfigureServices(services =>
            {
                services.AddResponseCompression(options =>
                {
                    options.Providers.Add<GzipCompressionProvider>();
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
                });
                services.Configure<GzipCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Fastest;
                });
                services.AddSingleton(s => System);
            })
            .Configure(app =>
            {
                app.UseResponseCompression();
                app.UseMvc();
            })
            .Build();
            host.Start();
        }
    }
}
