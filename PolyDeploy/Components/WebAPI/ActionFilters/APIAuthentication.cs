﻿using Cantarus.Modules.PolyDeploy.Components.DataAccess.Models;
using Cantarus.Modules.PolyDeploy.Components.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Cantarus.Modules.PolyDeploy.Components.WebAPI.ActionFilters
{
    internal class APIAuthentication : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);

            bool authenticated = false;
            string message = "Access denied.";

            string apiKey = null;

            try
            {
                apiKey = actionContext.Request.GetApiKey();

                // Make sure it's not null and it's 32 characters or we're wasting our time.
                if (apiKey != null && apiKey.Length == 32)
                {
                    // Attempt to look up the api user.
                    APIUser apiUser = APIUserManager.FindAndPrepare(apiKey);

                    // Did we find one and is it ready to use?
                    if (apiUser != null && apiUser.Prepared)
                    {
                        // Genuine API user.
                        authenticated = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Set appropriate message.
                message = "An error occurred while trying to authenticate this request.";

                EventLogManager.Log("AUTH_EXCEPTION", EventLogSeverity.Info, null, ex);
            }

            // If authentication failure occurs, return a response without carrying on executing actions.
            if (!authenticated)
            {
                EventLogManager.Log("AUTH_BAD_APIKEY", EventLogSeverity.Warning, string.Format("Authentication failed for API key: {0}.", apiKey));

                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, message);
            }
        }
    }
}
