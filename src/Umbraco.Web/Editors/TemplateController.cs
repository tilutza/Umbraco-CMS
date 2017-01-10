﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using AutoMapper;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;
using Constants = Umbraco.Core.Constants;

namespace Umbraco.Web.Editors
{
    [PluginController("UmbracoApi")]
    [UmbracoTreeAuthorize(Constants.Trees.Templates)]
    public class TemplateController : BackOfficeNotificationsController
    {
        /// <summary>
        /// Gets data type by alias
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public TemplateDisplay GetByAlias(string alias)
        {
            var template = Services.FileService.GetTemplate(alias);
            return template == null ? null : Mapper.Map<ITemplate, TemplateDisplay>(template);
        }

        /// <summary>
        /// Get all templates
        /// </summary>
        /// <returns></returns>
        public IEnumerable<EntityBasic> GetAll()
        {
            return Services.FileService.GetTemplates().Select(Mapper.Map<ITemplate, EntityBasic>);
        }

        /// <summary>
        /// Gets the content json for the content id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TemplateDisplay GetById(int id)
        {
            var template = Services.FileService.GetTemplate(id);
            if (template == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            return Mapper.Map<ITemplate, TemplateDisplay>(template);
        }

        /// <summary>
        /// Deletes a template wth a given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [HttpPost]
        public HttpResponseMessage DeleteById(int id)
        {
            var template = Services.FileService.GetTemplate(id);
            if (template == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            Services.FileService.DeleteTemplate(template.Alias);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        public TemplateDisplay GetEmpty()
        {
            var dt = new Template("", "");
            var content = ViewHelper.GetDefaultFileContent();

            var scaffold = Mapper.Map<ITemplate, TemplateDisplay>(dt);
            scaffold.Path = "-1";
            scaffold.Content =  content + "\r\n\r\n@* the fun starts here *@\r\n\r\n";
            return scaffold;
        }

        /// <summary>
        /// Saves the data type
        /// </summary>
        /// <param name="display"></param>
        /// <returns></returns>
        public TemplateDisplay PostSave(TemplateDisplay display)
        {

            //Checking the submitted is valid with the Required attributes decorated on the ViewModel
            if (ModelState.IsValid == false)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }


            if (display.Id > 0)
            {
                // update
                var template = Services.FileService.GetTemplate(display.Id);
                if (template == null)
                    throw new HttpResponseException(HttpStatusCode.NotFound);

                var changeMaster = template.MasterTemplateAlias != display.MasterTemplateAlias;
                Mapper.Map(display, template);

                if (changeMaster)
                {
                    if (string.IsNullOrEmpty(display.MasterTemplateAlias) == false)
                    {

                        var master = Services.FileService.GetTemplate(display.MasterTemplateAlias);
                        if(master == null || master.Id == display.Id)
                        {
                            template.SetMasterTemplate(null);
                        }else
                        {
                            template.SetMasterTemplate(master);
                        }

                    }
                    else
                    {
                        //remove the master
                        template.SetMasterTemplate(null);
                    }
                }

                Services.FileService.SaveTemplate(template);

                Mapper.Map(template, display);
            }
            else
            {
                //create
                ITemplate master = null;
                if (string.IsNullOrEmpty(display.MasterTemplateAlias) == false)
                {
                    master = Services.FileService.GetTemplate(display.MasterTemplateAlias);
                    if (master == null)
                        throw new HttpResponseException(HttpStatusCode.NotFound);
                }

                var template = Services.FileService.CreateTemplateWithIdentity(display.Name, display.Content, master);
                Mapper.Map(template, display);
            }

            return display;
        }
    }
}