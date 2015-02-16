﻿using System;
using System.ComponentModel.Composition;
using GitHub.Models;
using Octokit;
using Octokit.Internal;
using System.Threading.Tasks;
using System.Net.Http;
using GitHub.Extensions;
using System.Threading;
using System.Net;

namespace GitHub.Services
{
    [Export(typeof(IEnterpriseProbeTask))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class EnterpriseProbe : IEnterpriseProbeTask
    {
        static readonly Uri endPoint = new Uri("/site/sha", UriKind.Relative);
        readonly ProductHeaderValue productHeader;
        readonly IHttpClient httpClient;

        [ImportingConstructor]
        public EnterpriseProbe(IProgram program, IHttpClient httpClient)
        {
            productHeader = program.ProductHeader;
            this.httpClient = httpClient;
        }

        public async Task<EnterpriseProbeResult> ProbeAsync(Uri enterpriseBaseUrl)
        {
            var request = new Request
            {
                Method = HttpMethod.Get,
                BaseAddress = enterpriseBaseUrl,
                Endpoint = endPoint,
                Timeout = TimeSpan.FromSeconds(3),
                AllowAutoRedirect = false,
            };
            request.Headers.Add("User-Agent", productHeader.ToString());

            var ret = await httpClient
                    .Send<object>(request, CancellationToken.None)
                    .Catch(ex => null);

            if (ret == null)
                return EnterpriseProbeResult.Failed;
            else if (ret.StatusCode == HttpStatusCode.OK)
                return EnterpriseProbeResult.Ok;
            return EnterpriseProbeResult.NotFound;
        }
    }

    public enum EnterpriseProbeResult
    {
        /// <summary>
        /// Yep! It's an Enterprise server
        /// </summary>
        Ok,

        /// <summary>
        /// Got a response from a server, but it wasn't an Enterprise server
        /// </summary>
        NotFound,

        /// <summary>
        /// Request timed out or DNS failed. So it's probably the case it's not an enterprise server but 
        /// we can't know for sure.
        /// </summary>
        Failed
    }
}
