﻿using Cantarus.Modules.PolyDeploy.Components.DataAccess.Models;
using Cantarus.Modules.PolyDeploy.Components.WebAPI.ActionFilters;
using DotNetNuke.Web.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace Cantarus.Modules.PolyDeploy.Components.WebAPI
{
    [RequireHost]
    [ValidateAntiForgeryToken]
    [InWhitelist]
    public class APIUserController : DnnApiController
    {
        [HttpGet]
        public HttpResponseMessage GetAll()
        {
            List<APIUser> apiUsers = APIUserManager.GetAll().ToList();

            // Loop and remove sensitive information.
            foreach (APIUser apiUser in apiUsers)
            {
                apiUser.APIKey_Sha = null;
                apiUser.EncryptionKey_Enc = null;
                apiUser.Salt = null;
            }

            return Request.CreateResponse(HttpStatusCode.OK, apiUsers);
        }

        [HttpPost]
        public HttpResponseMessage Create(string name, bool bypass = false)
        {
            // Check we have a name.
            if (string.IsNullOrEmpty(name))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            // Create user.
            APIUser apiUser = APIUserManager.Create(name, bypass);

            apiUser.APIKey_Sha = null;
            apiUser.EncryptionKey_Enc = null;
            apiUser.Salt = null;

            return Request.CreateResponse(HttpStatusCode.Created, apiUser);
        }

        [HttpPut]
        public HttpResponseMessage Update()
        {
            JavaScriptSerializer jsonSer = new JavaScriptSerializer();

            string json = Request.Content.ReadAsStringAsync().Result;

            APIUser apiUser;

            try
            {
                apiUser = jsonSer.Deserialize<APIUser>(json);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Deserialization failure.");
            }

            try
            {
                apiUser = APIUserManager.Update(apiUser);

                apiUser.APIKey_Sha = null;
                apiUser.EncryptionKey_Enc = null;
                apiUser.Salt = null;
            }
            catch(Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update API user.");
            }

            return Request.CreateResponse(HttpStatusCode.OK, apiUser);
        }

        [HttpDelete]
        public HttpResponseMessage Delete(int id)
        {
            APIUser apiUser = APIUserManager.GetById(id);

            if (apiUser == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "API user not found.");
            }

            try
            {
                APIUserManager.Delete(apiUser);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete API user.");
            }

            return Request.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
