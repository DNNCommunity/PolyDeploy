using Cantarus.Modules.PolyDeploy.Components.DataAccess.Models;
using Cantarus.Modules.PolyDeploy.Components.Exceptions;
using Cantarus.Modules.PolyDeploy.Components.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Cantarus.Modules.PolyDeploy.Components.WebAPI.ActionFilters
{
    internal class InWhitelist : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);

            // Get whitelist state.
            bool whitelistDisabled;

            try
            {
                // Attempt to retrieve disabled state.
                whitelistDisabled = SettingManager.GetSetting("WHITELIST", "STATE").Value.ToLower() == "false";
            }
            catch (SettingNotFoundException ex)
            {
                // Setting not set, default to off.
                whitelistDisabled = true;
            }

            // Get api user.
            string apiKey = actionContext.Request.GetApiKey();
            APIUser apiUser = APIUserManager.GetByAPIKey(apiKey);

            // Is the whitelist disabled or does the api user have permission to
            // bypass it?
            if (whitelistDisabled || (apiUser != null && apiUser.BypassIPWhitelist))
            {
                // No need to perform whitelisting checks, return early.
                return;
            }

            bool authenticated = false;
            string message = "Access denied.";

            string forwardingAddress = null;
            string clientIpAddress = null;

            try
            {
                // There is a strong possibility that this is not the ip address of the machine
                // that sent the request. Being behind a load balancer with transparancy switched
                // off or being served through CloudFlare will both affect this value.
                clientIpAddress = HttpContext.Current.Request.UserHostAddress;

                // We need to get the X-Forwarded-For header from the request, if this is set we
                // should use it instead of the ip address from the request.
                string forwardedFor = HttpContext.Current.Request.Headers.Get("X-Forwarded-For");

                // Forwarded for set?
                if (forwardedFor != null)
                {
                    forwardingAddress = clientIpAddress;
                    clientIpAddress = forwardedFor;
                }

                // Got the ip address?
                if (!string.IsNullOrEmpty(clientIpAddress))
                {
                    // Is it whitelisted or localhost?
                    if (IPSpecManager.IsWhitelisted(clientIpAddress) || clientIpAddress.Equals("127.0.0.1"))
                    {
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
                string log = string.Format("Whitelist check failed for IP address: {0}.", clientIpAddress);

                // Was it forwarded?
                if (forwardingAddress != null)
                {
                    log = string.Format("Whitelist check failed for IP address: {0}, forwarded by: {1}.", clientIpAddress, forwardingAddress);
                }

                EventLogManager.Log("AUTH_BAD_IPADDRESS", EventLogSeverity.Warning, log);

                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, message);
            }
        }
    }
}
