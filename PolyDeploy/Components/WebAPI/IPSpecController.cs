﻿using Cantarus.Modules.PolyDeploy.Components.DataAccess.Models;
using Cantarus.Modules.PolyDeploy.Components.Exceptions;
using Cantarus.Modules.PolyDeploy.Components.WebAPI.ActionFilters;
using DotNetNuke.Web.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Cantarus.Modules.PolyDeploy.Components.WebAPI
{
    [RequireHost]
    [ValidateAntiForgeryToken]
    [InWhitelist]
    public class IPSpecController : DnnApiController
    {
        [HttpGet]
        public HttpResponseMessage GetAll()
        {
            List<IPSpec> ipSpecs = IPSpecManager.GetAll().ToList();

            return Request.CreateResponse(HttpStatusCode.OK, ipSpecs);
        }

        [HttpPost]
        public HttpResponseMessage Create(string name, string ip)
        {
            IPSpec ipSpec = null;

            try
            {
                 ipSpec = IPSpecManager.Create(name, ip);
            }
            catch (IPSpecExistsException ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.Conflict, ex.Message);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }

            return Request.CreateResponse(HttpStatusCode.Created, ipSpec);
        }

        [HttpDelete]
        public HttpResponseMessage Delete(int id)
        {
            IPSpec ipSpec = IPSpecManager.GetById(id);

            if (ipSpec == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "IP spec not found.");
            }

            try
            {
                IPSpecManager.Delete(ipSpec);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete IP spec.");
            }

            return Request.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
