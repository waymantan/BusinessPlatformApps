﻿using Microsoft.Deployment.Common.Actions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Deployment.Common.ActionModel;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Microsoft.Deployment.Common;
using Microsoft.Deployment.Common.Helpers;

namespace Microsoft.Deployment.Actions.Custom.Facebook
{
    [Export(typeof(IAction))]
    public class GetPermanentPageToken : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            string code = request.DataStore.GetValue("code");
            string clientId = Constants.FacebookClientId;
            string clientSecret = Constants.FacebookClientSecret;
            string redirectUri = request.Info.WebsiteRootUrl + Constants.WebsiteRedirectPath;
            JObject pages = new JObject();

            try
            {
                if (!string.IsNullOrEmpty(code))
                {
                    var shortLivedAccessToken = await GetToken($"https://graph.facebook.com/oauth/access_token?client_id={clientId}&client_secret={clientSecret}&redirect_uri={redirectUri}&code={code}&granted_scopes=manage_pages,publish_pages", "access_token");
                    var longLivedAccessToken = await GetToken($"https://graph.facebook.com/oauth/access_token?grant_type=fb_exchange_token&client_id={clientId}&client_secret={clientSecret}&fb_exchange_token={shortLivedAccessToken}&granted_scopes=manage_pages,publish_pages", "access_token");
                    var id = await GetUserId($"https://graph.facebook.com/v2.10/me?access_token={longLivedAccessToken}");
                    var pagePayload = await GetToken($"https://graph.facebook.com/v2.10/{id}/accounts?access_token={longLivedAccessToken}");
                    pages = JsonUtility.GetJObjectFromJsonString(pagePayload);
                    request.DataStore.AddObjectDataStore("FacebookPages", JsonUtility.GetJObjectFromObject(pages), DataStoreType.Private);
                }

                if (pages["data"].Count() > 0)
                {
                    return new ActionResponse(ActionStatus.Success);
                }
                return new ActionResponse(ActionStatus.Failure);
            }
            catch
            {
                throw;
            }
        }

        public static async Task<string> GetUserId(string uri)
        {
            string requestUri = uri;
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(requestUri);
            string responseObj = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception();
            }

            string id = JObject.Parse(responseObj)["id"].ToString();
            return id;
        }

        public static async Task<string> GetToken(string uri, string property = null)
        {
            string requestUri = uri;
            string responsePayload = string.Empty;
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(requestUri);
            string responseObj = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(await response.Content.ReadAsStringAsync());
            }

            if (string.IsNullOrEmpty(property))
            {
                responsePayload = JObject.Parse(responseObj).ToString();
            }
            else
            {
                responsePayload = JObject.Parse(responseObj)[property].ToString();
            }
            return responsePayload;
        }
    }
}
