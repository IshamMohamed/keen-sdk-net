﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keen.Core.AccessKey
{
    public class AccessKeys : IAccessKeys
    {
        private readonly IKeenHttpClient _keenHttpClient;
        private readonly string _acceskeyRelativeUrl;
        private readonly string _readKey;
        private readonly string _masterKey;

        internal AccessKeys(IProjectSettings prjSettings,
                       IKeenHttpClientProvider keenHttpClientProvider)
        {
            if (null == prjSettings)
            {
                throw new ArgumentNullException(nameof(prjSettings),
                                                "Project Settings must be provided.");
            }

            if (null == keenHttpClientProvider)
            {
                throw new ArgumentNullException(nameof(keenHttpClientProvider),
                                                "A KeenHttpClient provider must be provided.");
            }

            if (string.IsNullOrWhiteSpace(prjSettings.KeenUrl) ||
                !Uri.IsWellFormedUriString(prjSettings.KeenUrl, UriKind.Absolute))
            {
                throw new KeenException(
                    "A properly formatted KeenUrl must be provided via Project Settings.");
            }

            var serverBaseUrl = new Uri(prjSettings.KeenUrl);
            _keenHttpClient = keenHttpClientProvider.GetForUrl(serverBaseUrl);
            _acceskeyRelativeUrl = KeenHttpClient.GetRelativeUrl(prjSettings.ProjectId,
                                                               KeenConstants.AccessKeyResource);

            _readKey = prjSettings.ReadKey;
            _masterKey = prjSettings.MasterKey;
        }

        public async Task<JObject> CreateAccessKey(AccessKey accesskey)
        {
            if (string.IsNullOrWhiteSpace(_masterKey))
            {
                throw new KeenException("An API WriteKey is required to add events.");
            }

            var xcontent = accesskey.ToString();

            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };

            var content = JsonConvert.SerializeObject(accesskey, new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented
            }).ToSafeString();

            var responseMsg = await _keenHttpClient
                .PostAsync(_acceskeyRelativeUrl, _masterKey, content)
                .ConfigureAwait(continueOnCapturedContext: false);

            var responseString = await responseMsg
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            JObject jsonResponse = null;

            try
            {
                jsonResponse = JObject.Parse(responseString);
            }
            catch (Exception)
            {
            }
            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new KeenException("AddEvents failed with status: " + responseMsg.StatusCode);
            }

            if (null == jsonResponse)
            {
                throw new KeenException("AddEvents failed with empty response from server.");
            }

            return jsonResponse;
        }
    }
}
